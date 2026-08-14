namespace ScholarshipRequest.Shared.V1.Authentication;

public static class AuthenticationConstants
{
    public const string StaffRole = "Staff";

    public const string StaffPolicy = "StaffOnly";

    public const string AntiforgeryHeaderName = "X-CSRF-TOKEN";
}
