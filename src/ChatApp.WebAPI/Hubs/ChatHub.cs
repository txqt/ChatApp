using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enum;
using ChatApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatApp.WebAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IApplicationDbContext _context;
        private readonly IUserService _userService;

        public ChatHub(IApplicationDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public override async Task OnConnectedAsync()
        {
            // Sync user và get current user
            var currentUser = await GetCurrentUser();

            // Update user online status
            currentUser.IsOnline = true;
            currentUser.LastSeenAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Join user vào các chat groups
            var userChats = await _context.ChatMembers
                .Where(cm => cm.UserId == currentUser.Id && cm.IsActive)
                .Select(cm => cm.ChatId.ToString())
                .ToListAsync();

            if(userChats.Any())
            {
                foreach (var chatId in userChats)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Chat_{chatId}");
                }

                // Notify other users that this user is online
                await Clients.All.SendAsync("UserOnline", new { userId = currentUser.Id, displayName = currentUser.DisplayName });
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var auth0Id = Context.User?.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(auth0Id))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == auth0Id);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastSeenAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    await Clients.All.SendAsync("UserOffline", new { userId = user.Id });
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(int chatId, string content, MessageType messageType = MessageType.Text, int? replyToMessageId = null)
        {
            var currentUser = await GetCurrentUser();

            // Kiểm tra user có quyền gửi tin nhắn không
            var membership = await _context.ChatMembers
                .FirstOrDefaultAsync(cm => cm.ChatId == chatId && cm.UserId == currentUser.Id && cm.IsActive);

            if (membership == null)
            {
                await Clients.Caller.SendAsync("Error", "You don't have permission to send messages to this chat");
                return;
            }

            // Tạo message
            var message = new Message
            {
                ChatId = chatId,
                SenderId = currentUser.Id,
                Content = content,
                MessageType = messageType,
                ReplyToMessageId = replyToMessageId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);

            // Update last message của chat
            var chat = await _context.Chats.FindAsync(chatId);
            if (chat != null)
            {
                chat.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Load message với related data
            var messageWithDetails = await _context.Messages
                .Where(m => m.MessageId == message.MessageId)
                .Include(m => m.Sender)
                .Include(m => m.ReplyToMessage)
                    .ThenInclude(rm => rm.Sender)
                .Select(m => new
                {
                    m.MessageId,
                    m.ChatId,
                    m.Content,
                    m.MessageType,
                    m.CreatedAt,
                    Sender = new
                    {
                        m.Sender.Id,
                        m.Sender.DisplayName,
                        m.Sender.AvatarUrl
                    },
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
                .FirstOrDefaultAsync();

            // Update last message ID
            if (chat != null)
            {
                chat.LastMessageId = message.MessageId;
                await _context.SaveChangesAsync();
            }

            // Gửi message đến tất cả members trong chat
            await Clients.Group($"Chat_{chatId}").SendAsync("ReceiveMessage", messageWithDetails);

            // Create message status cho tất cả members (trừ sender)
            var chatMembers = await _context.ChatMembers
                .Where(cm => cm.ChatId == chatId && cm.IsActive && cm.UserId != currentUser.Id)
                .Select(cm => cm.UserId)
                .ToListAsync();

            var messageStatuses = chatMembers.Select(userId => new MessageStatus
            {
                MessageId = message.MessageId,
                UserId = userId,
                Status = MessageStatusType.Sent,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            _context.MessageStatuses.AddRange(messageStatuses);
            await _context.SaveChangesAsync();
        }

        public async Task JoinChat(int chatId)
        {
            var currentUser = await GetCurrentUser();

            var membership = await _context.ChatMembers
                .FirstOrDefaultAsync(cm => cm.ChatId == chatId && cm.UserId == currentUser.Id && cm.IsActive);

            if (membership != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Chat_{chatId}");
                await Clients.Caller.SendAsync("JoinedChat", chatId);
            }
        }

        public async Task LeaveChat(int chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Chat_{chatId}");
            await Clients.Caller.SendAsync("LeftChat", chatId);
        }

        public async Task MarkAsRead(int chatId, int messageId)
        {
            var currentUser = await GetCurrentUser();

            // Update last read message
            var membership = await _context.ChatMembers
                .FirstOrDefaultAsync(cm => cm.ChatId == chatId && cm.UserId == currentUser.Id);

            if (membership != null)
            {
                membership.LastReadMessageId = messageId;
                membership.LastReadAt = DateTime.UtcNow;

                // Update message status
                var messageStatus = await _context.MessageStatuses
                    .FirstOrDefaultAsync(ms => ms.MessageId == messageId && ms.UserId == currentUser.Id);

                if (messageStatus != null)
                {
                    messageStatus.Status = MessageStatusType.Read;
                    messageStatus.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Notify sender về read status
                var message = await _context.Messages
                    .Include(m => m.Sender)
                    .FirstOrDefaultAsync(m => m.MessageId == messageId);

                if (message != null && message.SenderId != currentUser.Id)
                {
                    await Clients.User(message.Sender.UserName!)
                        .SendAsync("MessageRead", new { messageId, readBy = currentUser.Id, readAt = DateTime.UtcNow });
                }
            }
        }

        public async Task StartTyping(int chatId)
        {
            var currentUser = await GetCurrentUser();

            await Clients.GroupExcept($"Chat_{chatId}", Context.ConnectionId)
                .SendAsync("UserTyping", new { chatId, userId = currentUser.Id, displayName = currentUser.DisplayName });
        }

        public async Task StopTyping(int chatId)
        {
            var currentUser = await GetCurrentUser();

            await Clients.GroupExcept($"Chat_{chatId}", Context.ConnectionId)
                .SendAsync("UserStoppedTyping", new { chatId, userId = currentUser.Id });
        }

        public async Task<ApplicationUser> GetCurrentUser()
        {
            var auth0Id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? Context.User?.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(auth0Id))
            {
                throw new UnauthorizedAccessException("User is not authenticated");
            }

            var currentUser = await _userService.GetUserByIdAsync(auth0Id);
            if (currentUser is null)
            {
                throw new UnauthorizedAccessException("User is not authenticated");
            }

            return currentUser;
        }
    }
}