using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScholarshipRequest.Api.Data;
using ScholarshipRequest.Shared.V1.Admin.ScholarshipRequests;
using ScholarshipRequest.Shared.V1.Authentication;

namespace ScholarshipRequest.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class AdminScholarshipRequestEndpointsTests(
    PostgreSqlFixture database,
    WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string DemoUserName = "admin";
    private const string DemoPassword = "Scholarship@2569";

    [Fact]
    public async Task AnonymousUser_ShouldReceive401ForListAndDetailWithoutRedirect()
    {
        using var application = CreateApplication();
        using var client = CreateClient(application);

        using var listResponse = await client.GetAsync("/api/v1/admin/scholarship-requests");
        using var detailResponse = await client.GetAsync(
            "/api/v1/admin/scholarship-requests/50000000-0000-0000-0000-000000000001");

        Assert.Equal(HttpStatusCode.Unauthorized, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, detailResponse.StatusCode);
        Assert.Null(listResponse.Headers.Location);
        Assert.Null(detailResponse.Headers.Location);
    }

    [Fact]
    public async Task List_ShouldPageSearchAndCombineFiltersOverSeededRequests()
    {
        using var application = CreateApplication();
        using var client = CreateClient(application);
        await LoginAsync(client);

        var pages = new List<AdminScholarshipRequestListResponse>();
        for (var page = 1; page <= 3; page++)
        {
            using var response = await client.GetAsync(
                $"/api/v1/admin/scholarship-requests?search={Uri.EscapeDataString("นักศึกษาจำลอง")}&page={page}");
            response.EnsureSuccessStatusCode();
            Assert.Contains("no-store", response.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);
            pages.Add(Assert.IsType<AdminScholarshipRequestListResponse>(
                await response.Content.ReadFromJsonAsync<AdminScholarshipRequestListResponse>()));
        }

        Assert.Equal([10, 10, 5], pages.Select(page => page.Items.Count).ToArray());
        Assert.All(pages, page =>
        {
            Assert.Equal(10, page.PageSize);
            Assert.Equal(25, page.TotalItems);
            Assert.Equal(3, page.TotalPages);
        });
        var allItems = pages.SelectMany(page => page.Items).ToArray();
        Assert.Equal(25, allItems.Select(item => item.Id).Distinct().Count());
        Assert.Equal(
            allItems.OrderByDescending(item => item.SubmittedAt)
                .ThenByDescending(item => item.RequestNumber)
                .Select(item => item.Id),
            allItems.Select(item => item.Id));

        await AssertStatusCountAsync(client, "Pending", 10);
        await AssertStatusCountAsync(client, "Approved", 8);
        await AssertStatusCountAsync(client, "Rejected", 7);

        var scholarshipTypes = await client.GetFromJsonAsync<AdminScholarshipTypeOptionResponse[]>(
            "/api/v1/admin/scholarship-types");
        Assert.NotNull(scholarshipTypes);
        Assert.Equal(5, scholarshipTypes.Length);
        foreach (var scholarshipType in scholarshipTypes)
        {
            var typeResult = await GetListAsync(
                client,
                $"?search={Uri.EscapeDataString("นักศึกษาจำลอง")}" +
                $"&scholarshipTypeId={scholarshipType.Id}");
            Assert.Equal(5, typeResult.TotalItems);
            Assert.All(typeResult.Items, item =>
                Assert.Equal(scholarshipType.Id, item.ScholarshipTypeId));
        }

        var studentSearch = await GetListAsync(client, "?search=6600000025");
        var student = Assert.Single(studentSearch.Items);
        Assert.Equal("DEMO-2569-000025", student.RequestNumber);

        var combined = await GetListAsync(
            client,
            $"?search={Uri.EscapeDataString("นักศึกษาจำลอง 01")}" +
            $"&status=Pending&scholarshipTypeId={studentSearch.Items[0].ScholarshipTypeId}");
        Assert.Empty(combined.Items);

        var firstTypeId = allItems.Single(item => item.RequestNumber == "DEMO-2569-000001")
            .ScholarshipTypeId;
        combined = await GetListAsync(
            client,
            $"?search={Uri.EscapeDataString("นักศึกษาจำลอง 01")}" +
            $"&status=Pending&scholarshipTypeId={firstTypeId}");
        var combinedItem = Assert.Single(combined.Items);
        Assert.Equal("DEMO-2569-000001", combinedItem.RequestNumber);

        var literalWildcard = await GetListAsync(client, "?search=%25");
        Assert.Equal(0, literalWildcard.TotalItems);
    }

    [Fact]
    public async Task Detail_ShouldMaskBankDataAndExposeStatusCapabilitiesAndConsentAudit()
    {
        using var application = CreateApplication();
        using var client = CreateClient(application);
        await LoginAsync(client);

        using var pendingResponse = await client.GetAsync(
            "/api/v1/admin/scholarship-requests/50000000-0000-0000-0000-000000000005");
        pendingResponse.EnsureSuccessStatusCode();
        var pendingJson = await pendingResponse.Content.ReadAsStringAsync();
        var pending = JsonSerializer.Deserialize<AdminScholarshipRequestDetailResponse>(
            pendingJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(pending);
        Assert.Equal("Pending", pending.Status);
        Assert.True(pending.CanEdit);
        Assert.True(pending.CanDelete);
        Assert.True(pending.CanDecide);
        Assert.Equal("Staff", pending.SubmissionSource);
        Assert.Equal("Document", pending.ConsentMethod);
        Assert.False(string.IsNullOrWhiteSpace(pending.ConsentEvidenceNote));
        Assert.Equal("******0005", pending.MaskedBankAccountNumber);
        Assert.DoesNotContain("9900000005", pendingJson, StringComparison.Ordinal);
        Assert.DoesNotContain("protectedBankAccountNumber", pendingJson, StringComparison.OrdinalIgnoreCase);

        var approved = await client.GetFromJsonAsync<AdminScholarshipRequestDetailResponse>(
            "/api/v1/admin/scholarship-requests/50000000-0000-0000-0000-000000000011");
        Assert.NotNull(approved);
        Assert.Equal("Approved", approved.Status);
        Assert.False(approved.CanEdit);
        Assert.False(approved.CanDelete);
        Assert.False(approved.CanDecide);
        Assert.NotNull(approved.DecidedAt);
        Assert.False(string.IsNullOrWhiteSpace(approved.DecidedByName));
    }

    [Theory]
    [InlineData("?page=0", "Page")]
    [InlineData("?page=abc", "Page")]
    [InlineData("?pageSize=25", "PageSize")]
    [InlineData("?status=Reviewing", "Status")]
    [InlineData("?scholarshipTypeId=not-a-guid", "ScholarshipTypeId")]
    public async Task InvalidQuery_ShouldReturnValidationProblem(string query, string errorKey)
    {
        using var application = CreateApplication();
        using var client = CreateClient(application);
        await LoginAsync(client);

        using var response = await client.GetAsync($"/api/v1/admin/scholarship-requests{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VALIDATION_FAILED", problem.GetProperty("code").GetString());
        Assert.True(problem.GetProperty("errors").TryGetProperty(errorKey, out _));
    }

    [Fact]
    public async Task SoftDeletedRequest_ShouldBeExcludedFromListAndDetail()
    {
        var requestId = Guid.Parse("50000000-0000-0000-0000-000000000001");
        using var application = CreateApplication();
        using var client = CreateClient(application);
        await LoginAsync(client);

        await using (var context = database.CreateDbContext())
        {
            var request = await context.ScholarshipRequests.SingleAsync(item => item.Id == requestId);
            request.DeletedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }

        try
        {
            var list = await GetListAsync(client, "?search=DEMO-2569-000001");
            Assert.Equal(0, list.TotalItems);

            using var detailResponse = await client.GetAsync(
                $"/api/v1/admin/scholarship-requests/{requestId}");
            Assert.Equal(HttpStatusCode.NotFound, detailResponse.StatusCode);
            var problem = await detailResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(
                "SCHOLARSHIP_REQUEST_NOT_FOUND",
                problem.GetProperty("code").GetString());
        }
        finally
        {
            await using var context = database.CreateDbContext();
            var request = await context.ScholarshipRequests
                .IgnoreQueryFilters()
                .SingleAsync(item => item.Id == requestId);
            request.DeletedAt = null;
            request.DeletedById = null;
            await context.SaveChangesAsync();
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
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/v1/auth/antiforgery-token");
        Assert.NotNull(token);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new StaffLoginRequest
            {
                UserName = DemoUserName,
                Password = DemoPassword
            })
        };
        request.Headers.TryAddWithoutValidation(token.HeaderName, token.RequestToken);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<AdminScholarshipRequestListResponse> GetListAsync(
        HttpClient client,
        string query)
    {
        var response = await client.GetFromJsonAsync<AdminScholarshipRequestListResponse>(
            $"/api/v1/admin/scholarship-requests{query}");
        return Assert.IsType<AdminScholarshipRequestListResponse>(response);
    }

    private static async Task AssertStatusCountAsync(
        HttpClient client,
        string status,
        int expectedCount)
    {
        var result = await GetListAsync(
            client,
            $"?search={Uri.EscapeDataString("นักศึกษาจำลอง")}&status={status}");
        Assert.Equal(expectedCount, result.TotalItems);
        Assert.All(result.Items, item => Assert.Equal(status, item.Status));
    }
}
