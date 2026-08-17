using ScholarshipRequest.Shared.V1.Masters;
using ScholarshipRequest.Shared.V1.Privacy;
using ScholarshipRequest.Shared.V1.ScholarshipRequests;

namespace ScholarshipRequest.Client.Features.PublicScholarshipRequests;

public interface IPublicScholarshipApi
{
    Task<PublicApiResult<IReadOnlyList<ScholarshipTypeResponse>>> GetScholarshipTypesAsync(
        CancellationToken cancellationToken = default);

    Task<PublicApiResult<IReadOnlyList<AcademicUnitResponse>>> SearchAcademicUnitsAsync(
        string? query,
        CancellationToken cancellationToken = default);

    Task<PublicApiResult<PdpaNoticeResponse>> GetPdpaNoticeAsync(
        CancellationToken cancellationToken = default);

    Task<PublicApiResult<CreateScholarshipRequestResponse>> CreateRequestAsync(
        CreatePublicScholarshipRequest request,
        CancellationToken cancellationToken = default);
}
