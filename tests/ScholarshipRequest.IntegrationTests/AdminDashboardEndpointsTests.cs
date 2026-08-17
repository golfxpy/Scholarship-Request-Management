using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using ScholarshipRequest.Api.Data;
using ScholarshipRequest.Api.Domain.ScholarshipRequests;
using ScholarshipRequest.Shared.V1.Admin.Dashboard;
using ScholarshipRequest.Shared.V1.Authentication;

namespace ScholarshipRequest.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class AdminDashboardEndpointsTests(
    PostgreSqlFixture database,
    WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string DemoUserName = "admin";
    private const string DemoPassword = "Scholarship@2569";

    [Fact]
    public async Task Dashboard_ShouldRequireStaffAndMatchNonDeletedDatabaseAggregates()
    {
        using var application = CreateApplication();
        using var anonymousClient = CreateClient(application);
        using var anonymousResponse = await anonymousClient.GetAsync("/api/v1/admin/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Null(anonymousResponse.Headers.Location);

        using var client = CreateClient(application);
        await LoginAsync(client);
        using var response = await client.GetAsync("/api/v1/admin/dashboard");
        response.EnsureSuccessStatusCode();
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no-cache", response.Headers.Pragma.ToString(), StringComparison.OrdinalIgnoreCase);
        var dashboard = await response.Content.ReadFromJsonAsync<AdminDashboardSummaryResponse>();
        Assert.NotNull(dashboard);

        await using var context = database.CreateDbContext();
        var requests = await context.ScholarshipRequests.AsNoTracking().ToArrayAsync();
        Assert.Equal(requests.Length, dashboard.TotalRequests);
        Assert.Equal(
            requests.Count(item => item.Status == ScholarshipRequestStatus.Pending),
            dashboard.PendingRequests);
        Assert.Equal(
            requests.Count(item => item.Status == ScholarshipRequestStatus.Approved),
            dashboard.ApprovedRequests);
        Assert.Equal(
            requests.Count(item => item.Status == ScholarshipRequestStatus.Rejected),
            dashboard.RejectedRequests);
        Assert.Equal(requests.Sum(item => item.RequestedAmount), dashboard.TotalRequestedAmount);

        var types = await context.ScholarshipTypes.AsNoTracking().ToDictionaryAsync(item => item.Id);
        var expectedByType = requests
            .GroupBy(item => item.ScholarshipTypeId)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Count = group.Count(),
                    Amount = group.Sum(item => item.RequestedAmount)
                });
        Assert.Equal(expectedByType.Count, dashboard.ByScholarshipType.Count);
        foreach (var item in dashboard.ByScholarshipType)
        {
            Assert.Equal(types[item.ScholarshipTypeId].Name, item.ScholarshipTypeName);
            Assert.Equal(expectedByType[item.ScholarshipTypeId].Count, item.RequestCount);
            Assert.Equal(expectedByType[item.ScholarshipTypeId].Amount, item.TotalRequestedAmount);
        }
    }

    [Fact]
    public async Task Dashboard_ShouldExcludeSoftDeletedRequestFromEveryAggregate()
    {
        var requestId = Guid.Parse("50000000-0000-0000-0000-000000000003");
        using var application = CreateApplication();
        using var client = CreateClient(application);
        await LoginAsync(client);
        var baseline = await client.GetFromJsonAsync<AdminDashboardSummaryResponse>(
            "/api/v1/admin/dashboard");
        Assert.NotNull(baseline);

        decimal deletedAmount;
        await using (var context = database.CreateDbContext())
        {
            var request = await context.ScholarshipRequests.SingleAsync(item => item.Id == requestId);
            deletedAmount = request.RequestedAmount;
            request.DeletedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }

        try
        {
            var afterDelete = await client.GetFromJsonAsync<AdminDashboardSummaryResponse>(
                "/api/v1/admin/dashboard");
            Assert.NotNull(afterDelete);
            Assert.Equal(baseline.TotalRequests - 1, afterDelete.TotalRequests);
            Assert.Equal(baseline.PendingRequests - 1, afterDelete.PendingRequests);
            Assert.Equal(baseline.TotalRequestedAmount - deletedAmount, afterDelete.TotalRequestedAmount);
            Assert.Equal(
                afterDelete.TotalRequests,
                afterDelete.PendingRequests + afterDelete.ApprovedRequests + afterDelete.RejectedRequests);
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
}
