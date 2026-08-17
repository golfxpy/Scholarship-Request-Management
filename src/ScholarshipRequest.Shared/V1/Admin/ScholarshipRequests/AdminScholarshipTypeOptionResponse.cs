namespace ScholarshipRequest.Shared.V1.Admin.ScholarshipRequests;

public sealed record AdminScholarshipTypeOptionResponse(
    Guid Id,
    string Code,
    string Name,
    bool IsActive);
