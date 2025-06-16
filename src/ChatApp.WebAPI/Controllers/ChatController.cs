using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enum;
using ChatApp.Infrastructure.Services;
using ChatApp.WebAPI.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.WebAPI.Controllers
{
    public class ChatController : BaseController
    {
        private readonly IApplicationDbContext _context;
        private readonly IChatPermissionService _chatPermissionService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ChatController(IApplicationDbContext context, IUserService userService, IChatPermissionService chatPermissionService, IHttpContextAccessor httpContextAccessor) : base(userService)
        {
            _context = context;
            _chatPermissionService = chatPermissionService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserChats()
        {
            var chatsData = await _context.ChatMembers
                        .Where(cm => cm.UserId == CurrentUserId && cm.IsActive)
                        .Include(cm => cm.Chat)
                            .ThenInclude(c => c.LastMessage)
                                .ThenInclude(lm => lm.Sender)
                        .Include(cm => cm.Chat)
                            .ThenInclude(c => c.Members.Where(m => m.IsActive))
                                .ThenInclude(m => m.User)
                        .Select(cm => new ChatDto
                        {
                            ChatId = cm.Chat.ChatId,
                            ChatName = cm.Chat.ChatName,
                            ChatType = cm.Chat.ChatType,
                            AvatarUrl = cm.Chat.AvatarUrl,
                            CreatedAt = cm.Chat.CreatedAt,
                            UpdatedAt = cm.Chat.UpdatedAt,
                            LastMessage = cm.Chat.LastMessage == null ? null : new MessageDto
                            {
                                MessageId = cm.Chat.LastMessage.MessageId,
                                Content = cm.Chat.LastMessage.Content,
                                MessageType = cm.Chat.LastMessage.MessageType,
                                CreatedAt = cm.Chat.LastMessage.CreatedAt,
                                Sender = new UserDto
                                {
                                    Id = cm.Chat.LastMessage.Sender.Id,
                                    DisplayName = cm.Chat.LastMessage.Sender.DisplayName,
                                    AvatarUrl = cm.Chat.LastMessage.Sender.AvatarUrl,
                                    IsOnline = cm.Chat.LastMessage.Sender.IsOnline
                                }
                            },
                            UnreadCount = _context.Messages
                                .Where(msg => msg.ChatId == cm.Chat.ChatId &&
                                               msg.CreatedAt > (cm.LastReadAt ?? DateTime.MinValue) &&
                                               msg.SenderId != CurrentUserId)
                                .Count(),
                            LastReadAt = cm.LastReadAt,
                            IsMuted = cm.IsMuted,
                            Members = cm.Chat.Members
                                .Select(m => new UserDto
                                {
                                    Id = m.User.Id,
                                    DisplayName = m.User.DisplayName,
                                    AvatarUrl = m.User.AvatarUrl,
                                    IsOnline = m.User.IsOnline
                                })
                                .ToList()
                        })
                        .ToListAsync();

            var chats = chatsData
                .OrderByDescending(c => c.LastMessage?.CreatedAt ?? c.CreatedAt)
                .ToList();


            return Ok(chats);
        }

        [HttpPost("direct")]
        [RequirePermission(AppPermissions.CreateDirectChat)]
        public async Task<IActionResult> CreateDirectChat([FromBody] CreateDirectChatRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Kiểm tra user tồn tại
            var targetUser = await _context.Users.FindAsync(request.UserId);
            if (targetUser == null)
                return NotFound("User not found");

            var currentUser = await _userService.GetCurrentUserAsync();

            // Kiểm tra đã có direct chat chưa
            var existingChat = await _context.ChatMembers
                .Where(cm => cm.UserId == CurrentUser.Id)
                .Join(_context.ChatMembers.Where(cm2 => cm2.UserId == request.UserId),
                      cm1 => cm1.ChatId,
                      cm2 => cm2.ChatId,
                      (cm1, cm2) => cm1.Chat)
                .Where(c => c.ChatType == ChatType.Direct)
                .FirstOrDefaultAsync();

            if (existingChat != null)
            {
                return Ok(new { chatId = existingChat.ChatId, message = "Chat already exists" });
            }

            // Tạo direct chat mới
            var chat = new Chat
            {
                ChatType = ChatType.Direct,
                CreatedBy = CurrentUser.Id,
                IsActive = true
            };

            
            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();

            var chatRolePermission = new ChatRolePermission
            {
                ChatId = chat.ChatId,
                Role = ChatMemberRole.Member,
                PermissionMask = (long)ChatPermissions.BasicMember // Direct chat has basic permissions
            };

            // Add members
            var members = new[]
            {
                new ChatMember { ChatId = chat.ChatId, UserId = CurrentUser.Id, Role = ChatMemberRole.Member },
                new ChatMember { ChatId = chat.ChatId, UserId = request.UserId, Role = ChatMemberRole.Member }
            };

            _context.ChatMembers.AddRange(members);
            await _context.SaveChangesAsync();

            return Ok(new { chatId = chat.ChatId, message = "Direct chat created successfully" });
        }

        [HttpPost("group")]
        [RequirePermission(AppPermissions.CreateGroup)]
        public async Task<IActionResult> CreateGroupChat([FromBody] CreateGroupChatRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUser = await _userService.GetCurrentUserAsync();

            var chat = new Chat
            {
                ChatType = ChatType.Group,
                ChatName = request.ChatName,
                Description = request.Description,
                CreatedBy = CurrentUser.Id,
                AllowMembersToAddOthers = request.AllowMembersToAddOthers,
                AllowMembersToEditInfo = request.AllowMembersToEditInfo,
                MaxMembers = request.MaxMembers ?? 1000,
                IsActive = true
            };

            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();

            // Add creator as admin
            var creatorMember = new ChatMember
            {
                ChatId = chat.ChatId,
                UserId = CurrentUser.Id,
                Role = ChatMemberRole.Admin
            };
            _context.ChatMembers.Add(creatorMember);

            // Add other members
            if (request.MemberIds?.Any() == true)
            {
                var members = request.MemberIds.Select(userId => new ChatMember
                {
                    ChatId = chat.ChatId,
                    UserId = userId,
                    Role = ChatMemberRole.Member,
                    AddedBy = CurrentUser.Id
                }).ToList();

                _context.ChatMembers.AddRange(members);
            }

            await _context.SaveChangesAsync();

            return Ok(new { chatId = chat.ChatId, message = "Group chat created successfully" });
        }

        [HttpGet("{chatId}/messages")]
        public async Task<IActionResult> GetChatMessages(int chatId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if(CurrentUser == null)
                return Unauthorized("You must be logged in to access chat messages");

            var permissions = await _chatPermissionService.CanUserPerformAction(CurrentUser, chatId, ChatPermissions.ViewMessageHistory);
            if (!permissions)
                return Forbid("You don't have permission to view messages in this chat");

            var messages = await _context.Messages
            .Where(m => m.ChatId == chatId && !m.IsDeleted)
            .Include(m => m.Sender)
            .Include(m => m.MediaFile)
            .Include(m => m.ReplyToMessage)
                .ThenInclude(rm => rm.Sender)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MessageDto
            {
                MessageId = m.MessageId,
                Content = m.Content,
                MessageType = m.MessageType,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt,
                IsEdited = m.IsEdited,
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
                    ContentType = m.MediaFile.ContentType,
                    FileSize = m.MediaFile.FileSize,
                    FilePath = m.MediaFile.FilePath,
                    ThumbnailPath = m.MediaFile.ThumbnailPath
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
                } : null
            })
            .ToListAsync();

            // 2) Build baseUrl
            var req = _httpContextAccessor.HttpContext!.Request;
            var baseUrl = $"{req.Scheme}://{req.Host}";

            // 3) Prefix tất cả các đường dẫn tương đối
            foreach (var msg in messages)
            {
                // MediaFile
                if (msg.MediaFile != null)
                {
                    msg.MediaFile.FilePath = Prefix(baseUrl, msg.MediaFile.FilePath);
                    msg.MediaFile.ThumbnailPath = Prefix(baseUrl, msg.MediaFile.ThumbnailPath);
                }

                // Sender avatar
                if (!string.IsNullOrEmpty(msg.Sender.AvatarUrl))
                    msg.Sender.AvatarUrl = Prefix(baseUrl, msg.Sender.AvatarUrl);

                // ReplyTo sender avatar
                if (msg.ReplyTo?.Sender != null && !string.IsNullOrEmpty(msg.ReplyTo.Sender.AvatarUrl))
                    msg.ReplyTo.Sender.AvatarUrl = Prefix(baseUrl, msg.ReplyTo.Sender.AvatarUrl);
            }

            // 4) Đảo lại thứ tự tăng dần trước khi trả về
            var ordered = messages.OrderBy(m => m.CreatedAt).ToList();
            return Ok(ordered);
        }

        private static string? Prefix(string baseUrl, string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath) || Uri.IsWellFormedUriString(relativePath, UriKind.Absolute))
                return relativePath;
            // đảm bảo chỉ gắn 1 lần
            return relativePath.StartsWith("/")
                 ? baseUrl + relativePath
                 : $"{baseUrl}/{relativePath}";
        }
    }
}