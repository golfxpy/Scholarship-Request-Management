using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ScholarshipRequest.Shared.V1.SystemInfo;

namespace ScholarshipRequest.IntegrationTests;

public sealed class SystemInfoEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetInfo_ShouldReturnVersionedApplicationContract()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/system/info");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SystemInfoResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Scholarship Request Management", payload.ApplicationName);
        Assert.Equal("v1", payload.ApiVersion);
    }
}
