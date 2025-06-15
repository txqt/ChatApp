using ChatApp.Application.DTOs;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;
using System.Text.Json;

namespace ChatApp.BlazorApp.Services
{
    public interface IChatService
    {
        Task<List<ChatDto>> GetUserChatsAsync();
        Task<List<MessageDto>> GetChatMessagesAsync(int chatId, int page = 1);
        Task<MessageDto> SendMessageAsync(SendMessageRequest request);
        Task<ChatDto> CreateDirectChatAsync(string userId);
        Task<ChatDto> CreateGroupChatAsync(CreateGroupChatRequest request);
        Task<MediaFileModel> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task<bool> EditMessageAsync(int messageId, string content);
        Task<bool> DeleteMessageAsync(int messageId, bool deleteForEveryone = false);
        Task<List<MessageDto>> SearchMessagesAsync(int chatId, string query);

        // SignalR Events
        event Action<MessageDto>? OnMessageReceived;
        event Action<int, string>? OnUserTyping;
        event Action<int>? OnUserStoppedTyping;
        event Action<string, bool>? OnUserOnlineStatusChanged;
        event Action<int, int, DateTime>? OnMessageRead;

        Task StartConnectionAsync();
        Task StopConnectionAsync();
        Task SendMessageViaSignalRAsync(int chatId, string content, int? replyToMessageId = null);
        Task MarkAsReadAsync(int chatId, int messageId);
        Task StartTypingAsync(int chatId);
        Task StopTypingAsync(int chatId);

        public bool IsConnected { get;}
    }

    public class ChatService : IChatService, IAsyncDisposable
    {
        private readonly HttpClient _httpClient;
        private HubConnection? _hubConnection;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IAccessTokenProvider _accessTokenProvider;

        public bool IsConnected { get; private set; }

        public ChatService(HttpClient httpClient, IAccessTokenProvider accessTokenProvider)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _accessTokenProvider = accessTokenProvider;
        }

        // Events
        public event Action<MessageDto>? OnMessageReceived;
        public event Action<int, string>? OnUserTyping;
        public event Action<int>? OnUserStoppedTyping;
        public event Action<string, bool>? OnUserOnlineStatusChanged;
        public event Action<int, int, DateTime>? OnMessageRead;
        public event Action? ConnectionStateChanged;

        public async Task<List<ChatDto>> GetUserChatsAsync()
        {
            var response = await _httpClient.GetAsync("/api/chat");
            response.EnsureSuccessStatusCode();

            var chats = await response.Content.ReadFromJsonAsync<List<ChatDto>>(_jsonOptions);
            return chats ?? new List<ChatDto>();
        }

        public async Task<List<MessageDto>> GetChatMessagesAsync(int chatId, int page = 1)
        {
            var response = await _httpClient.GetAsync($"/api/chat/{chatId}/messages?page={page}");
            response.EnsureSuccessStatusCode();

            var messages = await response.Content.ReadFromJsonAsync<List<MessageDto>>(_jsonOptions);
            
            return messages ?? new List<MessageDto>();
        }

        public async Task<MessageDto> SendMessageAsync(SendMessageRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/message", request);
            response.EnsureSuccessStatusCode();

            var message = await response.Content.ReadFromJsonAsync<MessageDto>(_jsonOptions);
            return message!;
        }

        public async Task<ChatDto> CreateDirectChatAsync(string userId)
        {
            var request = new { UserId = userId };
            var response = await _httpClient.PostAsJsonAsync("/api/chat/direct", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<dynamic>(_jsonOptions);
            // Return basic chat model, will be refreshed from GetUserChats
            return new ChatDto { ChatId = result?.chatId ?? 0 };
        }

        public async Task<ChatDto> CreateGroupChatAsync(CreateGroupChatRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/chat/group", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<dynamic>(_jsonOptions);
            return new ChatDto { ChatId = result?.chatId ?? 0 };
        }

        public async Task<MediaFileModel> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync("/api/message/upload", content);
            response.EnsureSuccessStatusCode();

            var mediaFile = await response.Content.ReadFromJsonAsync<MediaFileModel>(_jsonOptions);
            return mediaFile!;
        }

