namespace ScholarshipRequest.Shared.V1.ScholarshipRequests;

public sealed record CreateScholarshipRequestResponse(
    Guid Id,
    string RequestNumber,
    string Status,
    DateTimeOffset SubmittedAt);
