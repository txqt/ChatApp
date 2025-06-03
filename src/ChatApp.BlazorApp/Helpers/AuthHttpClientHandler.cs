using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Net.Http.Headers;

namespace ChatApp.BlazorApp.Helpers
{
    public class AuthHttpClientHandler : DelegatingHandler
    {
        private readonly IAccessTokenProvider _tokenService;

        public AuthHttpClientHandler(IAccessTokenProvider tokenService)
        {
            _tokenService = tokenService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var accessToken = await GetToken();

            if (!string.IsNullOrEmpty(accessToken))
            {
                if (!accessToken.StartsWith("Bearer "))
                {
                    accessToken = "Bearer " + accessToken;
                }

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Replace("Bearer ", ""));
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private async Task<string> GetToken()
        {
            var result = await _tokenService.RequestAccessToken();
            if (result.TryGetToken(out var token))
            {
                return token.Value;
            }
            return null;
        }
    }
}
