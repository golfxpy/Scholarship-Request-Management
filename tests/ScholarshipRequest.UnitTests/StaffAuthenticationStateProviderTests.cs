using ScholarshipRequest.Client.Features.Authentication;
using ScholarshipRequest.Shared.V1.Authentication;

namespace ScholarshipRequest.UnitTests;

public sealed class StaffAuthenticationStateProviderTests
{
    [Fact]
    public async Task GetAuthenticationStateAsync_ShouldMemoizeSessionAndCreateRoleClaims()
    {
        var session = CreateAuthenticatedSession();
        var api = new StubAuthenticationApi
        {
            SessionResult = AuthenticationApiResult<StaffSessionResponse>.Success(session)
        };
        var provider = new StaffAuthenticationStateProvider(api);

        var first = await provider.GetAuthenticationStateAsync();
        var second = await provider.GetAuthenticationStateAsync();

        Assert.Equal(1, api.SessionCalls);
        Assert.Same(first, second);
        Assert.True(first.User.Identity?.IsAuthenticated);
        Assert.True(first.User.IsInRole(AuthenticationConstants.StaffRole));
        Assert.Equal("admin", first.User.Identity?.Name);
        Assert.Equal(StaffAuthenticationAvailability.Available, provider.Availability);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ShouldKeepUnavailableDistinctFromAnonymous()
    {
        var api = new StubAuthenticationApi
        {
            SessionResult = AuthenticationApiResult<StaffSessionResponse>.Failure(
                new AuthenticationApiError(null, "NETWORK_ERROR", "offline"))
        };
        var provider = new StaffAuthenticationStateProvider(api);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
        Assert.Equal(StaffAuthenticationAvailability.Unavailable, provider.Availability);
        Assert.Null(provider.CurrentSession);
    }

    [Fact]
    public async Task LoginAndLogout_ShouldPublishAuthenticationStateChanges()
    {
        var api = new StubAuthenticationApi
        {
            SessionResult = AuthenticationApiResult<StaffSessionResponse>.Success(
                StaffSessionResponse.Anonymous),
            LoginResult = AuthenticationApiResult<StaffSessionResponse>.Success(
                CreateAuthenticatedSession()),
            LogoutResult = AuthenticationApiResult<bool>.Success(true)
        };
        var provider = new StaffAuthenticationStateProvider(api);
        var changes = new List<bool>();
        provider.AuthenticationStateChanged += async task =>
            changes.Add((await task).User.Identity?.IsAuthenticated == true);

        var login = await provider.LoginAsync(new StaffLoginRequest
        {
            UserName = "admin",
            Password = "test-only"
        });
        var logout = await provider.LogoutAsync();

        Assert.True(login.IsSuccess);
        Assert.True(logout.IsSuccess);
        Assert.Equal([true, false], changes);
        Assert.Null(provider.CurrentSession);
    }

    [Fact]
    public async Task OlderSessionResponse_ShouldNotOverwriteNewerLogin()
    {
        var pendingSession = new TaskCompletionSource<
            AuthenticationApiResult<StaffSessionResponse>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var api = new StubAuthenticationApi
        {
            PendingSession = pendingSession,
            LoginResult = AuthenticationApiResult<StaffSessionResponse>.Success(
                CreateAuthenticatedSession())
        };
        var provider = new StaffAuthenticationStateProvider(api);

        var oldSessionTask = provider.GetAuthenticationStateAsync();
        var login = await provider.LoginAsync(new StaffLoginRequest
        {
            UserName = "admin",
            Password = "test-only"
        });
        pendingSession.SetResult(
            AuthenticationApiResult<StaffSessionResponse>.Success(
                StaffSessionResponse.Anonymous));

        var staleCallerState = await oldSessionTask;
        var currentState = await provider.GetAuthenticationStateAsync();

        Assert.True(login.IsSuccess);
        Assert.True(staleCallerState.User.Identity?.IsAuthenticated);
        Assert.True(currentState.User.Identity?.IsAuthenticated);
        Assert.Equal("admin", provider.CurrentSession?.UserName);
    }

    [Fact]
    public async Task FailedLoginAndLogout_ShouldPreserveLastConfirmedSession()
    {
        var api = new StubAuthenticationApi
        {
            SessionResult = AuthenticationApiResult<StaffSessionResponse>.Success(
                CreateAuthenticatedSession()),
            LoginResult = AuthenticationApiResult<StaffSessionResponse>.Failure(
                new AuthenticationApiError(400, "ANTIFORGERY_VALIDATION_FAILED", "expired")),
            LogoutResult = AuthenticationApiResult<bool>.Failure(
                new AuthenticationApiError(null, "NETWORK_ERROR", "offline"))
        };
        var provider = new StaffAuthenticationStateProvider(api);
        Assert.True((await provider.GetAuthenticationStateAsync()).User.Identity?.IsAuthenticated);

        var login = await provider.LoginAsync(new StaffLoginRequest());
        var logout = await provider.LogoutAsync();
        var finalState = await provider.GetAuthenticationStateAsync();

        Assert.False(login.IsSuccess);
        Assert.False(logout.IsSuccess);
        Assert.True(finalState.User.Identity?.IsAuthenticated);
        Assert.Equal(StaffAuthenticationAvailability.Available, provider.Availability);
        Assert.NotNull(provider.CurrentSession);
    }

    private static StaffSessionResponse CreateAuthenticatedSession() =>
        new(
            true,
            Guid.Parse("dce15a74-7c8d-43b7-b8d2-cf51e135da4e"),
            "admin",
            "ผู้ดูแลระบบทดสอบ",
            [AuthenticationConstants.StaffRole]);

    private sealed class StubAuthenticationApi : IStaffAuthenticationApi
    {
        public int SessionCalls { get; private set; }

        public AuthenticationApiResult<StaffSessionResponse> SessionResult { get; set; } =
            AuthenticationApiResult<StaffSessionResponse>.Success(StaffSessionResponse.Anonymous);

        public AuthenticationApiResult<StaffSessionResponse> LoginResult { get; set; } =
            AuthenticationApiResult<StaffSessionResponse>.Failure(
                new AuthenticationApiError(401, "AUTH_INVALID_CREDENTIALS", "invalid"));

        public AuthenticationApiResult<bool> LogoutResult { get; set; } =
            AuthenticationApiResult<bool>.Success(true);

        public TaskCompletionSource<AuthenticationApiResult<StaffSessionResponse>>?
            PendingSession
        { get; set; }

        public Task<AuthenticationApiResult<StaffSessionResponse>> GetSessionAsync(
            CancellationToken cancellationToken = default)
        {
            SessionCalls++;
            return PendingSession?.Task ?? Task.FromResult(SessionResult);
        }

        public Task<AuthenticationApiResult<StaffSessionResponse>> LoginAsync(
            StaffLoginRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(LoginResult);

        public Task<AuthenticationApiResult<bool>> LogoutAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(LogoutResult);
    }
}
