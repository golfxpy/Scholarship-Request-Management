using System.Net;
using System.Net.Http.Json;
using ScholarshipRequest.Client.Features.Authentication;
using ScholarshipRequest.Shared.V1.Authentication;

namespace ScholarshipRequest.UnitTests;

public sealed class StaffAuthenticationApiTests
{
    [Fact]
    public async Task LoginAsync_ShouldFetchTokenAndAttachHeaderWithoutPersistingPassword()
    {
        var calls = 0;
        using var handler = new StubHttpMessageHandler((request, _) =>
        {
            calls++;
            if (calls == 1)
            {
                Assert.Equal("/api/v1/auth/antiforgery-token", request.RequestUri?.AbsolutePath);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new AntiforgeryTokenResponse(
                        "request-token",
                        AuthenticationConstants.AntiforgeryHeaderName))
                });
            }

            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/v1/auth/login", request.RequestUri?.AbsolutePath);
            Assert.True(request.Headers.TryGetValues(
                AuthenticationConstants.AntiforgeryHeaderName,
                out var values));
            Assert.Equal("request-token", Assert.Single(values));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new StaffSessionResponse(
                    true,
                    Guid.NewGuid(),
                    "admin",
                    "Admin",
                    [AuthenticationConstants.StaffRole]))
            });
        });
        using var client = CreateClient(handler);
        var api = new StaffAuthenticationApi(client);

        var result = await api.LoginAsync(new StaffLoginRequest
        {
            UserName = "admin",
            Password = "Scholarship@2569"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, calls);
        Assert.True(result.Value?.IsAuthenticated);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("[]")]
    [InlineData("null")]
    public async Task GetSessionAsync_ShouldUseSafeFallbackForMalformedPayload(string payload)
    {
        using var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload)
            }));
        using var client = CreateClient(handler);
        var api = new StaffAuthenticationApi(client);

        var result = await api.GetSessionAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_RESPONSE", result.Error?.Code);
    }

    [Fact]
    public async Task LogoutAsync_ShouldNotClaimSuccessOnNetworkFailure()
    {
        using var handler = new StubHttpMessageHandler(
            (_, _) => throw new HttpRequestException("test-only failure"));
        using var client = CreateClient(handler);
        var api = new StaffAuthenticationApi(client);

        var result = await api.LogoutAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("NETWORK_ERROR", result.Error?.Code);
    }

    private static HttpClient CreateClient(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://example.test") };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            sendAsync(request, cancellationToken);
    }
}
