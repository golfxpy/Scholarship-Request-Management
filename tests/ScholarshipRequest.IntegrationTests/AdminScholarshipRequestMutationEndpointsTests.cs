using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScholarshipRequest.Api.Data;
using ScholarshipRequest.Api.Domain.ScholarshipRequests;
using ScholarshipRequest.Api.Security;
using ScholarshipRequest.Shared.V1.Admin.ScholarshipRequests;
using ScholarshipRequest.Shared.V1.Authentication;
using ScholarshipRequest.Shared.V1.Privacy;
using ScholarshipRequest.Shared.V1.ScholarshipRequests;

namespace ScholarshipRequest.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class AdminScholarshipRequestMutationEndpointsTests(
    PostgreSqlFixture database,
    WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string DemoUserName = "admin";
    private const string DemoPassword = "Scholarship@2569";

    [Fact]
    public async Task Create_ShouldRequireAntiforgeryAndStaffConsentEvidence()
    {
        using var application = CreateApplication();
        using var client = CreateClient(application);
        await LoginAsync(client);
        var pdpa = await GetPdpaAsync(client);
        var request = CreateRequest(pdpa.Version);

        using var noTokenResponse = await client.PostAsJsonAsync(
            "/api/v1/admin/scholarship-requests",
            request);
        Assert.Equal(HttpStatusCode.BadRequest, noTokenResponse.StatusCode);
        await AssertProblemCodeAsync(noTokenResponse, "ANTIFORGERY_VALIDATION_FAILED");

        var token = await GetAntiforgeryTokenAsync(client);
        request.ConsentMethod = "Self";
        request.ConsentEvidenceNote = "   ";
        using var invalidConsentResponse = await SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/admin/scholarship-requests",
            request,
            token);
        Assert.Equal(HttpStatusCode.BadRequest, invalidConsentResponse.StatusCode);
        var problem = await invalidConsentResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VALIDATION_FAILED", problem.GetProperty("code").GetString());
        Assert.True(problem.GetProperty("errors").TryGetProperty("ConsentMethod", out _));
        Assert.True(problem.GetProperty("errors").TryGetProperty("ConsentEvidenceNote", out _));

        await using var context = database.CreateDbContext();
        Assert.False(await context.ScholarshipRequests
            .IgnoreQueryFilters()
            .AnyAsync(item => item.StudentId == request.StudentId));
    }

    [Fact]
    public async Task Create_ShouldPersistPendingStaffRequestWithProtectedBankAndConsentAudit()
    {
        using var application = CreateApplication();
        using var client = CreateClient(application);
        await LoginAsync(client);
        var token = await GetAntiforgeryTokenAsync(client);
        var pdpa = await GetPdpaAsync(client);
        var request = CreateRequest(pdpa.Version);
        Guid createdId = default;

        try
        {
            using var response = await SendAsync(
                client,
                HttpMethod.Post,
                "/api/v1/admin/scholarship-requests",
                request,
                token);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<CreateScholarshipRequestResponse>();
            Assert.NotNull(created);
            createdId = created.Id;
            Assert.Equal("Pending", created.Status);
            Assert.StartsWith("SCH-", created.RequestNumber, StringComparison.Ordinal);

            using var scope = application.Services.CreateScope();
            var protector = scope.ServiceProvider.GetRequiredService<IBankAccountProtector>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var entity = await context.ScholarshipRequests.SingleAsync(item => item.Id == createdId);
            Assert.Equal(SubmissionSource.Staff, entity.SubmissionSource);
            Assert.Equal(ConsentMethod.Document, entity.ConsentMethod);
            Assert.Equal(request.ConsentEvidenceNote, entity.ConsentEvidenceNote);
            Assert.NotNull(entity.CreatedById);
            Assert.Equal(ScholarshipRequestStatus.Pending, entity.Status);
            Assert.NotEqual(request.BankAccountNumber, entity.ProtectedBankAccountNumber);
            Assert.Equal("7890", entity.BankAccountLastFour);
            Assert.Equal("1234567890", protector.Unprotect(entity.ProtectedBankAccountNumber));
        }
        finally
        {
            if (createdId != Guid.Empty)
            {
                await HardDeleteAsync(createdId);
            }
        }
    }

    [Fact]
    public async Task PendingRequest_ShouldUpdateWithoutReplacingBankAndSoftDelete()
    {
        using var application = CreateApplication();
        using var client = CreateClient(application);
        await LoginAsync(client);
        var token = await GetAntiforgeryTokenAsync(client);
        var created = await CreateViaApiAsync(client, token);

        try
        {
            string protectedBankBefore;
            string studentIdBefore;
            Guid? academicUnitIdBefore;
            DateTimeOffset updatedAtBefore;
            await using (var beforeContext = database.CreateDbContext())
            {
                var before = await beforeContext.ScholarshipRequests
                    .SingleAsync(item => item.Id == created.Id);
                protectedBankBefore = before.ProtectedBankAccountNumber;
                studentIdBefore = before.StudentId;
                academicUnitIdBefore = before.AcademicUnitId;
                updatedAtBefore = before.UpdatedAt;
            }

            var update = CreateUpdateRequest();
            update.ExpectedUpdatedAt = updatedAtBefore;
            update.StudentId = studentIdBefore;
            update.AcademicUnitId = academicUnitIdBefore;
            update.StudentName = "นักศึกษาที่แก้ไขแล้ว";
            update.BankAccountNumber = null;
            using var updateResponse = await SendAsync(
                client,
                HttpMethod.Put,
                $"/api/v1/admin/scholarship-requests/{created.Id}",
                update,
                token);
            Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

            await using (var updatedContext = database.CreateDbContext())
            {
                var entity = await updatedContext.ScholarshipRequests
                    .SingleAsync(item => item.Id == created.Id);
                Assert.Equal(update.StudentName, entity.StudentName);
                Assert.Equal(studentIdBefore, entity.StudentId);
                Assert.Equal(academicUnitIdBefore, entity.AcademicUnitId);
                Assert.Equal(protectedBankBefore, entity.ProtectedBankAccountNumber);
                Assert.NotNull(entity.UpdatedById);
                update.ExpectedUpdatedAt = entity.UpdatedAt;
            }

            var staleUpdate = CreateUpdateRequest();
            staleUpdate.ExpectedUpdatedAt = updatedAtBefore;
            staleUpdate.StudentId = studentIdBefore;
            staleUpdate.AcademicUnitId = academicUnitIdBefore;
            using var staleUpdateResponse = await SendAsync(
                client,
                HttpMethod.Put,
                $"/api/v1/admin/scholarship-requests/{created.Id}",
                staleUpdate,
                token);
            Assert.Equal(HttpStatusCode.Conflict, staleUpdateResponse.StatusCode);
            await AssertProblemCodeAsync(
                staleUpdateResponse,
                "SCHOLARSHIP_REQUEST_VERSION_CONFLICT");

            update.StudentId = $"CHANGED{Guid.NewGuid():N}"[..20];
            using var changedStudentResponse = await SendAsync(
                client,
                HttpMethod.Put,
                $"/api/v1/admin/scholarship-requests/{created.Id}",
                update,
                token);
            Assert.Equal(HttpStatusCode.BadRequest, changedStudentResponse.StatusCode);
            var changedStudentProblem = await changedStudentResponse.Content
                .ReadFromJsonAsync<JsonElement>();
            Assert.True(changedStudentProblem.GetProperty("errors")
                .TryGetProperty("StudentId", out _));

            using var deleteResponse = await SendAsync<object?>(
                client,
                HttpMethod.Delete,
                DeleteUri(created.Id, update.ExpectedUpdatedAt!.Value),
                null,
                token);
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            using var detailResponse = await client.GetAsync(
                $"/api/v1/admin/scholarship-requests/{created.Id}");
            Assert.Equal(HttpStatusCode.NotFound, detailResponse.StatusCode);

            await using var deletedContext = database.CreateDbContext();
            var deleted = await deletedContext.ScholarshipRequests
                .IgnoreQueryFilters()
                .SingleAsync(item => item.Id == created.Id);
            Assert.NotNull(deleted.DeletedAt);
            Assert.NotNull(deleted.DeletedById);
        }
        finally
        {
            await HardDeleteAsync(created.Id);
        }
    }

    [Fact]
    public async Task Decisions_ShouldRequireRejectNoteAllowEmptyApproveAndMakeFinalImmutable()
    {
        using var application = CreateApplication();
        using var client = CreateClient(application);
        await LoginAsync(client);
        var token = await GetAntiforgeryTokenAsync(client);
        var approveTarget = await CreateViaApiAsync(client, token);
        var rejectTarget = await CreateViaApiAsync(client, token);
        var approveVersion = await GetUpdatedAtAsync(approveTarget.Id);
        var rejectVersion = await GetUpdatedAtAsync(rejectTarget.Id);

        try
        {
            using var emptyRejectResponse = await SendAsync(
                client,
                HttpMethod.Post,
                $"/api/v1/admin/scholarship-requests/{rejectTarget.Id}/decision",
                new AdminScholarshipRequestDecisionRequest
                {
                    ExpectedUpdatedAt = rejectVersion,
                    Decision = "Rejected",
                    Note = "   "
                },
                token);
            Assert.Equal(HttpStatusCode.BadRequest, emptyRejectResponse.StatusCode);
            var emptyRejectProblem = await emptyRejectResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(emptyRejectProblem.GetProperty("errors").TryGetProperty("Note", out _));

            using var approveResponse = await SendAsync(
                client,
                HttpMethod.Post,
                $"/api/v1/admin/scholarship-requests/{approveTarget.Id}/decision",
                new AdminScholarshipRequestDecisionRequest
                {
                    ExpectedUpdatedAt = approveVersion,
                    Decision = "Approved",
                    Note = null
                },
                token);
            Assert.Equal(HttpStatusCode.NoContent, approveResponse.StatusCode);

            using var rejectResponse = await SendAsync(
                client,
                HttpMethod.Post,
                $"/api/v1/admin/scholarship-requests/{rejectTarget.Id}/decision",
                new AdminScholarshipRequestDecisionRequest
                {
                    ExpectedUpdatedAt = rejectVersion,
                    Decision = "Rejected",
                    Note = "เอกสารหลักฐานยังไม่ครบถ้วน"
                },
                token);
            Assert.Equal(HttpStatusCode.NoContent, rejectResponse.StatusCode);

            await using (var context = database.CreateDbContext())
            {
                var approved = await context.ScholarshipRequests
                    .SingleAsync(item => item.Id == approveTarget.Id);
                var rejected = await context.ScholarshipRequests
                    .SingleAsync(item => item.Id == rejectTarget.Id);
                Assert.Equal(ScholarshipRequestStatus.Approved, approved.Status);
                Assert.Null(approved.DecisionNote);
                Assert.Equal(ScholarshipRequestStatus.Rejected, rejected.Status);
                Assert.Equal("เอกสารหลักฐานยังไม่ครบถ้วน", rejected.DecisionNote);
                Assert.NotNull(approved.DecidedAt);
                Assert.NotNull(rejected.DecidedById);
            }

            using var repeatDecision = await SendAsync(
                client,
                HttpMethod.Post,
                $"/api/v1/admin/scholarship-requests/{approveTarget.Id}/decision",
                new AdminScholarshipRequestDecisionRequest
                {
                    ExpectedUpdatedAt = await GetUpdatedAtAsync(approveTarget.Id),
                    Decision = "Rejected",
                    Note = "ห้ามย้อนผล"
                },
                token);
            Assert.Equal(HttpStatusCode.Conflict, repeatDecision.StatusCode);
            await AssertProblemCodeAsync(repeatDecision, "SCHOLARSHIP_REQUEST_NOT_PENDING");

            var approvedFinalVersion = await GetUpdatedAtAsync(approveTarget.Id);
            var finalUpdate = CreateUpdateRequest();
            finalUpdate.ExpectedUpdatedAt = approvedFinalVersion;
            using var updateFinal = await SendAsync(
                client,
                HttpMethod.Put,
                $"/api/v1/admin/scholarship-requests/{approveTarget.Id}",
                finalUpdate,
                token);
            Assert.Equal(HttpStatusCode.Conflict, updateFinal.StatusCode);

            using var deleteFinal = await SendAsync<object?>(
                client,
                HttpMethod.Delete,
                DeleteUri(approveTarget.Id, approvedFinalVersion),
                null,
                token);
            Assert.Equal(HttpStatusCode.Conflict, deleteFinal.StatusCode);
        }
        finally
        {
            await HardDeleteAsync(approveTarget.Id);
            await HardDeleteAsync(rejectTarget.Id);
        }
    }

    private WebApplicationFactory<Program> CreateApplication() =>
        factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development")
                .UseSetting("ConnectionStrings:DefaultConnection", database.ConnectionString)
                .UseSetting("DevelopmentDemoSeed:Enabled", "true")
                .UseSetting("DevelopmentDemoSeed:AdminUserName", DemoUserName)
                .UseSetting("DevelopmentDemoSeed:AdminPassword", DemoPassword));

    private static HttpClient CreateClient(WebApplicationFactory<Program> application) =>
        application.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });

    private static async Task LoginAsync(HttpClient client)
    {
        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/auth/login",
            new StaffLoginRequest { UserName = DemoUserName, Password = DemoPassword },
            token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<AntiforgeryTokenResponse> GetAntiforgeryTokenAsync(
        HttpClient client)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/v1/auth/antiforgery-token");
        return Assert.IsType<AntiforgeryTokenResponse>(token);
    }

    private static async Task<PdpaNoticeResponse> GetPdpaAsync(HttpClient client)
    {
        var notice = await client.GetFromJsonAsync<PdpaNoticeResponse>(
            "/api/v1/public/pdpa-notice");
        return Assert.IsType<PdpaNoticeResponse>(notice);
    }

    private static async Task<CreateScholarshipRequestResponse> CreateViaApiAsync(
        HttpClient client,
        AntiforgeryTokenResponse token)
    {
        var notice = await GetPdpaAsync(client);
        using var response = await SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/admin/scholarship-requests",
            CreateRequest(notice.Version),
            token);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreateScholarshipRequestResponse>();
        return Assert.IsType<CreateScholarshipRequestResponse>(created);
    }

    private static CreateAdminScholarshipRequest CreateRequest(string pdpaVersion)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        return new CreateAdminScholarshipRequest
        {
            StudentId = $"IT{suffix}",
            StudentName = "นักศึกษาทดสอบเจ้าหน้าที่",
            AcademicUnitId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            FacultyName = "คณะทดสอบ",
            Major = "หลักสูตรทดสอบ",
            YearLevel = 3,
            Gpax = 3.25m,
            Email = $"staff-{suffix}@example.invalid",
            ScholarshipTypeId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            RequestedAmount = 10_000m,
            BankAccountNumber = "123-456-7890",
            Reason = "ทดสอบคำขอที่เจ้าหน้าที่บันทึกแทน",
            PdpaNoticeVersion = pdpaVersion,
            ConsentMethod = "Document",
            ConsentEvidenceNote = "ได้รับแบบฟอร์ม consent ที่ลงลายมือชื่อแล้ว"
        };
    }

    private static UpdateAdminScholarshipRequest CreateUpdateRequest() => new()
    {
        StudentId = "ITUPDATE0000000001",
        StudentName = "นักศึกษาทดสอบแก้ไข",
        FacultyName = "คณะทดสอบ",
        Major = "หลักสูตรที่แก้ไข",
        YearLevel = 4,
        Gpax = 3.50m,
        Email = "updated@example.invalid",
        ScholarshipTypeId = Guid.Parse("30000000-0000-0000-0000-000000000002"),
        RequestedAmount = 12_500m,
        Reason = "ปรับปรุงเหตุผลของคำขอ"
    };

    private async Task<DateTimeOffset> GetUpdatedAtAsync(Guid id)
    {
        await using var context = database.CreateDbContext();
        return await context.ScholarshipRequests
            .IgnoreQueryFilters()
            .Where(item => item.Id == id)
            .Select(item => item.UpdatedAt)
            .SingleAsync();
    }

    private static string DeleteUri(Guid id, DateTimeOffset expectedUpdatedAt) =>
        $"/api/v1/admin/scholarship-requests/{id}" +
        $"?expectedUpdatedAt={Uri.EscapeDataString(expectedUpdatedAt.ToString("O"))}";

    private async Task HardDeleteAsync(Guid id)
    {
        await using var context = database.CreateDbContext();
        var entity = await context.ScholarshipRequests
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.Id == id);
        if (entity is not null)
        {
            context.ScholarshipRequests.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    private static async Task<HttpResponseMessage> SendAsync<T>(
        HttpClient client,
        HttpMethod method,
        string requestUri,
        T content,
        AntiforgeryTokenResponse token)
    {
        var message = new HttpRequestMessage(method, requestUri);
        if (content is not null)
        {
            message.Content = JsonContent.Create(content);
        }

        message.Headers.TryAddWithoutValidation(token.HeaderName, token.RequestToken);
        return await client.SendAsync(message);
    }

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
    }
}
