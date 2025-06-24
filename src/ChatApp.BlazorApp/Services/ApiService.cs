using ChatApp.Contracts.DTOs;
using System.Net.Http.Json;

namespace ChatApp.BlazorApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<UserInfoDto> GetUserProfileAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<UserInfoDto>("api/user/profile");
            return response;
        }

        public async Task<UserInfoDto> GetMeAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<UserInfoDto>("api/users/me");
            return response;
        }

        public async Task<List<FriendDto>> GetMyFriends()
        {
            var response = await _httpClient.GetFromJsonAsync<List<FriendDto>>("api/users/me/friends");
            return response;
        }
    }
}
