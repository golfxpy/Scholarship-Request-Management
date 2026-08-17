using ScholarshipRequest.Shared.V1.ScholarshipRequests;

namespace ScholarshipRequest.Api.Features.PublicScholarshipRequests;

public sealed class PublicScholarshipRequestValidator
{
    public Dictionary<string, string[]> Validate(CreatePublicScholarshipRequest request) =>
        PublicScholarshipRequestRules.Validate(request);
}
