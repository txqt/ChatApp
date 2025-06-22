using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enum;
using ChatApp.Infrastructure.Services;
using ChatApp.WebAPI.Attributes;
using ChatApp.WebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ChatApp.WebAPI.Controllers
{
    public class ChatController : BaseController
    {
        private readonly IApplicationDbContext _context;
        private readonly IChatPermissionService _chatPermissionService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMediaService _mediaService;

        public ChatController(IApplicationDbContext context, IUserService userService, IChatPermissionService chatPermissionService, IHttpContextAccessor httpContextAccessor, IMediaService mediaService) : base(userService)
        {
            _context = context;
            _chatPermissionService = chatPermissionService;
            _httpContextAccessor = httpContextAccessor;
            _mediaService = mediaService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserChats()
        {
            // Step 1: Project basic chat info first (no async work)
            var chatEntities = await _context.ChatMembers
                .Where(cm => cm.UserId == CurrentUserId && cm.IsActive)
                .Include(cm => cm.Chat)
                    .ThenInclude(c => c.LastMessage)
                        .ThenInclude(lm => lm.Sender)
                .Include(cm => cm.Chat)
                    .ThenInclude(c => c.Members.Where(m => m.IsActive))
                        .ThenInclude(m => m.User)
                .ToListAsync();

            var req = _httpContextAccessor.HttpContext!.Request;
            var baseUrl = $"{req.Scheme}://{req.Host}";

            // Step 2: Map with async data (e.g. permissions)
            var chatsData = new List<ChatDto>();
            foreach (var cm in chatEntities)
            {
                var chat = cm.Chat;

                var chatDto = new ChatDto
                {
                    ChatId = chat.ChatId,
                    ChatName = chat.ChatName,
                    ChatType = chat.ChatType,
                    AvatarUrl = Prefix(baseUrl, chat.AvatarUrl),
                    CreatedAt = chat.CreatedAt,
                    UpdatedAt = chat.UpdatedAt,
                    Permissions = await _chatPermissionService.GetUserPermissions(CurrentUser, chat.ChatId),
                    LastMessage = chat.LastMessage == null ? null : new MessageDto
                    {
                        MessageId = chat.LastMessage.MessageId,
                        Content = chat.LastMessage.Content,
                        MessageType = chat.LastMessage.MessageType,
                        CreatedAt = chat.LastMessage.CreatedAt,
                        Sender = new UserDto
                        {
                            Id = chat.LastMessage.Sender.Id,
                            DisplayName = chat.LastMessage.Sender.DisplayName,
                            AvatarUrl = chat.LastMessage.Sender.AvatarUrl,
                            IsOnline = chat.LastMessage.Sender.IsOnline
                        }
                    },
                    UnreadCount = _context.Messages
                        .Count(msg => msg.ChatId == chat.ChatId &&
                                      msg.CreatedAt > (cm.LastReadAt ?? DateTime.MinValue) &&
                                      msg.SenderId != CurrentUserId),
                    LastReadAt = cm.LastReadAt,
                    Members = chat.Members
                        .Select(m => new UserDto
                        {
                            Id = m.User.Id,
                            DisplayName = m.User.DisplayName,
                            AvatarUrl = m.User.AvatarUrl,
                            IsOnline = m.User.IsOnline,
                            Role = m.Role,
                        })
                        .ToList(),
                    ChatRolePermissions = chat.RolePermissions.Select(rp => new ChatRolePermissionDto
                    {
                        Role = rp.Role,
                        Permissions = (ChatPermissions)rp.PermissionMask,
                    }).ToList(),
                    AllowMembersToAddOthers = chat.AllowMembersToAddOthers,
                    MaxMembers = chat.MaxMembers,
                };

                chatsData.Add(chatDto);
            }


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
                PermissionMask = (long)ChatPermissions.BasicMember, // Direct chat has basic permissions
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = CurrentUserId
            };
            _context.ChatRolePermissions.Add(chatRolePermission);

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

            await using var tx = await _context.BeginTransactionAsync();

            try
            {
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
                    Role = ChatMemberRole.Owner
                };
                _context.ChatMembers.Add(creatorMember);
                await _context.SaveChangesAsync();

                var rolePermissionsMap = new Dictionary<ChatMemberRole, ChatPermissions>
                {
                    { ChatMemberRole.Member, ChatPermissions.BasicMember },
                    { ChatMemberRole.Moderator, ChatPermissions.Moderator },
                    { ChatMemberRole.Admin, ChatPermissions.Admin },
                    { ChatMemberRole.Owner, ChatPermissions.Owner }
                };

                var now = DateTime.UtcNow;

                var rolePermissions = rolePermissionsMap.Select(rp => new ChatRolePermission
                {
                    ChatId = chat.ChatId,
                    Role = rp.Key,
                    PermissionMask = (long)rp.Value,
                    UpdatedAt = now,
                    UpdatedBy = CurrentUserId
                }).ToList();

                _context.ChatRolePermissions.AddRange(rolePermissions);
                await _context.SaveChangesAsync();

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

                await tx.CommitAsync();

                return Ok(new { chatId = chat.ChatId, message = "Group chat created successfully" });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();

                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = CurrentUserId,
                    Action = "CreateGroupChat_Failed",
                    EntityType = "Chat",
                    EntityId = null,
                    OldValues = null,
                    NewValues = JsonSerializer.Serialize(request),
                    Timestamp = DateTime.UtcNow,
                    IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString()
                });
                await _context.SaveChangesAsync();
                return StatusCode(500, "An error occurred while creating the group chat.");
            }
        }

        [HttpGet("{chatId}/messages")]
        public async Task<IActionResult> GetChatMessages(int chatId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (CurrentUser == null)
                return Unauthorized("You must be logged in to access chat messages");

            var canView = await _chatPermissionService
                .CanUserPerformAction(CurrentUser, chatId, ChatPermissions.ViewMessageHistory);
            if (!canView)
                return Forbid("You don't have permission to view messages in this chat");

            // 1) Lấy messages cơ bản, bao gồm cột JSON MediaFileIdsJson
            var raw = await _context.Messages
                .Where(m => m.ChatId == chatId && !m.IsDeleted)
                .Include(m => m.Sender)
                .Include(m => m.ReplyToMessage)
                    .ThenInclude(rm => rm.Sender)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    m.MessageId,
                    m.Content,
                    m.MessageType,
                    m.CreatedAt,
                    m.UpdatedAt,
                    m.IsEdited,
                    SenderId = m.Sender.Id,
                    SenderName = m.Sender.DisplayName,
                    SenderAvatar = m.Sender.AvatarUrl,
                    MediaFileIdsJson = m.MediaFileIdsJson,        // JSON string: "[1,5,9]"
                    ReplyTo = m.ReplyToMessage == null ? null : new
                    {
                        m.ReplyToMessage.MessageId,
                        m.ReplyToMessage.Content,
                        SenderId = m.ReplyToMessage.Sender.Id,
                        SenderName = m.ReplyToMessage.Sender.DisplayName,
                        SenderAvatar = m.ReplyToMessage.Sender.AvatarUrl
                    }
                })
                .ToListAsync();

            // 2) Tập hợp tất cả mediaId từ mọi message
            var allMediaIds = raw
                .Where(x => !string.IsNullOrEmpty(x.MediaFileIdsJson))
                .SelectMany(x => JsonSerializer.Deserialize<List<int>>(x.MediaFileIdsJson)!)
                .Distinct()
                .ToList();

            // 3) Query một lần để lấy các MediaFile
            var mediaEntities = await _context.MediaFiles
                .Where(f => allMediaIds.Contains(f.FileId))
                .ToDictionaryAsync(f => f.FileId);

            // 4) Lấy baseUrl
            var req = _httpContextAccessor.HttpContext!.Request;
            var baseUrl = $"{req.Scheme}://{req.Host}";

            // 5) Map về DTO và prefix đường dẫn
            var dtos = raw
                .Select(x =>
                {
                    // Parse media IDs
                    var ids = string.IsNullOrEmpty(x.MediaFileIdsJson)
                        ? new List<int>()
                        : JsonSerializer.Deserialize<List<int>>(x.MediaFileIdsJson)!;

                    // Lấy model tương ứng
                    var mediaList = ids
                        .Where(id => mediaEntities.ContainsKey(id))
                        .Select(id =>
                        {
                            var e = mediaEntities[id];
                            return new MediaFileModel
                            {
                                FileId = e.FileId,
                                FileName = e.FileName,
                                OriginalFileName = e.OriginalFileName,
                                ContentType = e.ContentType,
                                FileSize = e.FileSize,
                                FilePath = Prefix(baseUrl, e.FilePath),
                                ThumbnailPath = e.ThumbnailPath != null
                                                    ? Prefix(baseUrl, e.ThumbnailPath)
                                                    : null
                            };
                        })
                        .ToList();

                    MessageDto? replyDto = null;
                    if (x.ReplyTo != null)
                    {
                        replyDto = new MessageDto
                        {
                            MessageId = x.ReplyTo.MessageId,
                            Content = x.ReplyTo.Content,
                            Sender = new UserDto
                            {
                                Id = x.ReplyTo.SenderId,
                                DisplayName = x.ReplyTo.SenderName,
                                AvatarUrl = Prefix(baseUrl, x.ReplyTo.SenderAvatar)
                            }
                        };
                    }

                    return new MessageDto
                    {
                        MessageId = x.MessageId,
                        Content = x.Content,
                        MessageType = x.MessageType,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt,
                        IsEdited = x.IsEdited,
                        Sender = new UserDto
                        {
                            Id = x.SenderId,
                            DisplayName = x.SenderName,
                            AvatarUrl = Prefix(baseUrl, x.SenderAvatar)
                        },
                        MediaFiles = mediaList,
                        ReplyTo = replyDto,
                        IsForwarded = false
                    };
                })
                .OrderBy(m => m.CreatedAt)
                .ToList();

            return Ok(dtos);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateChat([FromForm] UpdateChatRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Kiểm tra quyền sửa thông tin nhóm
            var canEdit = await _chatPermissionService.CanUserPerformAction(CurrentUser, request.ChatId, ChatPermissions.EditGroupInfo);
            if (!canEdit)
                return Forbid("You don't have permission to edit this chat");

            var chat = await _context.Chats.FindAsync(request.ChatId);
            if (chat == null)
                return NotFound("Chat not found");

            // Check if request has data before binding to chat
            if (request.ChatName != null)
                chat.ChatName = request.ChatName;
            if (request.Description != null)
                chat.Description = request.Description;
            chat.AllowMembersToAddOthers = request.AllowMembersToAddOthers;
            chat.AllowMembersToEditInfo = request.AllowMembersToEditInfo;
            chat.MaxMembers = request.MaxMembers ?? 1000; // Default max members

            if (request.RolePermissions != null && request.RolePermissions.Any())
            {
                // Cập nhật quyền cho các role trong chat
                foreach (var rolePermission in request.RolePermissions)
                {
                    var existingRole = chat.RolePermissions.FirstOrDefault(rp => rp.Role == rolePermission.Role);
                    if (existingRole != null)
                    {
                        existingRole.PermissionMask = (long)rolePermission.Permissions;
                        existingRole.UpdatedAt = DateTime.UtcNow;
                        existingRole.UpdatedBy = CurrentUserId;
                    }
                }
            }

            if (request.Avatar != null)
            {
                if (request.Avatar.Length > 5 * 1024 * 1024)
                {
                    return BadRequest("File quá lớn. Kích thước tối đa 5MB");
                }

                var result = await _mediaService.SaveMediaFileAsync(request.Avatar, CurrentUserId);
                if (result != null)
                    chat.AvatarUrl = result.FilePath;
            }

            _context.Chats.Update(chat);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Chat info updated successfully" });
        }

        private static string Prefix(string baseUrl, string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath) || Uri.IsWellFormedUriString(relativePath, UriKind.Absolute))
                return relativePath!;
            return relativePath.StartsWith("/")
                ? baseUrl + relativePath
                : $"{baseUrl}/{relativePath}";
        }
    }
}