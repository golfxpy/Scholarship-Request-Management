namespace ScholarshipRequest.Shared.V1.Masters;

public sealed record ScholarshipTypeResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    int SortOrder);
