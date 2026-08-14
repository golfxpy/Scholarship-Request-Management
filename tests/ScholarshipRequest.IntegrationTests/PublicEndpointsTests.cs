using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScholarshipRequest.Api.Domain.ScholarshipRequests;
using ScholarshipRequest.Api.Security;
using ScholarshipRequest.Shared.V1.Masters;
using ScholarshipRequest.Shared.V1.Privacy;
using ScholarshipRequest.Shared.V1.ScholarshipRequests;

namespace ScholarshipRequest.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class PublicEndpointsTests(
    PostgreSqlFixture database,
    WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task MasterEndpoints_ShouldReturnSeededHatYaiData()
    {
        using var application = CreateApplication();
        using var client = application.CreateClient();

        var scholarshipTypes = await client.GetFromJsonAsync<ScholarshipTypeResponse[]>(
            "/api/v1/public/scholarship-types");
        var academicUnits = await client.GetFromJsonAsync<AcademicUnitResponse[]>(
            "/api/v1/public/academic-units?query=ENG");
        var pdpaNotice = await client.GetFromJsonAsync<PdpaNoticeResponse>(
            "/api/v1/public/pdpa-notice");

        Assert.NotNull(scholarshipTypes);
        Assert.Equal(5, scholarshipTypes.Length);
        Assert.NotNull(academicUnits);
        Assert.Single(academicUnits);
        Assert.Equal("ENG", academicUnits[0].Code);
        Assert.NotNull(pdpaNotice);
        Assert.Equal("POC-v1", pdpaNotice.Version);
    }

    [Fact]
    public async Task Create_ShouldPersistPendingPublicRequestWithProtectedBankAccount()
    {
        using var application = CreateApplication();
        using var client = application.CreateClient();
        var request = await CreateValidRequestAsync(client, "6699000001");
        request.FacultyName = "Client-supplied mismatch";

        using var response = await client.PostAsJsonAsync(
            "/api/v1/public/scholarship-requests",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CreateScholarshipRequestResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Pending", payload.Status);
        Assert.StartsWith("SCH-", payload.RequestNumber, StringComparison.Ordinal);

        await using var context = database.CreateDbContext();
        var entity = await context.ScholarshipRequests
            .SingleAsync(item => item.Id == payload.Id);
        var protector = application.Services.GetRequiredService<IBankAccountProtector>();

        Assert.Equal(ScholarshipRequestStatus.Pending, entity.Status);
        Assert.Equal(SubmissionSource.Public, entity.SubmissionSource);
        Assert.Equal(ConsentMethod.Self, entity.ConsentMethod);
        Assert.Equal("คณะวิศวกรรมศาสตร์", entity.FacultyNameSnapshot);
        Assert.Equal("7890", entity.BankAccountLastFour);
        Assert.NotEqual("1234567890", entity.ProtectedBankAccountNumber);
        Assert.Equal("1234567890", protector.Unprotect(entity.ProtectedBankAccountNumber));
    }

    [Fact]
    public async Task Create_ShouldRejectInvalidConsentWithoutWritingRequest()
    {
        using var application = CreateApplication();
        using var client = application.CreateClient();
        var request = await CreateValidRequestAsync(client, "6699000002");
        request.PdpaConsent = false;
        var countBefore = await CountRequestsAsync();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/public/scholarship-requests",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(countBefore, await CountRequestsAsync());
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VALIDATION_FAILED", problem.GetProperty("code").GetString());
        Assert.True(
            problem.GetProperty("errors").TryGetProperty(
                nameof(CreatePublicScholarshipRequest.PdpaConsent),
                out var consentErrors));
        Assert.NotEmpty(consentErrors.EnumerateArray());
    }

    [Fact]
    public async Task Create_ShouldReturnConflictWhenPdpaVersionChanged()
    {
        using var application = CreateApplication();
        using var client = application.CreateClient();
        var request = await CreateValidRequestAsync(client, "6699000003");
        request.PdpaNoticeVersion = "outdated";
        var countBefore = await CountRequestsAsync();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/public/scholarship-requests",
            request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(countBefore, await CountRequestsAsync());
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("CONSENT_VERSION_CHANGED", problem.GetProperty("code").GetString());
        Assert.Equal("POC-v1", problem.GetProperty("currentVersion").GetString());
    }

    [Fact]
    public async Task ConcurrentCreate_ShouldGenerateUniqueRequestNumbers()
    {
        using var application = CreateApplication();
        using var client = application.CreateClient();
        var requests = await Task.WhenAll(
            Enumerable.Range(10, 10)
                .Select(sequence => CreateValidRequestAsync(client, $"66990000{sequence:00}")));

        var responses = await Task.WhenAll(requests.Select(request =>
            client.PostAsJsonAsync("/api/v1/public/scholarship-requests", request)));

        try
        {
            Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
            var payloads = await Task.WhenAll(responses.Select(response =>
                response.Content.ReadFromJsonAsync<CreateScholarshipRequestResponse>()));
            var requestNumbers = payloads.Select(payload => payload!.RequestNumber).ToArray();
            Assert.Equal(requestNumbers.Length, requestNumbers.Distinct(StringComparer.Ordinal).Count());
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    private WebApplicationFactory<Program> CreateApplication() =>
        factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Testing")
                .UseSetting("ConnectionStrings:DefaultConnection", database.ConnectionString));

    private async Task<int> CountRequestsAsync()
    {
        await using var context = database.CreateDbContext();
        return await context.ScholarshipRequests.CountAsync();
    }

    private static async Task<CreatePublicScholarshipRequest> CreateValidRequestAsync(
        HttpClient client,
        string studentId)
    {
        var scholarshipTypes = await client.GetFromJsonAsync<ScholarshipTypeResponse[]>(
            "/api/v1/public/scholarship-types");
        var academicUnits = await client.GetFromJsonAsync<AcademicUnitResponse[]>(
            "/api/v1/public/academic-units?query=ENG");
        var pdpaNotice = await client.GetFromJsonAsync<PdpaNoticeResponse>(
            "/api/v1/public/pdpa-notice");

        Assert.NotNull(scholarshipTypes);
        Assert.NotNull(academicUnits);
        Assert.NotNull(pdpaNotice);

        return new CreatePublicScholarshipRequest
        {
            StudentId = studentId,
            StudentName = $"นักศึกษาทดสอบ {studentId}",
            AcademicUnitId = academicUnits[0].Id,
            FacultyName = academicUnits[0].Name,
            Major = "วิศวกรรมคอมพิวเตอร์",
            YearLevel = 3,
            Gpax = 3.25m,
            Email = $"{studentId}@example.com",
            ScholarshipTypeId = scholarshipTypes[0].Id,
            RequestedAmount = 10_000m,
            BankAccountNumber = "123-456-7890",
            Reason = "ต้องการทุนเพื่อสนับสนุนค่าใช้จ่ายด้านการศึกษา",
            PdpaConsent = true,
            PdpaNoticeVersion = pdpaNotice.Version
        };
    }
}
