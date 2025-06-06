using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enum;
using ChatApp.WebAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.WebAPI.Controllers
{
    public class ChatController : BaseController
    {
        private readonly IApplicationDbContext _context;

        public ChatController(IApplicationDbContext context)
        {
            _context = context;
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
    .Select(cm => new
    {
        cm.Chat.ChatId,
        cm.Chat.ChatName,
        cm.Chat.ChatType,
        cm.Chat.AvatarUrl,
        cm.Chat.CreatedAt,
        cm.Chat.UpdatedAt,
        LastMessage = cm.Chat.LastMessage != null ? new
        {
            cm.Chat.LastMessage.MessageId,
            cm.Chat.LastMessage.Content,
            cm.Chat.LastMessage.MessageType,
            cm.Chat.LastMessage.CreatedAt,
            Sender = new
            {
                cm.Chat.LastMessage.Sender.Id,
                cm.Chat.LastMessage.Sender.DisplayName,
                cm.Chat.LastMessage.Sender.AvatarUrl
            }
        } : null,
        Members = cm.Chat.Members.Select(m => new
        {
            m.User.Id,
            m.User.DisplayName,
            m.User.AvatarUrl,
            m.User.IsOnline,
            m.Role
        }),
        UnreadCount = _context.Messages
            .Where(msg => msg.ChatId == cm.Chat.ChatId &&
                   msg.CreatedAt > (cm.LastReadAt ?? DateTime.MinValue) &&
                   msg.SenderId != CurrentUserId)
            .Count(),
        cm.LastReadAt,
        cm.IsMuted
    })
    .ToListAsync();

            var chats = chatsData
                .OrderByDescending(c => c.LastMessage?.CreatedAt ?? c.CreatedAt)
                .ToList();

            return Ok(chats);
        }

        [HttpPost("direct")]
        public async Task<IActionResult> CreateDirectChat([FromBody] CreateDirectChatRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Kiểm tra user tồn tại
            var targetUser = await _context.Users.FindAsync(request.UserId);
            if (targetUser == null)
                return NotFound("User not found");

            // Kiểm tra đã có direct chat chưa
            var existingChat = await _context.ChatMembers
                .Where(cm => cm.UserId == CurrentUserId)
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
                CreatedBy = CurrentUserId,
                IsActive = true
            };

            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();

            // Add members
            var members = new[]
            {
                new ChatMember { ChatId = chat.ChatId, UserId = CurrentUserId, Role = ChatMemberRole.Member },
                new ChatMember { ChatId = chat.ChatId, UserId = request.UserId, Role = ChatMemberRole.Member }
            };

            _context.ChatMembers.AddRange(members);
            await _context.SaveChangesAsync();

            return Ok(new { chatId = chat.ChatId, message = "Direct chat created successfully" });
        }

        [HttpPost("group")]
        public async Task<IActionResult> CreateGroupChat([FromBody] CreateGroupChatRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var chat = new Chat
            {
                ChatType = ChatType.Group,
                ChatName = request.ChatName,
                Description = request.Description,
                CreatedBy = CurrentUserId,
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
                UserId = CurrentUserId,
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
                    AddedBy = CurrentUserId
                }).ToList();

                _context.ChatMembers.AddRange(members);
            }

            await _context.SaveChangesAsync();

            return Ok(new { chatId = chat.ChatId, message = "Group chat created successfully" });
        }

        [HttpGet("{chatId}/messages")]
        public async Task<IActionResult> GetChatMessages(int chatId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            // Kiểm tra user có quyền xem chat không
            var membership = await _context.ChatMembers
                .FirstOrDefaultAsync(cm => cm.ChatId == chatId && cm.UserId == CurrentUserId && cm.IsActive);

            if (membership == null)
                return Forbid("You don't have access to this chat");

            var messages = await _context.Messages
                .Where(m => m.ChatId == chatId && !m.IsDeleted)
                .Include(m => m.Sender)
                .Include(m => m.MediaFile)
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
                    Sender = new
                    {
                        m.Sender.Id,
                        m.Sender.DisplayName,
                        m.Sender.AvatarUrl
                    },
                    MediaFile = m.MediaFile != null ? new
                    {
                        m.MediaFile.FileId,
                        m.MediaFile.FileName,
                        m.MediaFile.ContentType,
                        m.MediaFile.FileSize,
                        m.MediaFile.ThumbnailPath
                    } : null,
                    ReplyTo = m.ReplyToMessage != null ? new
                    {
                        m.ReplyToMessage.MessageId,
                        m.ReplyToMessage.Content,
                        Sender = new
                        {
                            m.ReplyToMessage.Sender.Id,
                            m.ReplyToMessage.Sender.DisplayName
                        }
                    } : null
                })
                .ToListAsync();

            return Ok(messages.OrderBy(m => m.CreatedAt));
        }
    }

    public class CreateDirectChatRequest
    {
        public int UserId { get; set; }
    }

    public class CreateGroupChatRequest
    {
        public string ChatName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<int>? MemberIds { get; set; }
        public bool AllowMembersToAddOthers { get; set; } = true;
        public bool AllowMembersToEditInfo { get; set; } = false;
        public int? MaxMembers { get; set; }
    }
}