using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using ScholarshipRequest.Client;
using ScholarshipRequest.Client.Features.Admin.ScholarshipRequests;
using ScholarshipRequest.Client.Features.Authentication;
using ScholarshipRequest.Client.Features.PublicScholarshipRequests;
using ScholarshipRequest.Shared.V1.Authentication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient(
    new CookieCredentialsHandler(new HttpClientHandler()))
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});
builder.Services.AddMudServices();
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy(
        AuthenticationConstants.StaffPolicy,
        policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(AuthenticationConstants.StaffRole));
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IAdminScholarshipRequestApi, AdminScholarshipRequestApi>();
builder.Services.AddScoped<IPublicScholarshipApi, PublicScholarshipApi>();
builder.Services.AddScoped<IStaffAuthenticationApi, StaffAuthenticationApi>();
builder.Services.AddScoped<StaffAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(services =>
    services.GetRequiredService<StaffAuthenticationStateProvider>());

await builder.Build().RunAsync();
