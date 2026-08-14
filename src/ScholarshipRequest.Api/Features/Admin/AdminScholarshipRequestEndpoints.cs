using Microsoft.EntityFrameworkCore;
using ScholarshipRequest.Api.Data;
using ScholarshipRequest.Api.Domain.ScholarshipRequests;
using ScholarshipRequest.Shared.V1.Admin.ScholarshipRequests;

namespace ScholarshipRequest.Api.Features.Admin;

public static class AdminScholarshipRequestEndpoints
{
    private const int PageSize = 10;
    private const int MaximumSearchLength = 100;

    public static RouteGroupBuilder MapAdminScholarshipRequestEndpoints(
        this RouteGroupBuilder group)
    {
        group.MapGet("/scholarship-requests", GetListAsync)
            .WithName("GetAdminScholarshipRequests");
        group.MapGet("/scholarship-requests/{id:guid}", GetDetailAsync)
            .WithName("GetAdminScholarshipRequestDetail");
        group.MapGet("/scholarship-types", GetScholarshipTypesAsync)
            .WithName("GetAdminScholarshipTypes");

        group.MapAdminScholarshipRequestMutationEndpoints();

        return group;
    }

    private static async Task<IResult> GetListAsync(
        HttpContext httpContext,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        SetNoStore(httpContext);
        var parsedQuery = ParseQuery(httpContext.Request.Query);
        if (parsedQuery.Errors.Count > 0)
        {
            return ValidationProblem(parsedQuery.Errors);
        }

        var query =
            from request in context.ScholarshipRequests.AsNoTracking()
            join scholarshipType in context.ScholarshipTypes.AsNoTracking()
                on request.ScholarshipTypeId equals scholarshipType.Id
            select new { Request = request, ScholarshipType = scholarshipType };

        if (!string.IsNullOrEmpty(parsedQuery.Search))
        {
            var pattern = $"%{EscapeLikePattern(parsedQuery.Search)}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.Request.RequestNumber, pattern, "\\") ||
                EF.Functions.ILike(item.Request.StudentId, pattern, "\\") ||
                EF.Functions.ILike(item.Request.StudentName, pattern, "\\"));
        }

        if (parsedQuery.Status is not null)
        {
            query = query.Where(item => item.Request.Status == parsedQuery.Status);
        }

