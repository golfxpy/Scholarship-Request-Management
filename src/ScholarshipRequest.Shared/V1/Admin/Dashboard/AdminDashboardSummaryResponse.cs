namespace ScholarshipRequest.Shared.V1.Admin.Dashboard;

public sealed record AdminDashboardTypeSummaryResponse(
    Guid ScholarshipTypeId,
    string ScholarshipTypeName,
    int RequestCount,
    decimal TotalRequestedAmount);

public sealed record AdminDashboardSummaryResponse(
    int TotalRequests,
    int PendingRequests,
    int ApprovedRequests,
    int RejectedRequests,
    decimal TotalRequestedAmount,
    IReadOnlyList<AdminDashboardTypeSummaryResponse> ByScholarshipType);
