using Microsoft.AspNetCore.Identity;
using ScholarshipRequest.Api.Data.Identity;
using ScholarshipRequest.Api.Features.StaffAuthentication;
using ScholarshipRequest.Api.Security;
using ScholarshipRequest.Shared.V1.Authentication;

namespace ScholarshipRequest.Api.Features.Admin;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin")
            .WithTags("Admin")
            .RequireAuthorization(AuthenticationConstants.StaffPolicy)
            .AddEndpointFilter<AntiforgeryValidationFilter>();

        group.MapGet("/context", GetContextAsync)
            .WithName("GetAdminContext");

        group.MapAdminDashboardEndpoints();
        group.MapAdminScholarshipRequestEndpoints();

        return endpoints;
    }

    private static async Task<IResult> GetContextAsync(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager)
    {
        httpContext.Response.Headers.CacheControl = "no-store";
        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(
            await StaffAuthenticationEndpoints.CreateSessionAsync(userManager, user));
    }
}
