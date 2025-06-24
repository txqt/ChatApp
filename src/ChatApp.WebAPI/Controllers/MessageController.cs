using ChatApp.Contracts.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enum;

using ChatApp.WebAPI.Controllers;
using ChatApp.WebAPI.Hubs;
using ChatApp.WebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.WebAPI.Controllers
{
    public class MessageController : BaseController
    {
        private readonly IApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IHubContext<ChatHub> _chatHubContext;
        private readonly IChatPermissionService _chatPermissionService;
        private readonly IMediaService _mediaService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MessageController(IApplicationDbContext context, IWebHostEnvironment environment, IHubContext<ChatHub> chatHubContext, IUserService userService, IChatPermissionService chatPermissionService, IMediaService mediaService, IHttpContextAccessor httpContextAccessor) : base(userService)
        {
            _context = context;
            _environment = environment;
            _chatHubContext = chatHubContext;
            _chatPermissionService = chatPermissionService;
            _mediaService = mediaService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SendMessage([FromForm] SendMessageRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1. Phân quyền
            if (!await _chatPermissionService
                .CanUserPerformAction(CurrentUserId, request.ChatId, ChatPermissions.SendMessages))
                return Forbid("You don't have permission to send messages in this chat");

            // 2. Xử lý reply-to
            if (request.ReplyToMessageId.HasValue)
            {
                var exists = await _context.Messages
                    .AnyAsync(m => m.MessageId == request.ReplyToMessageId.Value
                                   && m.ChatId == request.ChatId);
                if (!exists)
                    return BadRequest("Reply message not found");
            }

            request.MessageType = MessageType.Text;
            var savedMedia = new List<MediaFile>();
            if (await _chatPermissionService.CanUserPerformAction(CurrentUserId, request.ChatId, ChatPermissions.SendMedia))
            {
                if (request.Files != null && request.Files.Count >= 0)
                {
                    foreach (var file in request.Files)
                    {
                        var result = await _mediaService.SaveMediaFileAsync(file, CurrentUserId);
                        savedMedia.Add(result);
                    }
                    bool allImage = request.Files.All(f => f.ContentType.StartsWith("image/"));
                    bool allVideo = request.Files.All(f => f.ContentType.StartsWith("video/"));
                    bool allAudio = request.Files.All(f => f.ContentType.StartsWith("audio/"));

                    if (allImage)
                        request.MessageType = MessageType.Image;
                    else if (allVideo)
                        request.MessageType = MessageType.Video;
                    else if (allAudio)
                        request.MessageType = MessageType.Audio;
                    else
                        request.MessageType = MessageType.File;
                }
            }

            // 4. Tạo message và lưu database
            var message = new Message
            {
                ChatId = request.ChatId,
                SenderId = CurrentUserId,
                Content = request.Content,
                MessageType = request.MessageType,
                MediaFileIds = savedMedia.Select(x => x.FileId).ToList() ?? new List<int>(), // Chỉ lấy file đầu tiên nếu có nhiều file
                ReplyToMessageId = request.ReplyToMessageId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Update chat last message
            var chat = await _context.Chats.FindAsync(request.ChatId);
            if (chat != null)
            {
                chat.LastMessageId = message.MessageId;
                chat.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            var dto = ConvertToMessageDto(message, savedMedia);

            await _chatHubContext
                .Clients.Group($"Chat_{request.ChatId}")
                .SendAsync("ReceiveMessage", dto);

            return Ok(dto);
        }

        [HttpPut("{messageId}")]
        public async Task<IActionResult> EditMessage(int messageId, [FromBody] EditMessageRequest request)
        {
            var message = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.ReplyToMessage)
                    .ThenInclude(rm => rm.Sender)
                .FirstOrDefaultAsync(m => m.MessageId == messageId && m.SenderId == CurrentUserId && !m.IsDeleted);

            if (message == null)
                return NotFound("Message not found or you don't have permission to edit");

            var permission = await _chatPermissionService.CanUserPerformAction(CurrentUserId, message.ChatId, ChatPermissions.EditOwnMessages | ChatPermissions.EditAnyMessage);
            if (!permission)
                return Forbid("You don't have permission");

            // Chỉ cho phép edit trong 24 giờ
            if (DateTime.UtcNow - message.CreatedAt > TimeSpan.FromHours(24))
                return BadRequest("Cannot edit message after 24 hours");

            message.Content = request.Content;
            message.IsEdited = true;
            message.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var mediaFiles = await _mediaService.GetMediaFilesAsync(message.MediaFileIds, CurrentUserId);

            var messageWithDetails = ConvertToMessageDto(message, mediaFiles);

            return Ok(messageWithDetails);
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId, [FromQuery] bool deleteForEveryone = false)
        {
            var message = await _context.Messages
                .Include(m => m.Chat)
                    .ThenInclude(c => c.Members)
                .FirstOrDefaultAsync(m => m.MessageId == messageId && !m.IsDeleted);

            if (message == null)
                return NotFound("Message not found");

            // Kiểm tra quyền xóa
            var permission = await _chatPermissionService.CanUserPerformAction(CurrentUserId, message.ChatId, ChatPermissions.DeleteAnyMessage | ChatPermissions.DeleteOwnMessages);
            if (!permission)
                return Forbid("You don't have permission");

            message.IsDeleted = true;
            message.DeletedAt = DateTime.UtcNow;
            message.DeletedBy = CurrentUserId;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Message deleted successfully", deleteForEveryone });
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchMessages([FromQuery] int chatId, [FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Search query is required");

            var permission = await _chatPermissionService.CanUserPerformAction(CurrentUserId, chatId, ChatPermissions.ViewMessageHistory | ChatPermissions.ViewMessages);
            if (!permission)
                return Forbid("You don't have permission");

            var messages = await _context.Messages
                .Where(m => m.ChatId == chatId && !m.IsDeleted && m.Content.Contains(query))
                .Include(m => m.Sender)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    m.MessageId,
                    m.Content,
                    m.CreatedAt,
                    Sender = new
                    {
                        m.Sender.Id,
                        m.Sender.DisplayName,
                        m.Sender.AvatarUrl
                    }
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpPost("{messageId}/forward")]
        public async Task<IActionResult> ForwardMessage(int messageId, [FromBody] ForwardMessageRequest request)
        {
            var originalMessage = await _context.Messages
                .FirstOrDefaultAsync(m => m.MessageId == messageId && !m.IsDeleted);

            if (originalMessage == null)
                return NotFound("Message not found");

            var neededPermissions = ChatPermissions.ForwardMessages | ChatPermissions.SendMessages;

            var permission = await _chatPermissionService.CanUserPerformAction(CurrentUserId, originalMessage.ChatId, neededPermissions);
            if (!permission)
                return Forbid("You don't have permission");

            var forwardedMessages = new List<MessageDto>();

            foreach (var targetChatId in request.TargetChatIds)
            {
                // Kiểm tra quyền gửi tin nhắn đến chat đích
                var targetMembership = await _context.ChatMembers
                    .FirstOrDefaultAsync(cm => cm.ChatId == targetChatId && cm.UserId == CurrentUserId && cm.IsActive);

                if (targetMembership == null)
                    continue;

                var forwardedMessage = new Message
                {
                    ChatId = targetChatId,
                    SenderId = CurrentUserId,
                    Content = originalMessage.Content,
                    MessageType = originalMessage.MessageType,
                    MediaFileIds = originalMessage.MediaFileIds,
                    ForwardedFromChatId = originalMessage.ChatId,
                    ForwardedFromMessageId = originalMessage.MessageId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(forwardedMessage);
                await _context.SaveChangesAsync();

                // Load message với related entities
                var messageWithRelations = await _context.Messages
                    .Where(m => m.MessageId == forwardedMessage.MessageId)
                    .Include(m => m.Sender)
                    .Include(m => m.ReplyToMessage)
                        .ThenInclude(rm => rm.Sender)
                    .FirstOrDefaultAsync();

                var mediaFiles = await _mediaService.GetMediaFilesAsync(messageWithRelations.MediaFileIds, CurrentUserId);

                var messageWithDetails = ConvertToMessageDto(messageWithRelations, mediaFiles);
                forwardedMessages.Add(messageWithDetails);
            }

            return Ok(new { forwardedMessages, count = forwardedMessages.Count });
        }

        private MessageDto ConvertToMessageDto(Message message, List<MediaFile>? mediaFiles)
        {
            if (message == null)
                return null;

            var dto = new MessageDto
            {
                MessageId = message.MessageId,
                ChatId = message.ChatId,
                Content = message.Content,
                MessageType = message.MessageType,
                CreatedAt = message.CreatedAt,
                UpdatedAt = message.UpdatedAt,
                IsEdited = message.IsEdited,
                IsDeleted = message.IsDeleted,
                Sender = message.Sender != null ? new UserDto
                {
                    Id = message.Sender.Id,
                    DisplayName = message.Sender.DisplayName,
                    AvatarUrl = message.Sender.AvatarUrl
                } : null,
                ReplyTo = message.ReplyToMessage != null ? new MessageDto
                {
                    MessageId = message.ReplyToMessage.MessageId,
                    Content = message.ReplyToMessage.Content,
                    Sender = message.ReplyToMessage.Sender != null ? new UserDto
                    {
                        Id = message.ReplyToMessage.Sender.Id,
                        DisplayName = message.ReplyToMessage.Sender.DisplayName,
                        AvatarUrl = message.ReplyToMessage.Sender.AvatarUrl
                    } : null
                } : null,
                IsForwarded = message.ForwardedFromMessageId.HasValue
            };
            var mediaFileModels = new List<MediaFileModel>();
            // Xử lý URL cho MediaFile
            if (mediaFiles != null)
            {
                var req = _httpContextAccessor.HttpContext?.Request;
                if (req != null)
                {
                    foreach (var media in mediaFiles)
                    {
                        var baseUrl = $"{req.Scheme}://{req.Host}";
                        mediaFileModels.Add(new MediaFileModel
                        {
                            FileId = media.FileId,
                            FileName = media.FileName,
                            OriginalFileName = media.OriginalFileName,
                            ContentType = media.ContentType,
                            FilePath = baseUrl + media.FilePath,
                            ThumbnailPath = media.ThumbnailPath = media.ThumbnailPath != null
                                            ? baseUrl + media.ThumbnailPath
                                            : null,
                            FileSize = media.FileSize
                        });
                    }
                }
            }
            dto.MediaFiles = mediaFileModels;

            // Xử lý URL cho Avatar của ReplyTo
            if (dto.ReplyTo?.Sender?.AvatarUrl != null)
            {
                var req = _httpContextAccessor.HttpContext?.Request;
                if (req != null)
                {
                    var baseUrl = $"{req.Scheme}://{req.Host}";
                    if (dto.ReplyTo.Sender.AvatarUrl.StartsWith("/"))
                        dto.ReplyTo.Sender.AvatarUrl = baseUrl + dto.ReplyTo.Sender.AvatarUrl;
                }
            }

            // Xử lý URL cho Avatar của Sender
            if (dto.Sender?.AvatarUrl != null)
            {
                var req = _httpContextAccessor.HttpContext?.Request;
                if (req != null)
                {
                    var baseUrl = $"{req.Scheme}://{req.Host}";
                    if (dto.Sender.AvatarUrl.StartsWith("/"))
                        dto.Sender.AvatarUrl = baseUrl + dto.Sender.AvatarUrl;
                }
            }

            return dto;
        }
    }
}