        if (parsedQuery.ScholarshipTypeId is not null)
        {
            query = query.Where(item =>
                item.Request.ScholarshipTypeId == parsedQuery.ScholarshipTypeId);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var skip = ((long)parsedQuery.Page - 1) * PageSize;
        var items = skip > int.MaxValue
            ? []
            : await query
                .OrderByDescending(item => item.Request.SubmittedAt)
                .ThenByDescending(item => item.Request.RequestNumber)
                .Skip((int)skip)
                .Take(PageSize)
                .Select(item => new AdminScholarshipRequestListItemResponse(
                    item.Request.Id,
                    item.Request.RequestNumber,
                    item.Request.StudentId,
                    item.Request.StudentName,
                    item.Request.ScholarshipTypeId,
                    item.ScholarshipType.Name,
                    item.Request.RequestedAmount,
                    item.Request.Status.ToString(),
                    item.Request.SubmittedAt,
                    item.Request.Status == ScholarshipRequestStatus.Pending,
                    item.Request.Status == ScholarshipRequestStatus.Pending,
                    item.Request.Status == ScholarshipRequestStatus.Pending))
                .ToArrayAsync(cancellationToken);

        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (decimal)PageSize);
        return Results.Ok(new AdminScholarshipRequestListResponse(
            items,
            parsedQuery.Page,
            PageSize,
            totalItems,
            totalPages));
    }

    private static async Task<IResult> GetDetailAsync(
        Guid id,
        HttpContext httpContext,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        SetNoStore(httpContext);
        var detail = await (
            from request in context.ScholarshipRequests.AsNoTracking()
            join scholarshipType in context.ScholarshipTypes.AsNoTracking()
                on request.ScholarshipTypeId equals scholarshipType.Id
            join notice in context.PdpaNotices.AsNoTracking()
                on request.PdpaNoticeId equals notice.Id
            join decidedByUser in context.Users.AsNoTracking()
                on request.DecidedById equals (Guid?)decidedByUser.Id into decidedByUsers
            from decidedBy in decidedByUsers.DefaultIfEmpty()
            join createdByUser in context.Users.AsNoTracking()
                on request.CreatedById equals (Guid?)createdByUser.Id into createdByUsers
            from createdBy in createdByUsers.DefaultIfEmpty()
            where request.Id == id
            select new AdminScholarshipRequestDetailResponse(
                request.Id,
                request.RequestNumber,
                request.StudentId,
                request.StudentName,
                request.AcademicUnitId,
                request.FacultyNameSnapshot,
                request.Major,
                request.EducationLevel.ToString(),
                request.YearLevel,
                request.YearLevelOther,
                request.Gpax,
                request.Email,
                request.ScholarshipTypeId,
                scholarshipType.Code,
                scholarshipType.Name,
                request.RequestedAmount,
                "******" + request.BankAccountLastFour,
                request.Reason,
                request.Status.ToString(),
                request.DecisionNote,
                request.DecidedAt,
                request.DecidedById,
                decidedBy == null ? null : decidedBy.FullName,
                request.SubmissionSource.ToString(),
                request.CreatedById,
                createdBy == null ? null : createdBy.FullName,
                notice.Version,
                request.ConsentMethod.ToString(),
                request.ConsentEvidenceNote,
                request.ConsentObtainedAt,
                request.SubmittedAt,
                request.CreatedAt,
                request.UpdatedAt,
                request.Status == ScholarshipRequestStatus.Pending,
                request.Status == ScholarshipRequestStatus.Pending,
                request.Status == ScholarshipRequestStatus.Pending))
            .SingleOrDefaultAsync(cancellationToken);

        return detail is null
            ? Results.Problem(
                title: "ไม่พบคำขอทุนการศึกษา",
                detail: "คำขออาจถูกลบหรือไม่มีอยู่ในระบบ",
                statusCode: StatusCodes.Status404NotFound,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "SCHOLARSHIP_REQUEST_NOT_FOUND"
                })
            : Results.Ok(detail);
    }

    private static async Task<IResult> GetScholarshipTypesAsync(
        HttpContext httpContext,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        SetNoStore(httpContext);
        var items = await context.ScholarshipTypes
            .AsNoTracking()
            .OrderBy(type => type.SortOrder)
            .ThenBy(type => type.Name)
            .Select(type => new AdminScholarshipTypeOptionResponse(
                type.Id,
                type.Code,
                type.Name,
                type.IsActive))
            .ToArrayAsync(cancellationToken);
        return Results.Ok(items);
    }

    private static ParsedQuery ParseQuery(IQueryCollection query)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var page = 1;
        var pageValue = query["page"].ToString();
        if (!string.IsNullOrWhiteSpace(pageValue) &&
            (!int.TryParse(pageValue, out page) || page < 1))
        {
            errors["Page"] = ["หน้าต้องเป็นจำนวนเต็มตั้งแต่ 1 ขึ้นไป"];
        }

        var pageSizeValue = query["pageSize"].ToString();
        if (!string.IsNullOrWhiteSpace(pageSizeValue) &&
            (!int.TryParse(pageSizeValue, out var requestedPageSize) || requestedPageSize != PageSize))
        {
            errors["PageSize"] = ["ระบบกำหนดจำนวนรายการต่อหน้าไว้ที่ 10 รายการ"];
        }

        var search = query["search"].ToString().Trim();
        if (search.Length > MaximumSearchLength)
        {
            errors["Search"] = [$"ข้อความค้นหาต้องยาวไม่เกิน {MaximumSearchLength} ตัวอักษร"];
        }

        ScholarshipRequestStatus? status = null;
        var statusValue = query["status"].ToString().Trim();
        if (!string.IsNullOrEmpty(statusValue))
        {
            var statusName = Enum.GetNames<ScholarshipRequestStatus>()
                .SingleOrDefault(name => string.Equals(
                    name,
                    statusValue,
                    StringComparison.OrdinalIgnoreCase));
            if (statusName is not null)
            {
                status = Enum.Parse<ScholarshipRequestStatus>(statusName);
            }
            else
            {
                errors["Status"] = ["สถานะต้องเป็น Pending, Approved หรือ Rejected"];
            }
        }

        Guid? scholarshipTypeId = null;
        var scholarshipTypeValue = query["scholarshipTypeId"].ToString().Trim();
        if (!string.IsNullOrEmpty(scholarshipTypeValue))
        {
            if (Guid.TryParse(scholarshipTypeValue, out var parsedScholarshipTypeId) &&
                parsedScholarshipTypeId != Guid.Empty)
            {
                scholarshipTypeId = parsedScholarshipTypeId;
            }
            else
            {
                errors["ScholarshipTypeId"] = ["รหัสประเภททุนไม่ถูกต้อง"];
            }
        }

        return new ParsedQuery(page, search, status, scholarshipTypeId, errors);
    }

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private static IResult ValidationProblem(Dictionary<string, string[]> errors) =>
        Results.ValidationProblem(
            errors,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = "VALIDATION_FAILED"
            });

    private static void SetNoStore(HttpContext httpContext) =>
        httpContext.Response.Headers.CacheControl = "no-store";

    private sealed record ParsedQuery(
        int Page,
        string Search,
        ScholarshipRequestStatus? Status,
        Guid? ScholarshipTypeId,
        Dictionary<string, string[]> Errors);
}
