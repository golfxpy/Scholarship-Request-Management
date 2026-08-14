using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScholarshipRequest.Api.Data.Identity;
using ScholarshipRequest.Api.Domain.ScholarshipRequests;
using ScholarshipRequest.Api.Security;
using ScholarshipRequest.Shared.V1.Authentication;
using ScholarshipRequestEntity = ScholarshipRequest.Api.Domain.ScholarshipRequests.ScholarshipRequest;

namespace ScholarshipRequest.Api.Data;

public sealed class DevelopmentDemoDataSeeder(
    IHostEnvironment environment,
    IOptions<DevelopmentDemoSeedOptions> options,
    ApplicationDbContext context,
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    IBankAccountProtector bankAccountProtector,
    ILogger<DevelopmentDemoDataSeeder> logger)
{
    public const int DemoRequestCount = 25;

    private static readonly Guid DemoAdminId =
        Guid.Parse("60000000-0000-0000-0000-000000000001");

    private static readonly DateTimeOffset BaseSubmittedAt =
        new(2026, 7, 1, 2, 0, 0, TimeSpan.Zero);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!environment.IsDevelopment() || !settings.Enabled)
        {
            return;
        }

        ValidateSettings(settings);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock(739256900001)",
            cancellationToken);

        var role = await EnsureStaffRoleAsync();
        var admin = await EnsureAdminAsync(settings, role);
        var addedRequests = await EnsureDemoRequestsAsync(admin, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Development demo seed is ready for user {UserName}; added {RequestCount} fictional requests.",
            admin.UserName,
            addedRequests);
    }

    private async Task<IdentityRole<Guid>> EnsureStaffRoleAsync()
    {
        var role = await roleManager.FindByNameAsync(AuthenticationConstants.StaffRole);
        if (role is not null)
        {
            return role;
        }

        role = new IdentityRole<Guid>(AuthenticationConstants.StaffRole);
        ThrowIfFailed(
            await roleManager.CreateAsync(role),
            "create the Development Staff role");
        return role;
    }

    private async Task<ApplicationUser> EnsureAdminAsync(
        DevelopmentDemoSeedOptions settings,
        IdentityRole<Guid> role)
    {
        var user = await userManager.FindByNameAsync(settings.AdminUserName.Trim());
        if (user is not null && user.Id != DemoAdminId)
        {
            throw new InvalidOperationException(
                "The configured Development demo username is already owned by a non-demo account. " +
                "Choose another DevelopmentDemoSeed__AdminUserName or remove the collision deliberately.");
        }

        if (user is null)
        {
            var now = DateTimeOffset.UtcNow;
            user = new ApplicationUser
            {
                Id = DemoAdminId,
                UserName = settings.AdminUserName.Trim(),
                FullName = "ผู้ดูแลระบบตัวอย่าง",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            ThrowIfFailed(
                await userManager.CreateAsync(user, settings.AdminPassword),
                "create the Development demo administrator");
        }

        if (!await userManager.IsInRoleAsync(user, role.Name!))
        {
            ThrowIfFailed(
                await userManager.AddToRoleAsync(user, role.Name!),
                "assign the Development demo administrator role");
        }

        return user;
    }

    private async Task<int> EnsureDemoRequestsAsync(
        ApplicationUser admin,
        CancellationToken cancellationToken)
    {
        var demoIds = Enumerable.Range(1, DemoRequestCount)
            .Select(CreateDemoRequestId)
            .ToArray();
        var existingRequests = await context.ScholarshipRequests
            .IgnoreQueryFilters()
            .Where(request => demoIds.Contains(request.Id))
            .ToDictionaryAsync(request => request.Id, cancellationToken);

        foreach (var (sequence, existingRequest) in Enumerable.Range(1, DemoRequestCount)
            .Select(sequence => (sequence, request: existingRequests.GetValueOrDefault(
                CreateDemoRequestId(sequence))))
            .Where(item => item.request is not null))
        {
            existingRequest!.RequestNumber = $"DEMO-2569-{sequence:000000}";
        }

        var requests = Enumerable.Range(1, DemoRequestCount)
            .Where(sequence => !existingRequests.ContainsKey(CreateDemoRequestId(sequence)))
            .Select(sequence => CreateDemoRequest(sequence, admin.Id))
            .ToArray();
        context.ScholarshipRequests.AddRange(requests);
        await context.SaveChangesAsync(cancellationToken);
        return requests.Length;
    }

    private ScholarshipRequestEntity CreateDemoRequest(int sequence, Guid adminUserId)
    {
        var status = sequence switch
        {
            <= 10 => ScholarshipRequestStatus.Pending,
            <= 18 => ScholarshipRequestStatus.Approved,
            _ => ScholarshipRequestStatus.Rejected
        };
        var submittedAt = BaseSubmittedAt.AddDays(sequence - 1);
        var isStaffSubmission = sequence % 5 == 0;
        var normalizedBankAccount = $"{9_900_000_000L + sequence:0000000000}";
        var academicUnit = SeedData.AcademicUnits[(sequence - 1) % SeedData.AcademicUnits.Count];
        var scholarshipType = SeedData.ScholarshipTypes[
            (sequence - 1) % SeedData.ScholarshipTypes.Count];

        return new ScholarshipRequestEntity
        {
            Id = CreateDemoRequestId(sequence),
            RequestNumber = $"DEMO-2569-{sequence:000000}",
            StudentId = $"{6_600_000_000L + sequence:0000000000}",
            StudentName = $"นักศึกษาจำลอง {sequence:00}",
            CampusId = SeedData.HatYaiCampusId,
            AcademicUnitId = academicUnit.Id,
            FacultyNameSnapshot = academicUnit.Name,
            Major = sequence % 4 == 0 ? null : "หลักสูตรตัวอย่าง",
            EducationLevel = EducationLevel.Undergraduate,
            YearLevel = ((sequence - 1) % 6) + 1,
            Gpax = 2.00m + ((sequence - 1) % 20 * 0.10m),
            Email = $"demo.student{sequence:00}@example.invalid",
            ScholarshipTypeId = scholarshipType.Id,
            RequestedAmount = 5_000m + ((sequence - 1) % 5 * 2_500m),
            ProtectedBankAccountNumber = bankAccountProtector.Protect(normalizedBankAccount),
            BankAccountLastFour = normalizedBankAccount[^4..],
            Reason = "ข้อมูลจำลองสำหรับสาธิตกระบวนการพิจารณาคำขอทุนการศึกษา",
            Status = status,
            DecisionNote = status switch
            {
                ScholarshipRequestStatus.Approved => "ข้อมูลจำลอง: อนุมัติเพื่อใช้สาธิตระบบ",
                ScholarshipRequestStatus.Rejected => "ข้อมูลจำลอง: ปฏิเสธเพื่อใช้สาธิตระบบ",
                _ => null
            },
            DecidedAt = status == ScholarshipRequestStatus.Pending
                ? null
                : submittedAt.AddDays(2),
            DecidedById = status == ScholarshipRequestStatus.Pending ? null : adminUserId,
            SubmissionSource = isStaffSubmission ? SubmissionSource.Staff : SubmissionSource.Public,
            CreatedById = isStaffSubmission ? adminUserId : null,
            PdpaNoticeId = SeedData.ActivePdpaNoticeId,
            ConsentMethod = isStaffSubmission ? ConsentMethod.Document : ConsentMethod.Self,
            ConsentEvidenceNote = isStaffSubmission
                ? "ข้อมูลจำลอง: เจ้าหน้าที่รับหลักฐานความยินยอมแล้ว"
                : null,
            ConsentObtainedAt = submittedAt,
            SubmittedAt = submittedAt,
            CreatedAt = submittedAt,
            UpdatedAt = status == ScholarshipRequestStatus.Pending
                ? submittedAt
                : submittedAt.AddDays(2),
            UpdatedById = status == ScholarshipRequestStatus.Pending ? null : adminUserId
        };
    }

    private static Guid CreateDemoRequestId(int sequence) =>
        Guid.Parse($"50000000-0000-0000-0000-{sequence:000000000000}");

    private static void ValidateSettings(DevelopmentDemoSeedOptions settings)
    {
        if (string.IsNullOrWhiteSpace(settings.AdminUserName) ||
            string.IsNullOrEmpty(settings.AdminPassword))
        {
            throw new InvalidOperationException(
                "Development demo seed is enabled, but its administrator credentials are missing.");
        }
    }

    private static void ThrowIfFailed(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var details = string.Join(
            "; ",
            result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        throw new InvalidOperationException($"Unable to {operation}. {details}");
    }
}

public static class DevelopmentDemoDataSeederExtensions
{
    public static async Task SeedDevelopmentDemoDataAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentDemoDataSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
