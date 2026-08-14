namespace ScholarshipRequest.Shared.V1.Admin.ScholarshipRequests;

public sealed record AdminScholarshipRequestListItemResponse(
    Guid Id,
    string RequestNumber,
    string StudentId,
    string StudentName,
    Guid ScholarshipTypeId,
    string ScholarshipTypeName,
    decimal RequestedAmount,
    string Status,
    DateTimeOffset SubmittedAt,
    bool CanEdit,
    bool CanDelete,
    bool CanDecide);

public sealed record AdminScholarshipRequestListResponse(
    IReadOnlyList<AdminScholarshipRequestListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
