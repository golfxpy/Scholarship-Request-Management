using Microsoft.EntityFrameworkCore;
using ScholarshipRequest.Api.Data;
using ScholarshipRequest.Api.Domain.ScholarshipRequests;
using ScholarshipRequest.Shared.V1.Admin.Dashboard;

namespace ScholarshipRequest.Api.Features.Admin;

public static class AdminDashboardEndpoints
{
    public static RouteGroupBuilder MapAdminDashboardEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/dashboard", GetDashboardAsync)
            .WithName("GetAdminDashboard");
        return group;
    }

    private static async Task<IResult> GetDashboardAsync(
        HttpContext httpContext,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.Headers.Pragma = "no-cache";

        var totals = await context.ScholarshipRequests
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Pending = group.Count(request =>
                    request.Status == ScholarshipRequestStatus.Pending),
                Approved = group.Count(request =>
                    request.Status == ScholarshipRequestStatus.Approved),
                Rejected = group.Count(request =>
                    request.Status == ScholarshipRequestStatus.Rejected),
                Amount = group.Sum(request => request.RequestedAmount)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var byScholarshipType = await (
            from request in context.ScholarshipRequests.AsNoTracking()
            join scholarshipType in context.ScholarshipTypes.AsNoTracking()
                on request.ScholarshipTypeId equals scholarshipType.Id
            group request by new { scholarshipType.Id, scholarshipType.Name, scholarshipType.SortOrder }
            into requestsByType
            orderby requestsByType.Key.SortOrder, requestsByType.Key.Name
            select new AdminDashboardTypeSummaryResponse(
                requestsByType.Key.Id,
                requestsByType.Key.Name,
                requestsByType.Count(),
                requestsByType.Sum(request => request.RequestedAmount)))
            .ToArrayAsync(cancellationToken);

        return Results.Ok(new AdminDashboardSummaryResponse(
            totals?.Total ?? 0,
            totals?.Pending ?? 0,
            totals?.Approved ?? 0,
            totals?.Rejected ?? 0,
            totals?.Amount ?? 0,
            byScholarshipType));
    }
}
