namespace ScholarshipRequest.Shared.V1.Authentication;

public sealed record AntiforgeryTokenResponse(
    string RequestToken,
    string HeaderName);
