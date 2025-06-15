using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enum;
using ChatApp.Infrastructure.Services;
using ChatApp.WebAPI.Controllers;
using ChatApp.WebAPI.Hubs;
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

        public MessageController(IApplicationDbContext context, IWebHostEnvironment environment, IHubContext<ChatHub> chatHubContext, IUserService userService, IChatPermissionService chatPermissionService) : base(userService)
        {
            _context = context;
            _environment = environment;
            _chatHubContext = chatHubContext;
            _chatPermissionService = chatPermissionService;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var permission = await _chatPermissionService.CanUserPerformAction(CurrentUserId, request.ChatId, ChatPermissions.SendMessages);
            if(!permission)
                return Forbid("You don't have permission to send messages in this chat");

            // Validate reply message nếu có
            if (request.ReplyToMessageId.HasValue)
            {
                var replyMessage = await _context.Messages
                    .FirstOrDefaultAsync(m => m.MessageId == request.ReplyToMessageId.Value && m.ChatId == request.ChatId);

                if (replyMessage == null)
                    return BadRequest("Reply message not found");
            }

            var message = new Message
            {
                ChatId = request.ChatId,
                SenderId = CurrentUserId,
                Content = request.Content,
                MessageType = request.MessageType,
                ReplyToMessageId = request.ReplyToMessageId,
                MediaFileId = request.MediaFileId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Update chat's last message
            var chat = await _context.Chats.FindAsync(request.ChatId);
            if (chat != null)
            {
                chat.UpdatedAt = DateTime.UtcNow;
                chat.LastMessageId = message.MessageId;
            }

            await _context.SaveChangesAsync();

            // Load message với details để return
            var messageWithDetails = await GetMessageWithDetails(message.MessageId);

            await _chatHubContext.Clients.Group("Chat_"+request.ChatId.ToString())
                .SendAsync("ReceiveMessage", messageWithDetails);

            return Ok(messageWithDetails);
        }

        [HttpPut("{messageId}")]
        public async Task<IActionResult> EditMessage(int messageId, [FromBody] EditMessageRequest request)
        {
            var message = await _context.Messages
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

            var messageWithDetails = await GetMessageWithDetails(messageId);
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

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            // Validate file size (max 50MB)
            if (file.Length > 50 * 1024 * 1024)
                return BadRequest("File size cannot exceed 50MB");

            // Validate file type
            var allowedTypes = new[] { "image/", "video/", "audio/", "application/pdf", "text/", "application/msword", "application/vnd.openxmlformats-officedocument" };
            if (!allowedTypes.Any(type => file.ContentType.StartsWith(type)))
                return BadRequest("File type not allowed");

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create MediaFile record
                var mediaFile = new MediaFile
                {
                    FileName = fileName,
                    OriginalFileName = file.FileName,
                    ContentType = file.ContentType,
                    FilePath = $"/uploads/{fileName}",
                    FileSize = file.Length,
                    UploadedBy = CurrentUserId,
                    UploadedAt = DateTime.UtcNow
                };

                // Generate thumbnail for images
                if (file.ContentType.StartsWith("image/"))
                {
                    await GenerateThumbnail(filePath, fileName);
                    mediaFile.ThumbnailPath = $"/uploads/thumbnails/{fileName}";
                }

                _context.MediaFiles.Add(mediaFile);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    fileId = mediaFile.FileId,
                    fileName = mediaFile.FileName,
                    originalFileName = mediaFile.OriginalFileName,
                    filePath = mediaFile.FilePath,
                    thumbnailPath = mediaFile.ThumbnailPath,
                    contentType = mediaFile.ContentType,
                    fileSize = mediaFile.FileSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error uploading file: {ex.Message}");
            }
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
                .Include(m => m.MediaFile)
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
                    MediaFileId = originalMessage.MediaFileId,
                    ForwardedFromChatId = originalMessage.ChatId,
                    ForwardedFromMessageId = originalMessage.MessageId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(forwardedMessage);
                await _context.SaveChangesAsync();

                var messageWithDetails = await GetMessageWithDetails(forwardedMessage.MessageId);
                forwardedMessages.Add(messageWithDetails);
            }

            return Ok(new { forwardedMessages, count = forwardedMessages.Count });
        }

        private async Task<MessageDto?> GetMessageWithDetails(int messageId)
        {
            return await _context.Messages
                .Where(m => m.MessageId == messageId)
                .Include(m => m.Sender)
                .Include(m => m.MediaFile)
                .Include(m => m.ReplyToMessage)
                    .ThenInclude(rm => rm.Sender)
                .Select(m => new MessageDto
                {
                    MessageId = m.MessageId,
                    ChatId = m.ChatId,
                    Content = m.Content,
                    MessageType = m.MessageType,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt,
                    IsEdited = m.IsEdited,
                    IsDeleted = m.IsDeleted,
                    Sender = new UserDto
                    {
                        Id = m.Sender.Id,
                        DisplayName = m.Sender.DisplayName,
                        AvatarUrl = m.Sender.AvatarUrl
                    },
                    MediaFile = m.MediaFile != null ? new MediaFileModel
                    {
                        FileId = m.MediaFile.FileId,
                        FileName = m.MediaFile.FileName,
                        OriginalFileName = m.MediaFile.OriginalFileName,
                        ContentType = m.MediaFile.ContentType,
                        FilePath = m.MediaFile.FilePath,
                        ThumbnailPath = m.MediaFile.ThumbnailPath,
                        FileSize = m.MediaFile.FileSize
                    } : null,
                    ReplyTo = m.ReplyToMessage != null ? new MessageDto
                    {
                        MessageId = m.ReplyToMessage.MessageId,
                        Content = m.ReplyToMessage.Content,
                        Sender = new UserDto
                        {
                            Id = m.ReplyToMessage.Sender.Id,
                            DisplayName = m.ReplyToMessage.Sender.DisplayName,
                            AvatarUrl = m.ReplyToMessage.Sender.AvatarUrl
                        }
                    } : null,
                    IsForwarded = m.ForwardedFromMessageId.HasValue
                })
                .FirstOrDefaultAsync();
        }

        private async Task GenerateThumbnail(string filePath, string fileName)
        {
            try
            {
                var thumbnailsFolder = Path.Combine(_environment.WebRootPath, "uploads", "thumbnails");
                if (!Directory.Exists(thumbnailsFolder))
                    Directory.CreateDirectory(thumbnailsFolder);

                // Simple thumbnail generation - in production use ImageSharp or similar
                var thumbnailPath = Path.Combine(thumbnailsFolder, fileName);
                System.IO.File.Copy(filePath, thumbnailPath, true);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the upload
                Console.WriteLine($"Error generating thumbnail: {ex.Message}");
            }
        }
    }
}