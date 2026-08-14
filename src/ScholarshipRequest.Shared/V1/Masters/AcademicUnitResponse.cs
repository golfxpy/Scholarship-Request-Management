namespace ScholarshipRequest.Shared.V1.Masters;

public sealed record AcademicUnitResponse(
    Guid Id,
    string Code,
    string Name,
    int SortOrder);
