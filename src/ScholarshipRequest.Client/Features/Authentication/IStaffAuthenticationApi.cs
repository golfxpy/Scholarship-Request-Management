using ScholarshipRequest.Shared.V1.Authentication;

namespace ScholarshipRequest.Client.Features.Authentication;

public interface IStaffAuthenticationApi
{
    Task<AuthenticationApiResult<StaffSessionResponse>> GetSessionAsync(
        CancellationToken cancellationToken = default);

    Task<AuthenticationApiResult<StaffSessionResponse>> LoginAsync(
        StaffLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthenticationApiResult<bool>> LogoutAsync(
        CancellationToken cancellationToken = default);
}
