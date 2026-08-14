using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ScholarshipRequest.Api.Data;
using ScholarshipRequest.Api.Domain.Masters;
using ScholarshipRequest.Api.Domain.Privacy;
using ScholarshipRequest.Api.Domain.ScholarshipRequests;
using ScholarshipRequest.Api.Features.PublicScholarshipRequests;
using ScholarshipRequest.Api.Security;
using ScholarshipRequest.Api.Time;
using ScholarshipRequest.Shared.V1.Admin.ScholarshipRequests;
using ScholarshipRequest.Shared.V1.ScholarshipRequests;
using ScholarshipRequestEntity = ScholarshipRequest.Api.Domain.ScholarshipRequests.ScholarshipRequest;

namespace ScholarshipRequest.Api.Features.Admin;

public static class AdminScholarshipRequestMutationEndpoints
{
    public static RouteGroupBuilder MapAdminScholarshipRequestMutationEndpoints(
        this RouteGroupBuilder group)
    {
        group.MapPost("/scholarship-requests", CreateAsync)
            .WithName("CreateAdminScholarshipRequest");
        group.MapPut("/scholarship-requests/{id:guid}", UpdateAsync)
            .WithName("UpdateAdminScholarshipRequest");
        group.MapDelete("/scholarship-requests/{id:guid}", DeleteAsync)
            .WithName("DeleteAdminScholarshipRequest");
        group.MapPost("/scholarship-requests/{id:guid}/decision", DecideAsync)
            .WithName("DecideAdminScholarshipRequest");

        return group;
    }

