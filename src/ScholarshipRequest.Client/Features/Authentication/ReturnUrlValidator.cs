namespace ScholarshipRequest.Client.Features.Authentication;

public static class ReturnUrlValidator
{
    public const string DefaultAdminPath = "/admin";

    public static string Sanitize(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return DefaultAdminPath;
        }

        var candidate = returnUrl.Trim();
        if (!candidate.StartsWith("/admin", StringComparison.Ordinal) ||
            candidate.StartsWith("//", StringComparison.Ordinal) ||
            candidate.Contains('\\') ||
            candidate.Any(char.IsControl) ||
            (candidate.Length > "/admin".Length &&
                candidate["/admin".Length] is not ('/' or '?' or '#')) ||
            Uri.TryCreate(candidate, UriKind.Absolute, out _))
        {
            return DefaultAdminPath;
        }

        return candidate;
    }
}
