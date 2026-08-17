using Microsoft.EntityFrameworkCore;
using ScholarshipRequest.Api.Data;
using ScholarshipRequest.Api.Domain.Privacy;
using ScholarshipRequest.Api.Domain.ScholarshipRequests;
using ScholarshipRequest.Api.Security;
using ScholarshipRequest.Api.Time;
using ScholarshipRequest.Shared.V1.Masters;
using ScholarshipRequest.Shared.V1.Privacy;
using ScholarshipRequest.Shared.V1.ScholarshipRequests;
using ScholarshipRequestEntity = ScholarshipRequest.Api.Domain.ScholarshipRequests.ScholarshipRequest;

namespace ScholarshipRequest.Api.Features.PublicScholarshipRequests;

public static class PublicEndpoints
{
    public static IEndpointRouteBuilder MapPublicEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/public")
            .WithTags("Public");

        group.MapGet("/scholarship-types", GetScholarshipTypesAsync)
            .WithName("GetPublicScholarshipTypes");
        group.MapGet("/academic-units", GetAcademicUnitsAsync)
            .WithName("GetPublicAcademicUnits");
        group.MapGet("/pdpa-notice", GetPdpaNoticeAsync)
            .WithName("GetPublicPdpaNotice");
        group.MapPost("/scholarship-requests", CreateScholarshipRequestAsync)
            .WithName("CreatePublicScholarshipRequest");

