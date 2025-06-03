using ChatApp.BlazorApp;
using ChatApp.BlazorApp.Helpers;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<AuthHttpClientHandler>();

builder.Services.AddHttpClient("ApiServer", client =>
{
    client.BaseAddress = new Uri("https://localhost:5000/"); // API URL
})
.AddHttpMessageHandler<AuthHttpClientHandler>();

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiServer"));


var auth0Section = builder.Configuration.GetSection("Auth0");
string authority = auth0Section["Authority"]!;
string clientId = auth0Section["ClientId"]!;
string audience = auth0Section["Audience"]!;             // nếu có
string redirectUri = auth0Section["RedirectUri"]!;       // "https://localhost:5001/authentication/login-callback"
string postLogoutRedirectUri = auth0Section["PostLogoutRedirectUri"]!;

builder.Services.AddOidcAuthentication(options =>
{
    // Bắt buộc: Authority + ClientId
    options.ProviderOptions.Authority = authority;  // "https://brochat.us.auth0.com"
    options.ProviderOptions.ClientId = clientId;   // "dSYcc1KnaYGjIfXF7TLDoL8ASYLyySUx"
    options.ProviderOptions.ResponseType = "code";

    // Khai báo redirectUri / postLogoutRedirectUri
    options.ProviderOptions.RedirectUri = redirectUri;
    options.ProviderOptions.PostLogoutRedirectUri = postLogoutRedirectUri;

    // Thêm các scope bắt buộc (Auth0 yêu cầu ít nhất "openid")
    options.ProviderOptions.DefaultScopes.Add("openid");
    options.ProviderOptions.DefaultScopes.Add("profile");
    options.ProviderOptions.DefaultScopes.Add("email");

    // Nếu bạn dùng Audience để request Access Token cho API:
    options.ProviderOptions.AdditionalProviderParameters.Add("audience", audience);
});

await builder.Build().RunAsync();
