using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ScholarshipRequest.Api.Data;
using ScholarshipRequest.Api.Data.Identity;
using ScholarshipRequest.Api.Domain.ScholarshipRequests;
using ScholarshipRequest.Api.Security;
using ScholarshipRequest.Shared.V1.Authentication;

namespace ScholarshipRequest.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class DevelopmentDemoDataSeederTests(
    PostgreSqlFixture database,
    WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string DemoUserName = "admin";
    private const string DemoPassword = "Scholarship@2569";

    [Fact]
    public async Task DevelopmentStartup_ShouldCreateHashedAdminAnd25IdempotentFictionalRequests()
    {
        using (var firstApplication = CreateDevelopmentApplication())
        {
            using var client = firstApplication.CreateClient();
            using var response = await client.GetAsync("/health/live");
            response.EnsureSuccessStatusCode();

            await AssertDevelopmentSeedAsync(firstApplication);
        }

        using (var restartedApplication = CreateDevelopmentApplication())
        {
            using var client = restartedApplication.CreateClient();
            using var response = await client.GetAsync("/health/live");
            response.EnsureSuccessStatusCode();

            await AssertDevelopmentSeedAsync(restartedApplication);
        }
    }

    [Fact]
    public async Task NonDevelopmentEnvironment_ShouldIgnoreEnabledDemoSeed()
    {
        var outsideDevelopmentUserName = $"outside-{Guid.NewGuid():N}";
        using var application = factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Testing")
                .UseSetting("ConnectionStrings:DefaultConnection", database.ConnectionString)
                .UseSetting("DevelopmentDemoSeed:Enabled", "true")
                .UseSetting("DevelopmentDemoSeed:AdminUserName", outsideDevelopmentUserName)
                .UseSetting("DevelopmentDemoSeed:AdminPassword", DemoPassword));
        using var client = application.CreateClient();
        using var response = await client.GetAsync("/health/live");
        response.EnsureSuccessStatusCode();

        using (var scope = application.Services.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentDemoDataSeeder>();
            await seeder.SeedAsync();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.Null(await userManager.FindByNameAsync(outsideDevelopmentUserName));
        }
    }

    [Fact]
    public async Task DevelopmentSeed_ShouldFailFastInsteadOfPromotingCollidingUserName()
    {
        var collidingUserName = $"collision-{Guid.NewGuid():N}";
        using var application = factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Testing")
                .UseSetting("ConnectionStrings:DefaultConnection", database.ConnectionString)
                .UseSetting("DevelopmentDemoSeed:Enabled", "true")
                .UseSetting("DevelopmentDemoSeed:AdminUserName", collidingUserName)
                .UseSetting("DevelopmentDemoSeed:AdminPassword", DemoPassword));
        using var client = application.CreateClient();
        using var response = await client.GetAsync("/health/live");
        response.EnsureSuccessStatusCode();

        using (var scope = application.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var now = DateTimeOffset.UtcNow;
            var foreignUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = collidingUserName,
                FullName = "Existing non-demo account",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            Assert.True((await userManager.CreateAsync(foreignUser, DemoPassword)).Succeeded);

            var developmentEnvironment = new StubHostEnvironment { EnvironmentName = "Development" };
            var seeder = ActivatorUtilities.CreateInstance<DevelopmentDemoDataSeeder>(
                scope.ServiceProvider,
                developmentEnvironment);
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => seeder.SeedAsync());

            Assert.Contains("non-demo account", exception.Message, StringComparison.Ordinal);
            Assert.False(await userManager.IsInRoleAsync(
                foreignUser,
                AuthenticationConstants.StaffRole));
        }
    }

    private WebApplicationFactory<Program> CreateDevelopmentApplication() =>
        factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development")
                .UseSetting("ConnectionStrings:DefaultConnection", database.ConnectionString)
                .UseSetting("DevelopmentDemoSeed:Enabled", "true")
                .UseSetting("DevelopmentDemoSeed:AdminUserName", DemoUserName)
                .UseSetting("DevelopmentDemoSeed:AdminPassword", DemoPassword));

    private static async Task AssertDevelopmentSeedAsync(
        WebApplicationFactory<Program> application)
    {
        using var scope = application.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var protector = scope.ServiceProvider.GetRequiredService<IBankAccountProtector>();

        var admin = await userManager.FindByNameAsync(DemoUserName);
        Assert.NotNull(admin);
        Assert.True(admin.IsActive);
        Assert.NotNull(admin.PasswordHash);
        Assert.NotEqual(DemoPassword, admin.PasswordHash);
        Assert.True(await userManager.CheckPasswordAsync(admin, DemoPassword));
        Assert.True(await userManager.IsInRoleAsync(admin, AuthenticationConstants.StaffRole));

        var requests = await context.ScholarshipRequests
            .AsNoTracking()
            .Where(request => request.RequestNumber.StartsWith("DEMO-2569-"))
            .OrderBy(request => request.RequestNumber)
            .ToArrayAsync();

        Assert.Equal(DevelopmentDemoDataSeeder.DemoRequestCount, requests.Length);
        Assert.Equal(10, requests.Count(request => request.Status == ScholarshipRequestStatus.Pending));
        Assert.Equal(8, requests.Count(request => request.Status == ScholarshipRequestStatus.Approved));
        Assert.Equal(7, requests.Count(request => request.Status == ScholarshipRequestStatus.Rejected));
        Assert.Equal(5, requests.Select(request => request.ScholarshipTypeId).Distinct().Count());
        Assert.All(requests, request =>
        {
            Assert.StartsWith("DEMO-2569-", request.RequestNumber, StringComparison.Ordinal);
            Assert.Contains("จำลอง", request.StudentName, StringComparison.Ordinal);
            Assert.EndsWith("@example.invalid", request.Email, StringComparison.Ordinal);
            var bankAccount = protector.Unprotect(request.ProtectedBankAccountNumber);
            Assert.NotEqual(bankAccount, request.ProtectedBankAccountNumber);
            Assert.EndsWith(request.BankAccountLastFour, bankAccount, StringComparison.Ordinal);
        });
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = string.Empty;

        public string ApplicationName { get; set; } = "IntegrationTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}