    private static async Task<IResult> CreateAsync(
        CreateAdminScholarshipRequest request,
        HttpContext httpContext,
        ApplicationDbContext context,
        AdminScholarshipRequestValidator validator,
        IRequestNumberGenerator requestNumberGenerator,
        IBankAccountProtector bankAccountProtector,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(httpContext, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var errors = validator.Validate(request);
        if (errors.Count > 0)
        {
            return ValidationProblem(errors);
        }

        var now = clock.UtcNow;
        var notice = await GetCurrentPdpaNoticeAsync(context, now, cancellationToken);
        if (notice is null)
        {
            return PdpaUnavailable();
        }

        if (!string.Equals(
            notice.Version,
            request.PdpaNoticeVersion.Trim(),
            StringComparison.Ordinal))
        {
            return ConsentVersionChanged(notice.Version);
        }

        var scholarshipTypeExists = await context.ScholarshipTypes
            .AnyAsync(type => type.Id == request.ScholarshipTypeId && type.IsActive, cancellationToken);
        if (!scholarshipTypeExists)
        {
            errors[nameof(request.ScholarshipTypeId)] = ["ไม่พบประเภททุนที่เปิดใช้งาน"];
        }

        var facultyName = await ResolveFacultyNameAsync(
            request.AcademicUnitId,
            request.FacultyName,
            context,
            errors,
            cancellationToken);
        if (errors.Count > 0)
        {
            return ValidationProblem(errors);
        }

        var normalizedBankAccount =
            PublicScholarshipRequestRules.NormalizeBankAccount(request.BankAccountNumber);
        var consentMethod = Enum.Parse<ConsentMethod>(request.ConsentMethod, true);

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
            FacultyNameSnapshot = facultyName,
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
            SubmissionSource = SubmissionSource.Staff,
            CreatedById = userId,
            PdpaNoticeId = notice.Id,
            ConsentMethod = consentMethod,
            ConsentEvidenceNote = request.ConsentEvidenceNote.Trim(),
            ConsentObtainedAt = now,
            SubmittedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            UpdatedById = userId
        };

        context.ScholarshipRequests.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Results.Created(
            $"/api/v1/admin/scholarship-requests/{entity.Id}",
            new CreateScholarshipRequestResponse(
                entity.Id,
                entity.RequestNumber,
                entity.Status.ToString(),
                entity.SubmittedAt));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateAdminScholarshipRequest request,
        HttpContext httpContext,
        ApplicationDbContext context,
        AdminScholarshipRequestValidator validator,
        IBankAccountProtector bankAccountProtector,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(httpContext, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var errors = validator.Validate(request);
        if (errors.Count > 0)
        {
            return ValidationProblem(errors);
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var entity = await LockRequestAsync(context, id, cancellationToken);
        if (entity is null)
        {
            return RequestNotFound();
        }

        if (entity.Status != ScholarshipRequestStatus.Pending)
        {
            return RequestNotPending();
        }

        if (entity.UpdatedAt != request.ExpectedUpdatedAt!.Value)
        {
            return RequestVersionConflict();
        }

        if (!string.Equals(
                entity.StudentId,
                request.StudentId.Trim(),
                StringComparison.Ordinal))
        {
            errors[nameof(request.StudentId)] =
                ["ไม่สามารถเปลี่ยนรหัสนักศึกษาได้ เนื่องจากหลักฐาน consent ผูกกับผู้ยื่นคำขอเดิม"];
            return ValidationProblem(errors);
        }

        var scholarshipTypeExists = await context.ScholarshipTypes.AnyAsync(
            type => type.Id == request.ScholarshipTypeId &&
                (type.IsActive || type.Id == entity.ScholarshipTypeId),
            cancellationToken);
        if (!scholarshipTypeExists)
        {
            errors[nameof(request.ScholarshipTypeId)] = ["ไม่พบประเภททุนที่เลือก"];
        }

        var facultyName = request.AcademicUnitId.HasValue &&
            request.AcademicUnitId == entity.AcademicUnitId
                ? entity.FacultyNameSnapshot
                : await ResolveFacultyNameAsync(
                    request.AcademicUnitId,
                    request.FacultyName,
                    context,
                    errors,
                    cancellationToken);
        if (errors.Count > 0)
        {
            return ValidationProblem(errors);
        }

        entity.StudentName = request.StudentName.Trim();
        entity.AcademicUnitId = request.AcademicUnitId;
        entity.FacultyNameSnapshot = facultyName;
        entity.Major = NullIfWhiteSpace(request.Major);
        entity.YearLevel = request.YearLevel;
        entity.YearLevelOther = NullIfWhiteSpace(request.YearLevelOther);
        entity.Gpax = request.Gpax;
        entity.Email = request.Email.Trim();
        entity.ScholarshipTypeId = request.ScholarshipTypeId;
        entity.RequestedAmount = request.RequestedAmount;
        entity.Reason = request.Reason.Trim();
        entity.UpdatedAt = clock.UtcNow;
        entity.UpdatedById = userId;

        if (!string.IsNullOrWhiteSpace(request.BankAccountNumber))
        {
            var normalizedBankAccount =
                PublicScholarshipRequestRules.NormalizeBankAccount(request.BankAccountNumber);
            entity.ProtectedBankAccountNumber = bankAccountProtector.Protect(normalizedBankAccount);
            entity.BankAccountLastFour = normalizedBankAccount[^4..];
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        DateTimeOffset? expectedUpdatedAt,
        HttpContext httpContext,
        ApplicationDbContext context,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(httpContext, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        if (expectedUpdatedAt is null)
        {
            return ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["ExpectedUpdatedAt"] = ["ต้องระบุเวอร์ชันข้อมูลที่ใช้ยืนยันการลบ"]
            });
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var entity = await LockRequestAsync(context, id, cancellationToken);
        if (entity is null)
        {
            return RequestNotFound();
        }

        if (entity.Status != ScholarshipRequestStatus.Pending)
        {
            return RequestNotPending();
        }

        if (entity.UpdatedAt != expectedUpdatedAt.Value)
        {
            return RequestVersionConflict();
        }

        var now = clock.UtcNow;
        entity.DeletedAt = now;
        entity.DeletedById = userId;
        entity.UpdatedAt = now;
        entity.UpdatedById = userId;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> DecideAsync(
        Guid id,
        AdminScholarshipRequestDecisionRequest request,
        HttpContext httpContext,
        ApplicationDbContext context,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(httpContext, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var errors = ValidateDecision(request, out var decision);
        if (errors.Count > 0)
        {
            return ValidationProblem(errors);
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var entity = await LockRequestAsync(context, id, cancellationToken);
        if (entity is null)
        {
            return RequestNotFound();
        }

        if (entity.Status != ScholarshipRequestStatus.Pending)
        {
            return RequestNotPending();
        }

        if (entity.UpdatedAt != request.ExpectedUpdatedAt!.Value)
        {
            return RequestVersionConflict();
        }

        var now = clock.UtcNow;
        entity.Status = decision;
        entity.DecisionNote = NullIfWhiteSpace(request.Note);
        entity.DecidedAt = now;
        entity.DecidedById = userId;
        entity.UpdatedAt = now;
        entity.UpdatedById = userId;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static Dictionary<string, string[]> ValidateDecision(
        AdminScholarshipRequestDecisionRequest request,
        out ScholarshipRequestStatus decision)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (request.ExpectedUpdatedAt is null)
        {
            errors[nameof(request.ExpectedUpdatedAt)] =
                ["ต้องระบุเวอร์ชันข้อมูลที่ใช้ตัดสินคำขอ"];
        }

        if (string.Equals(
                request.Decision,
                nameof(ScholarshipRequestStatus.Approved),
                StringComparison.OrdinalIgnoreCase))
        {
            decision = ScholarshipRequestStatus.Approved;
        }
        else if (string.Equals(
            request.Decision,
            nameof(ScholarshipRequestStatus.Rejected),
            StringComparison.OrdinalIgnoreCase))
        {
            decision = ScholarshipRequestStatus.Rejected;
        }
        else
        {
            decision = default;
            errors[nameof(request.Decision)] = ["ผลการพิจารณาต้องเป็น Approved หรือ Rejected"];
        }

        if (decision == ScholarshipRequestStatus.Rejected &&
            string.IsNullOrWhiteSpace(request.Note))
        {
            errors[nameof(request.Note)] = ["การปฏิเสธคำขอต้องระบุหมายเหตุ"];
        }
        else if (request.Note?.Trim().Length > 2000)
        {
            errors[nameof(request.Note)] = ["หมายเหตุต้องยาวไม่เกิน 2,000 ตัวอักษร"];
        }

        return errors;
    }

    private static Task<ScholarshipRequestEntity?> LockRequestAsync(
        ApplicationDbContext context,
        Guid id,
        CancellationToken cancellationToken) =>
        context.ScholarshipRequests
            .FromSqlInterpolated($"SELECT * FROM scholarship_requests WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private static async Task<string> ResolveFacultyNameAsync(
        Guid? academicUnitId,
        string suppliedFacultyName,
        ApplicationDbContext context,
        IDictionary<string, string[]> errors,
        CancellationToken cancellationToken)
    {
        if (academicUnitId is null)
        {
            return suppliedFacultyName.Trim();
        }

        var name = await context.AcademicUnits
            .Where(unit => unit.Id == academicUnitId &&
                unit.CampusId == SeedData.HatYaiCampusId &&
                unit.IsActive)
            .Select(unit => unit.Name)
            .SingleOrDefaultAsync(cancellationToken);
        if (name is null)
        {
            errors[nameof(CreateAdminScholarshipRequest.AcademicUnitId)] =
                ["ไม่พบคณะ/หน่วยการเรียนของวิทยาเขตหาดใหญ่"];
            return suppliedFacultyName.Trim();
        }

        return name;
    }

    private static Task<PdpaNotice?> GetCurrentPdpaNoticeAsync(
        ApplicationDbContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        context.PdpaNotices
            .AsNoTracking()
            .Where(notice => notice.IsActive && notice.EffectiveAt <= now)
            .OrderByDescending(notice => notice.EffectiveAt)
            .FirstOrDefaultAsync(cancellationToken);

    private static bool TryGetUserId(HttpContext context, out Guid userId) =>
        Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private static IResult RequestNotFound() => Results.Problem(
        title: "ไม่พบคำขอทุนการศึกษา",
        detail: "คำขออาจถูกลบหรือไม่มีอยู่ในระบบ",
        statusCode: StatusCodes.Status404NotFound,
        extensions: new Dictionary<string, object?>
        {
            ["code"] = "SCHOLARSHIP_REQUEST_NOT_FOUND"
        });

    private static IResult RequestNotPending() => Results.Problem(
        title: "คำขอนี้สิ้นสุดการพิจารณาแล้ว",
        detail: "แก้ไข ลบ หรือเปลี่ยนผลได้เฉพาะคำขอสถานะ Pending เท่านั้น",
        statusCode: StatusCodes.Status409Conflict,
        extensions: new Dictionary<string, object?>
        {
            ["code"] = "SCHOLARSHIP_REQUEST_NOT_PENDING"
        });

    private static IResult RequestVersionConflict() => Results.Problem(
        title: "ข้อมูลคำขอมีการเปลี่ยนแปลงแล้ว",
        detail: "กรุณาโหลดข้อมูลล่าสุดและตรวจสอบอีกครั้งก่อนดำเนินการ",
        statusCode: StatusCodes.Status409Conflict,
        extensions: new Dictionary<string, object?>
        {
            ["code"] = "SCHOLARSHIP_REQUEST_VERSION_CONFLICT"
        });

    private static IResult ValidationProblem(Dictionary<string, string[]> errors) =>
        Results.ValidationProblem(
            errors,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = "VALIDATION_FAILED"
            });

    private static IResult PdpaUnavailable() => Results.Problem(
        title: "ไม่พบประกาศความเป็นส่วนตัวที่ใช้งานอยู่",
        statusCode: StatusCodes.Status503ServiceUnavailable,
        extensions: new Dictionary<string, object?>
        {
            ["code"] = "PDPA_NOTICE_UNAVAILABLE"
        });

    private static IResult ConsentVersionChanged(string currentVersion) => Results.Problem(
        title: "ประกาศความเป็นส่วนตัวมีการเปลี่ยนแปลง",
        detail: "กรุณาตรวจสอบหลักฐาน consent กับประกาศฉบับปัจจุบันก่อนบันทึกอีกครั้ง",
        statusCode: StatusCodes.Status409Conflict,
        extensions: new Dictionary<string, object?>
        {
            ["code"] = "CONSENT_VERSION_CHANGED",
            ["currentVersion"] = currentVersion
        });

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
