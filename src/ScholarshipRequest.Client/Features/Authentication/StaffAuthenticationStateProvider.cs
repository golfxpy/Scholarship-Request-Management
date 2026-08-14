using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using ScholarshipRequest.Shared.V1.Authentication;

namespace ScholarshipRequest.Client.Features.Authentication;

public sealed class StaffAuthenticationStateProvider(IStaffAuthenticationApi authenticationApi)
    : AuthenticationStateProvider
{
    private static readonly TimeSpan SessionVerificationLifetime = TimeSpan.FromMinutes(2);

    private static readonly AuthenticationState AnonymousState =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly object _sync = new();
    private Task<AuthenticationState>? _sessionTask;
    private long _generation;
    private DateTimeOffset _verifiedAt;

    public StaffAuthenticationAvailability Availability { get; private set; } =
        StaffAuthenticationAvailability.Loading;

    public StaffSessionResponse? CurrentSession { get; private set; }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        lock (_sync)
        {
            if (_sessionTask is null ||
                DateTimeOffset.UtcNow - _verifiedAt >= SessionVerificationLifetime)
            {
                var generation = ++_generation;
                Availability = StaffAuthenticationAvailability.Loading;
                _sessionTask = LoadSessionAsync(generation);
            }

            return _sessionTask;
        }
    }

    public async Task<AuthenticationApiResult<StaffSessionResponse>> LoginAsync(
        StaffLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var generation = BeginMutation();
        var result = await authenticationApi.LoginAsync(request, cancellationToken);
        if (result.IsSuccess && result.Value is { IsAuthenticated: true } session)
        {
            Publish(session, generation);
        }
        else if (result.Error?.StatusCode == 401 && CurrentSession is null)
        {
            Publish(StaffSessionResponse.Anonymous, generation);
        }
        else if (Availability == StaffAuthenticationAvailability.Loading)
        {
            await RetrySessionAsync();
        }

        return result;
    }

    public async Task<AuthenticationApiResult<bool>> LogoutAsync(
        CancellationToken cancellationToken = default)
    {
        var generation = BeginMutation();
        var result = await authenticationApi.LogoutAsync(cancellationToken);
        if (result.IsSuccess)
        {
            Publish(StaffSessionResponse.Anonymous, generation);
        }

        return result;
    }

    public async Task RetrySessionAsync()
    {
        Task<AuthenticationState> task;
        long generation;
        lock (_sync)
        {
            generation = ++_generation;
            Availability = StaffAuthenticationAvailability.Loading;
            CurrentSession = null;
            _verifiedAt = default;
            task = LoadSessionAsync(generation);
            _sessionTask = task;
        }

        NotifyAuthenticationStateChanged(task);
        await task;
    }

    public void InvalidateSession()
    {
        var generation = BeginMutation();
        Publish(StaffSessionResponse.Anonymous, generation);
    }

    private async Task<AuthenticationState> LoadSessionAsync()
    {
        long generation;
        lock (_sync)
        {
            generation = _generation;
        }

        return await LoadSessionAsync(generation);
    }

    private async Task<AuthenticationState> LoadSessionAsync(long generation)
    {
        var result = await authenticationApi.GetSessionAsync();
        if (!result.IsSuccess || result.Value is null)
        {
            lock (_sync)
            {
                if (generation != _generation)
                {
                    return _sessionTask is { IsCompletedSuccessfully: true }
                        ? _sessionTask.Result
                        : AnonymousState;
                }

                Availability = StaffAuthenticationAvailability.Unavailable;
                CurrentSession = null;
                _verifiedAt = default;
            }

            return AnonymousState;
        }

        var state = CreateState(result.Value);
        lock (_sync)
        {
            if (generation != _generation)
            {
                return _sessionTask is { IsCompletedSuccessfully: true }
                    ? _sessionTask.Result
                    : AnonymousState;
            }

            Availability = StaffAuthenticationAvailability.Available;
            CurrentSession = result.Value.IsAuthenticated ? result.Value : null;
            _verifiedAt = DateTimeOffset.UtcNow;
            _sessionTask = Task.FromResult(state);
        }

        return state;
    }

    private long BeginMutation()
    {
        lock (_sync)
        {
            return ++_generation;
        }
    }

    private void Publish(StaffSessionResponse session, long generation)
    {
        var state = CreateState(session);
        var stateTask = Task.FromResult(state);
        lock (_sync)
        {
            if (generation != _generation)
            {
                return;
            }

            Availability = StaffAuthenticationAvailability.Available;
            CurrentSession = session.IsAuthenticated ? session : null;
            _verifiedAt = DateTimeOffset.UtcNow;
            _sessionTask = stateTask;
        }

        NotifyAuthenticationStateChanged(stateTask);
    }

    private static AuthenticationState CreateState(StaffSessionResponse session)
    {
        if (!session.IsAuthenticated || session.UserId is null)
        {
            return AnonymousState;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserId.Value.ToString()),
            new(ClaimTypes.Name, session.UserName ?? string.Empty),
            new(ClaimTypes.GivenName, session.FullName ?? session.UserName ?? string.Empty)
        };
        claims.AddRange(session.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity(claims, "StaffCookie")));
    }
}
