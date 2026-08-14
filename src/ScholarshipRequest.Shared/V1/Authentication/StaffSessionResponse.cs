namespace ScholarshipRequest.Shared.V1.Authentication;

public sealed record StaffSessionResponse(
    bool IsAuthenticated,
    Guid? UserId,
    string? UserName,
    string? FullName,
    string[] Roles)
{
    public static StaffSessionResponse Anonymous { get; } =
        new(false, null, null, null, []);
}
