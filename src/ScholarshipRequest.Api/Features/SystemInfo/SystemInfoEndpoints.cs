using ScholarshipRequest.Shared.V1.SystemInfo;

namespace ScholarshipRequest.Api.Features.SystemInfo;

public static class SystemInfoEndpoints
{
    public static IEndpointRouteBuilder MapSystemInfoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system")
            .WithTags("System");

        group.MapGet("/info", () =>
                TypedResults.Ok(new SystemInfoResponse(
                    ApplicationName: "Scholarship Request Management",
                    ApiVersion: "v1")))
            .WithName("GetSystemInfo");

        return endpoints;
    }
}
