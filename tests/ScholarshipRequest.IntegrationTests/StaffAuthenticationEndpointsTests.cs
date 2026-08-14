using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ScholarshipRequest.Api.Data.Identity;
using ScholarshipRequest.Shared.V1.Authentication;

namespace ScholarshipRequest.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class StaffAuthenticationEndpointsTests(
    PostgreSqlFixture database,
    WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ValidPassword = "Testing@2026!";

    [Fact]
    public async Task AnonymousSession_ShouldBeAnonymous_AndAdminShouldReturn401WithoutRedirect()
    {
        using var application = CreateApplication();
        using var client = CreateClient(application);

        var session = await client.GetFromJsonAsync<StaffSessionResponse>("/api/v1/auth/session");
        using var adminResponse = await client.GetAsync("/api/v1/admin/context");

        Assert.NotNull(session);
        Assert.False(session.IsAuthenticated);
        Assert.Null(session.UserId);
        Assert.Empty(session.Roles);
        Assert.Equal(HttpStatusCode.Unauthorized, adminResponse.StatusCode);
        Assert.Null(adminResponse.Headers.Location);
    }

    [Fact]
    public async Task LoginWithoutAntiforgeryToken_ShouldReturn400WithoutCheckingPassword()
    {
        using var application = CreateApplication();
        var user = await CreateUserAsync(application, isStaff: true, isActive: true);
        using var client = CreateClient(application);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new StaffLoginRequest { UserName = user.UserName!, Password = "wrong-password" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(response, "ANTIFORGERY_VALIDATION_FAILED");

        using var scope = application.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var persistedUser = await userManager.FindByIdAsync(user.Id.ToString());
        Assert.NotNull(persistedUser);
        Assert.Equal(0, persistedUser.AccessFailedCount);
    }

    [Fact]
    public async Task InvalidInactiveAndNonStaffLogin_ShouldUseSameGenericResponse()
    {
        using var application = CreateApplication();
        var staff = await CreateUserAsync(application, isStaff: true, isActive: true);
        var inactive = await CreateUserAsync(application, isStaff: true, isActive: false);
        var nonStaff = await CreateUserAsync(application, isStaff: false, isActive: true);
        using var client = CreateClient(application);
        var token = await GetAntiforgeryTokenAsync(client);

        var attempts = new[]
        {
            new StaffLoginRequest { UserName = staff.UserName!, Password = "wrong-password" },
            new StaffLoginRequest { UserName = $"missing-{Guid.NewGuid():N}", Password = ValidPassword },
            new StaffLoginRequest { UserName = inactive.UserName!, Password = ValidPassword },
            new StaffLoginRequest { UserName = nonStaff.UserName!, Password = ValidPassword }
        };

        foreach (var attempt in attempts)
        {
            using var response = await PostWithAntiforgeryAsync(
                client,
                "/api/v1/auth/login",
                attempt,
                token);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            await AssertProblemCodeAsync(response, "AUTH_INVALID_CREDENTIALS");
        }
    }

    [Fact]
    public async Task SuccessfulLogin_ShouldCreateSecureSession_RequireFreshTokenForLogout()
    {
        using var application = CreateApplication();
        var user = await CreateUserAsync(application, isStaff: true, isActive: true);
        using var client = CreateClient(application);
        var anonymousToken = await GetAntiforgeryTokenAsync(client);

        using var loginResponse = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/auth/login",
            new StaffLoginRequest { UserName = user.UserName!, Password = ValidPassword },
            anonymousToken);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginSession = await loginResponse.Content.ReadFromJsonAsync<StaffSessionResponse>();
        AssertAuthenticatedSession(loginSession, user);

        var authCookie = Assert.Single(
            loginResponse.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith("ScholarshipRequest.Auth=", StringComparison.Ordinal));
        var normalizedCookie = authCookie.ToLowerInvariant();
        Assert.Contains("httponly", normalizedCookie, StringComparison.Ordinal);
        Assert.Contains("samesite=strict", normalizedCookie, StringComparison.Ordinal);
        Assert.Contains("path=/", normalizedCookie, StringComparison.Ordinal);
        Assert.Contains("secure", normalizedCookie, StringComparison.Ordinal);
        Assert.DoesNotContain("domain=", normalizedCookie, StringComparison.Ordinal);
        Assert.DoesNotContain("expires=", normalizedCookie, StringComparison.Ordinal);
        Assert.DoesNotContain("max-age=", normalizedCookie, StringComparison.Ordinal);

        var session = await client.GetFromJsonAsync<StaffSessionResponse>("/api/v1/auth/session");
        AssertAuthenticatedSession(session, user);

        using var adminResponse = await client.GetAsync("/api/v1/admin/context");
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);

        using var staleTokenLogout = await PostWithAntiforgeryAsync<object?>(
            client,
            "/api/v1/auth/logout",
            null,
            anonymousToken);
        Assert.Equal(HttpStatusCode.BadRequest, staleTokenLogout.StatusCode);
        await AssertProblemCodeAsync(staleTokenLogout, "ANTIFORGERY_VALIDATION_FAILED");

        session = await client.GetFromJsonAsync<StaffSessionResponse>("/api/v1/auth/session");
        Assert.True(session!.IsAuthenticated);

        var authenticatedToken = await GetAntiforgeryTokenAsync(client);
        using var logoutResponse = await PostWithAntiforgeryAsync<object?>(
            client,
            "/api/v1/auth/logout",
            null,
            authenticatedToken);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        session = await client.GetFromJsonAsync<StaffSessionResponse>("/api/v1/auth/session");
        Assert.NotNull(session);
        Assert.False(session.IsAuthenticated);
        using var adminAfterLogout = await client.GetAsync("/api/v1/admin/context");
        Assert.Equal(HttpStatusCode.Unauthorized, adminAfterLogout.StatusCode);
    }

    [Fact]
    public async Task RepeatedPasswordFailures_ShouldLockAccount()
    {
        using var application = CreateApplication();
        var user = await CreateUserAsync(application, isStaff: true, isActive: true);
        using var client = CreateClient(application);
        var token = await GetAntiforgeryTokenAsync(client);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var response = await PostWithAntiforgeryAsync(
                client,
                "/api/v1/auth/login",
                new StaffLoginRequest { UserName = user.UserName!, Password = "wrong-password" },
                token);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        using (var scope = application.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var persistedUser = await userManager.FindByIdAsync(user.Id.ToString());
            Assert.NotNull(persistedUser);
            Assert.True(await userManager.IsLockedOutAsync(persistedUser));
        }

        using var correctPasswordResponse = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/auth/login",
            new StaffLoginRequest { UserName = user.UserName!, Password = ValidPassword },
            token);
        Assert.Equal(HttpStatusCode.Unauthorized, correctPasswordResponse.StatusCode);
        await AssertProblemCodeAsync(correctPasswordResponse, "AUTH_INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task DeactivatedStaffSession_ShouldLoseAdminAccessImmediately()
    {
        using var application = CreateApplication();
        var user = await CreateUserAsync(application, isStaff: true, isActive: true);
        using var client = CreateClient(application);
        var token = await GetAntiforgeryTokenAsync(client);
        using var loginResponse = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/auth/login",
            new StaffLoginRequest { UserName = user.UserName!, Password = ValidPassword },
            token);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using (var scope = application.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var persistedUser = await userManager.FindByIdAsync(user.Id.ToString());
            Assert.NotNull(persistedUser);
            persistedUser.IsActive = false;
            persistedUser.UpdatedAt = DateTimeOffset.UtcNow;
            Assert.True((await userManager.UpdateAsync(persistedUser)).Succeeded);
            await userManager.UpdateSecurityStampAsync(persistedUser);
        }

        using var adminResponse = await client.GetAsync("/api/v1/admin/context");
        Assert.Equal(HttpStatusCode.Forbidden, adminResponse.StatusCode);

        var session = await client.GetFromJsonAsync<StaffSessionResponse>("/api/v1/auth/session");
        Assert.NotNull(session);
        Assert.False(session.IsAuthenticated);
        using var adminAfterSessionRefresh = await client.GetAsync("/api/v1/admin/context");
        Assert.Equal(HttpStatusCode.Unauthorized, adminAfterSessionRefresh.StatusCode);
    }

    [Fact]
    public async Task Passwords_ShouldBeSaltedAndNeverStoredAsPlainText()
    {
        using var application = CreateApplication();
        var first = await CreateUserAsync(application, isStaff: false, isActive: true);
        var second = await CreateUserAsync(application, isStaff: false, isActive: true);

        Assert.NotNull(first.PasswordHash);
        Assert.NotNull(second.PasswordHash);
        Assert.NotEqual(ValidPassword, first.PasswordHash);
        Assert.NotEqual(first.PasswordHash, second.PasswordHash);

        using var scope = application.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.True(await userManager.CheckPasswordAsync(first, ValidPassword));
        Assert.False(await userManager.CheckPasswordAsync(first, "wrong-password"));
    }

    [Fact]
    public void AdminEndpoints_ShouldAllCarryStaffAuthorizationMetadata()
    {
        using var application = CreateApplication();
        var dataSource = application.Services.GetRequiredService<EndpointDataSource>();
        var adminEndpoints = dataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith(
                "/api/v1/admin",
                StringComparison.Ordinal) == true)
            .ToArray();

        Assert.NotEmpty(adminEndpoints);
        Assert.All(adminEndpoints, endpoint =>
        {
            Assert.Null(endpoint.Metadata.GetMetadata<IAllowAnonymous>());
            Assert.Contains(
                endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>(),
                authorization => string.Equals(
                    authorization.Policy,
                    AuthenticationConstants.StaffPolicy,
                    StringComparison.Ordinal));
        });
    }

    private WebApplicationFactory<Program> CreateApplication() =>
        factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Testing")
                .UseSetting("ConnectionStrings:DefaultConnection", database.ConnectionString));

    private static HttpClient CreateClient(WebApplicationFactory<Program> application) =>
        application.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });

    private static async Task<ApplicationUser> CreateUserAsync(
        WebApplicationFactory<Program> application,
        bool isStaff,
        bool isActive)
    {
        using var scope = application.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (isStaff && !await roleManager.RoleExistsAsync(AuthenticationConstants.StaffRole))
        {
            var roleResult = await roleManager.CreateAsync(
                new IdentityRole<Guid>(AuthenticationConstants.StaffRole));
            Assert.True(roleResult.Succeeded, FormatErrors(roleResult));
        }

        var now = DateTimeOffset.UtcNow;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"staff-{Guid.NewGuid():N}",
            FullName = "Integration Test Staff",
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        var createResult = await userManager.CreateAsync(user, ValidPassword);
        Assert.True(createResult.Succeeded, FormatErrors(createResult));

        if (isStaff)
        {
            var roleResult = await userManager.AddToRoleAsync(user, AuthenticationConstants.StaffRole);
            Assert.True(roleResult.Succeeded, FormatErrors(roleResult));
        }

        return user;
    }

    private static async Task<AntiforgeryTokenResponse> GetAntiforgeryTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/v1/auth/antiforgery-token");
        response.EnsureSuccessStatusCode();
        Assert.Contains(
            "no-store",
            response.Headers.CacheControl?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        var token = await response.Content.ReadFromJsonAsync<AntiforgeryTokenResponse>();
        return Assert.IsType<AntiforgeryTokenResponse>(token);
    }

    private static async Task<HttpResponseMessage> PostWithAntiforgeryAsync<T>(
        HttpClient client,
        string requestUri,
        T content,
        AntiforgeryTokenResponse token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(content)
        };
        request.Headers.TryAddWithoutValidation(token.HeaderName, token.RequestToken);
        return await client.SendAsync(request);
    }

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
    }

    private static void AssertAuthenticatedSession(
        StaffSessionResponse? session,
        ApplicationUser user)
    {
        Assert.NotNull(session);
        Assert.True(session.IsAuthenticated);
        Assert.Equal(user.Id, session.UserId);
        Assert.Equal(user.UserName, session.UserName);
        Assert.Equal(user.FullName, session.FullName);
        Assert.Contains(AuthenticationConstants.StaffRole, session.Roles);
    }

    private static string FormatErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
}
