using ScholarshipRequest.Client.Features.Authentication;

namespace ScholarshipRequest.UnitTests;

public sealed class ReturnUrlValidatorTests
{
    [Theory]
    [InlineData("/admin", "/admin")]
    [InlineData("/admin/requests?page=2", "/admin/requests?page=2")]
    [InlineData("/admin#summary", "/admin#summary")]
    public void Sanitize_ShouldAllowOnlyLocalAdminPaths(string candidate, string expected)
    {
        Assert.Equal(expected, ReturnUrlValidator.Sanitize(candidate));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://evil.example/admin")]
    [InlineData("//evil.example/admin")]
    [InlineData("/admin\\evil")]
    [InlineData("/administrator")]
    [InlineData("/apply")]
    public void Sanitize_ShouldRejectExternalAndNonAdminPaths(string? candidate)
    {
        Assert.Equal(ReturnUrlValidator.DefaultAdminPath, ReturnUrlValidator.Sanitize(candidate));
    }
}