        return endpoints;
    }

    private static async Task<IResult> GetScholarshipTypesAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var items = await context.ScholarshipTypes
            .AsNoTracking()
            .Where(type => type.IsActive)
            .OrderBy(type => type.SortOrder)
            .ThenBy(type => type.Name)
            .Select(type => new ScholarshipTypeResponse(
                type.Id,
                type.Code,
                type.Name,
                type.Description,
                type.SortOrder))
            .ToArrayAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> GetAcademicUnitsAsync(
        string? query,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = query?.Trim();
        var unitsQuery = context.AcademicUnits
            .AsNoTracking()
            .Where(unit => unit.CampusId == SeedData.HatYaiCampusId && unit.IsActive);

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            var pattern = $"%{normalizedQuery}%";
            unitsQuery = unitsQuery.Where(unit =>
                EF.Functions.ILike(unit.Name, pattern) ||
                EF.Functions.ILike(unit.Code, pattern));
        }

        var items = await unitsQuery
            .OrderBy(unit => unit.SortOrder)
            .ThenBy(unit => unit.Name)
            .Take(30)
            .Select(unit => new AcademicUnitResponse(
                unit.Id,
                unit.Code,
                unit.Name,
                unit.SortOrder))
            .ToArrayAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> GetPdpaNoticeAsync(
        ApplicationDbContext context,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var notice = await GetCurrentPdpaNoticeAsync(context, clock.UtcNow, cancellationToken);
        if (notice is null)
        {
            return Results.Problem(
                title: "ไม่พบประกาศความเป็นส่วนตัวที่ใช้งานอยู่",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "PDPA_NOTICE_UNAVAILABLE"
                });
        }

        return Results.Ok(new PdpaNoticeResponse(
            notice.Id,
            notice.Version,
            notice.Content,
            notice.EffectiveAt));
    }

    private static async Task<IResult> CreateScholarshipRequestAsync(
        CreatePublicScholarshipRequest request,
        ApplicationDbContext context,
        PublicScholarshipRequestValidator validator,
        IRequestNumberGenerator requestNumberGenerator,
        IBankAccountProtector bankAccountProtector,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0)
        {
            return ValidationProblem(errors);
        }

        var submittedAt = clock.UtcNow;
        var notice = await GetCurrentPdpaNoticeAsync(context, submittedAt, cancellationToken);
        if (notice is null)
        {
            return Results.Problem(
                title: "ไม่พบประกาศความเป็นส่วนตัวที่ใช้งานอยู่",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "PDPA_NOTICE_UNAVAILABLE"
                });
        }

        if (!string.Equals(notice.Version, request.PdpaNoticeVersion.Trim(), StringComparison.Ordinal))
        {
            return Results.Problem(
                title: "ประกาศความเป็นส่วนตัวมีการเปลี่ยนแปลง",
                detail: "กรุณาอ่านและยอมรับประกาศฉบับปัจจุบันก่อนส่งคำขออีกครั้ง",
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "CONSENT_VERSION_CHANGED",
                    ["currentVersion"] = notice.Version
                });
        }

        var scholarshipTypeExists = await context.ScholarshipTypes
            .AnyAsync(type => type.Id == request.ScholarshipTypeId && type.IsActive, cancellationToken);
        if (!scholarshipTypeExists)
        {
            errors[nameof(request.ScholarshipTypeId)] = ["ไม่พบประเภททุนที่เปิดใช้งาน"];
        }

        string? authoritativeFacultyName = null;
        if (request.AcademicUnitId is not null)
        {
            authoritativeFacultyName = await context.AcademicUnits
                .Where(unit => unit.Id == request.AcademicUnitId &&
                    unit.CampusId == SeedData.HatYaiCampusId &&
                    unit.IsActive)
                .Select(unit => unit.Name)
                .SingleOrDefaultAsync(cancellationToken);
            if (authoritativeFacultyName is null)
            {
                errors[nameof(request.AcademicUnitId)] = ["ไม่พบคณะ/หน่วยการเรียนของวิทยาเขตหาดใหญ่"];
            }
        }

        if (errors.Count > 0)
        {
            return ValidationProblem(errors);
        }

        var normalizedBankAccount =
            PublicScholarshipRequestRules.NormalizeBankAccount(request.BankAccountNumber);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var requestNumber = await requestNumberGenerator.NextAsync(cancellationToken);
        var entity = new ScholarshipRequestEntity
        {
            Id = Guid.NewGuid(),
            RequestNumber = requestNumber,
            StudentId = request.StudentId.Trim(),
            StudentName = request.StudentName.Trim(),
            CampusId = SeedData.HatYaiCampusId,
            AcademicUnitId = request.AcademicUnitId,
            FacultyNameSnapshot = authoritativeFacultyName ?? request.FacultyName.Trim(),
            Major = NullIfWhiteSpace(request.Major),
            EducationLevel = EducationLevel.Undergraduate,
            YearLevel = request.YearLevel,
            YearLevelOther = NullIfWhiteSpace(request.YearLevelOther),
            Gpax = request.Gpax,
            Email = request.Email.Trim(),
            ScholarshipTypeId = request.ScholarshipTypeId,
            RequestedAmount = request.RequestedAmount,
            ProtectedBankAccountNumber = bankAccountProtector.Protect(normalizedBankAccount),
            BankAccountLastFour = normalizedBankAccount[^4..],
            Reason = request.Reason.Trim(),
            Status = ScholarshipRequestStatus.Pending,
            SubmissionSource = SubmissionSource.Public,
            PdpaNoticeId = notice.Id,
            ConsentMethod = ConsentMethod.Self,
            ConsentObtainedAt = submittedAt,
            SubmittedAt = submittedAt,
            CreatedAt = submittedAt,
            UpdatedAt = submittedAt
        };

        context.ScholarshipRequests.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Results.Created(
            "/api/v1/public/scholarship-requests",
            new CreateScholarshipRequestResponse(
                entity.Id,
                entity.RequestNumber,
                entity.Status.ToString(),
                entity.SubmittedAt));
    }

    private static Task<PdpaNotice?> GetCurrentPdpaNoticeAsync(
        ApplicationDbContext context,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken) =>
        context.PdpaNotices
            .AsNoTracking()
            .Where(notice => notice.IsActive && notice.EffectiveAt <= utcNow)
            .OrderByDescending(notice => notice.EffectiveAt)
            .FirstOrDefaultAsync(cancellationToken);

    private static IResult ValidationProblem(Dictionary<string, string[]> errors) =>
        Results.ValidationProblem(
            errors,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = "VALIDATION_FAILED"
            });

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
