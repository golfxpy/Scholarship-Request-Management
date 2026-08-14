using ScholarshipRequest.Api.Features.PublicScholarshipRequests;

namespace ScholarshipRequest.UnitTests;

public sealed class RequestNumberGeneratorTests
{
    [Theory]
    [InlineData(2569, 1, "SCH-2569-000001")]
    [InlineData(2569, 42, "SCH-2569-000042")]
    [InlineData(2570, 1_000_000, "SCH-2570-1000000")]
    public void Format_ShouldUseBuddhistYearAndMinimumSixDigitSequence(
        int buddhistYear,
        long value,
        string expected)
    {
        Assert.Equal(expected, RequestNumberGenerator.Format(buddhistYear, value));
    }

    [Fact]
    public void GetBuddhistYear_ShouldUseBangkokYearBoundary()
    {
        var beforeBangkokNewYear =
            new DateTimeOffset(2025, 12, 31, 16, 59, 59, TimeSpan.Zero);
        var afterBangkokNewYear =
            new DateTimeOffset(2025, 12, 31, 17, 0, 0, TimeSpan.Zero);

        Assert.Equal(2568, RequestNumberGenerator.GetBuddhistYear(beforeBangkokNewYear));
        Assert.Equal(2569, RequestNumberGenerator.GetBuddhistYear(afterBangkokNewYear));
    }
}