        public async Task<bool> EditMessageAsync(int messageId, string content)
        {
            var request = new { Content = content };
            var response = await _httpClient.PutAsJsonAsync($"/api/message/{messageId}", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteMessageAsync(int messageId, bool deleteForEveryone = false)
        {
            var response = await _httpClient.DeleteAsync($"/api/message/{messageId}?deleteForEveryone={deleteForEveryone}");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<MessageDto>> SearchMessagesAsync(int chatId, string query)
        {
            var response = await _httpClient.GetAsync($"/api/message/search?chatId={chatId}&query={Uri.EscapeDataString(query)}");
            response.EnsureSuccessStatusCode();

            var messages = await response.Content.ReadFromJsonAsync<List<MessageDto>>(_jsonOptions);
            return messages ?? new List<MessageDto>();
        }

        // SignalR Methods
        public async Task StartConnectionAsync()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("https://localhost:5000/chathub", options =>
                {
                    options.AccessTokenProvider = async () => await GetToken();
                })
                .Build();

            // Register event handlers
            _hubConnection.On<MessageDto>("ReceiveMessage", message =>
            {
                OnMessageReceived?.Invoke(message);
            });

            _hubConnection.On<dynamic>("UserTyping", data =>
            {
                OnUserTyping?.Invoke((int)data.chatId, (string)data.displayName);
            });

            _hubConnection.On<dynamic>("UserStoppedTyping", data =>
            {
                OnUserStoppedTyping?.Invoke((int)data.chatId);
            });

            _hubConnection.On<dynamic>("UserOnline", data =>
            {
                OnUserOnlineStatusChanged?.Invoke((string)data.userId, true);
            });

            _hubConnection.On<dynamic>("UserOffline", data =>
            {
                OnUserOnlineStatusChanged?.Invoke((string)data.userId, false);
            });

            _hubConnection.On<dynamic>("MessageRead", data =>
            {
                OnMessageRead?.Invoke((int)data.messageId, (int)data.readBy, DateTime.Parse((string)data.readAt));
            });

            _hubConnection.Reconnecting += error =>
            {
                IsConnected = false;
                ConnectionStateChanged?.Invoke();
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                IsConnected = true;
                ConnectionStateChanged?.Invoke();
                return Task.CompletedTask;
            };

            _hubConnection.Closed += async error =>
            {
                IsConnected = false;
                ConnectionStateChanged?.Invoke();

                await Task.Delay(5000);
                await StartConnectionAsync();
            };

            await _hubConnection.StartAsync();
            IsConnected = true;
            ConnectionStateChanged?.Invoke();
        }

        public async Task StopConnectionAsync()
        {
            if (_hubConnection is not null)
            {
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }

        public async Task SendMessageViaSignalRAsync(int chatId, string content, int? replyToMessageId = null)
        {
            if (_hubConnection is not null)
            {
                await _hubConnection.SendAsync("SendMessage", chatId, content, Domain.Enum.MessageType.Text, replyToMessageId);
            }
        }

        public async Task MarkAsReadAsync(int chatId, int messageId)
        {
            if (_hubConnection is not null)
            {
                await _hubConnection.SendAsync("MarkAsRead", chatId, messageId);
            }
        }

        public async Task StartTypingAsync(int chatId)
        {
            if (_hubConnection is not null)
            {
                await _hubConnection.SendAsync("StartTyping", chatId);
            }
        }

        public async Task StopTypingAsync(int chatId)
        {
            if (_hubConnection is not null)
            {
                await _hubConnection.SendAsync("StopTyping", chatId);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopConnectionAsync();
        }

        private async Task<string> GetToken()
        {
            var result = await _accessTokenProvider.RequestAccessToken();
            if (result.TryGetToken(out var token))
            {
                return token.Value;
            }
            Console.WriteLine("Không thể lấy token truy cập.");
            return string.Empty;
        }
    }
}