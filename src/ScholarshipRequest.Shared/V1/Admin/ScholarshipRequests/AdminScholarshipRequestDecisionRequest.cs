namespace ScholarshipRequest.Shared.V1.Admin.ScholarshipRequests;

public sealed class AdminScholarshipRequestDecisionRequest
{
    public DateTimeOffset? ExpectedUpdatedAt { get; set; }

    public string Decision { get; set; } = string.Empty;

    public string? Note { get; set; }
}
