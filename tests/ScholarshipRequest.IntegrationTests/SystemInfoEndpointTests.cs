using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using ScholarshipRequest.Shared.V1.SystemInfo;

namespace ScholarshipRequest.IntegrationTests;

public sealed class SystemInfoEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetInfo_ShouldReturnVersionedApplicationContract()
    {
        using var application = factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Testing")
                .UseSetting(
                    "ConnectionStrings:DefaultConnection",
                    "Host=localhost;Database=scholarship_test;Username=postgres;Password=postgres"));
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/api/v1/system/info");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SystemInfoResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Scholarship Request Management", payload.ApplicationName);
        Assert.Equal("v1", payload.ApiVersion);
    }

    [Fact]
    public async Task LiveHealth_ShouldNotDependOnDatabaseAvailability()
    {
        using var application = factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Testing")
                .UseSetting(
                    "ConnectionStrings:DefaultConnection",
                    "Host=localhost;Database=unavailable;Username=postgres;Password=postgres"));
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/health/live");

        response.EnsureSuccessStatusCode();
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
