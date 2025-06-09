using Blazored.Modal;
using ChatApp.BlazorApp;
using ChatApp.BlazorApp.Helpers;
using ChatApp.BlazorApp.Services;
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
string audience = auth0Section["Audience"]!;
string redirectUri = auth0Section["RedirectUri"]!;
string postLogoutRedirectUri = auth0Section["PostLogoutRedirectUri"]!;

builder.Services.AddOidcAuthentication(options =>
{
    options.ProviderOptions.Authority = authority;
    options.ProviderOptions.ClientId = clientId;
    options.ProviderOptions.ResponseType = "code";

    options.ProviderOptions.RedirectUri = redirectUri;
    options.ProviderOptions.PostLogoutRedirectUri = postLogoutRedirectUri;

    options.ProviderOptions.DefaultScopes.Add("openid");
    options.ProviderOptions.DefaultScopes.Add("profile");
    options.ProviderOptions.DefaultScopes.Add("email");
    options.ProviderOptions.DefaultScopes.Add("offline_access");

    // Nếu bạn dùng Audience để request Access Token cho API:
    options.ProviderOptions.AdditionalProviderParameters.Add("audience", audience);
});

builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddBlazoredModal();

await builder.Build().RunAsync();
