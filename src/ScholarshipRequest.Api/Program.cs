using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarshipRequest.Api.Data;
using ScholarshipRequest.Api.Data.Identity;
using ScholarshipRequest.Api.Features.Admin;
using ScholarshipRequest.Api.Features.PublicScholarshipRequests;
using ScholarshipRequest.Api.Features.StaffAuthentication;
using ScholarshipRequest.Api.Features.SystemInfo;
using ScholarshipRequest.Api.Security;
using ScholarshipRequest.Api.Time;
using ScholarshipRequest.Shared.V1.Authentication;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection is required. Set ConnectionStrings__DefaultConnection.");
}

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"]
    ?? Path.Combine(Path.GetTempPath(), "scholarship-request-management", "keys");
Directory.CreateDirectory(dataProtectionKeysPath);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services
    .AddDataProtection()
    .SetApplicationName("ScholarshipRequestManagement")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
builder.Services.AddSingleton<IBankAccountProtector, DataProtectionBankAccountProtector>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.Configure<DevelopmentDemoSeedOptions>(
    builder.Configuration.GetSection(DevelopmentDemoSeedOptions.SectionName));
builder.Services.AddScoped<DevelopmentDemoDataSeeder>();
builder.Services.AddScoped<AdminScholarshipRequestValidator>();
builder.Services.AddScoped<PublicScholarshipRequestValidator>();
builder.Services.AddScoped<IRequestNumberGenerator, RequestNumberGenerator>();
builder.Services.AddScoped<AntiforgeryValidationFilter>();
builder.Services.AddScoped<IAuthorizationHandler, StaffAccessHandler>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("postgresql");
builder.Services.AddAntiforgery(options =>
{
    var allowsLocalHttp = builder.Environment.IsDevelopment() ||
        builder.Environment.IsEnvironment("Testing");
    options.HeaderName = AuthenticationConstants.AntiforgeryHeaderName;
    options.Cookie.Name = allowsLocalHttp
        ? "ScholarshipRequest.Antiforgery"
        : "__Host-ScholarshipRequest.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Path = "/";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = allowsLocalHttp
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthenticationConstants.StaffPolicy,
        policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new StaffAccessRequirement()));
});
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = false;
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(2);
});

builder.Services.ConfigureApplicationCookie(options =>
{
    var allowsLocalHttp = builder.Environment.IsDevelopment() ||
        builder.Environment.IsEnvironment("Testing");
    options.Cookie.Name = allowsLocalHttp
        ? "ScholarshipRequest.Auth"
        : "__Host-ScholarshipRequest.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Path = "/";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = allowsLocalHttp
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready");
if (!app.Environment.IsEnvironment("Testing"))
{
    await app.Services.MigrateDatabaseAsync();
}

if (app.Environment.IsDevelopment())
{
    await app.Services.SeedDevelopmentDemoDataAsync();
}

app.MapSystemInfoEndpoints();

app.MapPublicEndpoints();
app.MapStaffAuthenticationEndpoints();
app.MapAdminEndpoints();
app.Run();

public partial class Program;
