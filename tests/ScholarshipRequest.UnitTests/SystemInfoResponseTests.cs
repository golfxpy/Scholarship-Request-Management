using ScholarshipRequest.Shared.V1.SystemInfo;

namespace ScholarshipRequest.UnitTests;

public sealed class SystemInfoResponseTests
{
    [Fact]
    public void Constructor_ShouldPreserveVersionedContractValues()
    {
        var response = new SystemInfoResponse(
            ApplicationName: "Scholarship Request Management",
            ApiVersion: "v1");

        Assert.Equal("Scholarship Request Management", response.ApplicationName);
        Assert.Equal("v1", response.ApiVersion);
    }
}
