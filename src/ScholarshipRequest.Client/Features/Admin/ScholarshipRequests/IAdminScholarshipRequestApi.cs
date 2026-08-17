using ScholarshipRequest.Shared.V1.Admin.Dashboard;
using ScholarshipRequest.Shared.V1.Admin.ScholarshipRequests;
using ScholarshipRequest.Shared.V1.ScholarshipRequests;

namespace ScholarshipRequest.Client.Features.Admin.ScholarshipRequests;

public interface IAdminScholarshipRequestApi
{
    Task<AdminApiResult<AdminDashboardSummaryResponse>> GetDashboardAsync(
        CancellationToken cancellationToken = default);

    Task<AdminApiResult<AdminScholarshipRequestListResponse>> GetListAsync(
        AdminScholarshipRequestQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminApiResult<AdminScholarshipRequestDetailResponse>> GetDetailAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AdminApiResult<IReadOnlyList<AdminScholarshipTypeOptionResponse>>> GetScholarshipTypesAsync(
        CancellationToken cancellationToken = default);

    Task<AdminApiResult<CreateScholarshipRequestResponse>> CreateAsync(
        CreateAdminScholarshipRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminApiResult<bool>> UpdateAsync(
        Guid id,
        UpdateAdminScholarshipRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminApiResult<bool>> DeleteAsync(
        Guid id,
        DateTimeOffset expectedUpdatedAt,
        CancellationToken cancellationToken = default);

    Task<AdminApiResult<bool>> DecideAsync(
        Guid id,
        AdminScholarshipRequestDecisionRequest request,
        CancellationToken cancellationToken = default);
}
