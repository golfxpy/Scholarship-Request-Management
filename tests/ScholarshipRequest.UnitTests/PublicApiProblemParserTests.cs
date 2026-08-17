using System.Net;
using ScholarshipRequest.Client.Features.PublicScholarshipRequests;

namespace ScholarshipRequest.UnitTests;

public sealed class PublicApiProblemParserTests
{
    [Fact]
    public void Parse_ShouldReadValidationCodeAndAllFieldMessages()
    {
        const string content = """
            {
              "title": "ข้อมูลไม่ถูกต้อง",
              "status": 400,
              "code": "VALIDATION_FAILED",
              "errors": {
                "StudentId": ["กรุณาระบุรหัสนักศึกษา", "รหัสนักศึกษาต้องไม่เกิน 20 ตัวอักษร"],
                "YearLevel": ["กรุณาเลือกชั้นปี"]
              }
            }
            """;

        var error = PublicApiProblemParser.Parse(HttpStatusCode.BadRequest, content);

        Assert.Equal(400, error.StatusCode);
        Assert.Equal("VALIDATION_FAILED", error.Code);
        Assert.Equal("ข้อมูลไม่ถูกต้อง", error.Title);
        Assert.Equal(2, error.FieldErrors["StudentId"].Length);
        Assert.Equal("กรุณาเลือกชั้นปี", Assert.Single(error.FieldErrors["YearLevel"]));
    }

    [Fact]
    public void Parse_ShouldReadConsentVersionConflict()
    {
        const string content = """
            {
              "title": "ประกาศความเป็นส่วนตัวมีการเปลี่ยนแปลง",
              "detail": "กรุณาอ่านประกาศฉบับใหม่",
              "status": 409,
              "code": "CONSENT_VERSION_CHANGED",
              "currentVersion": "POC-v2"
            }
            """;

        var error = PublicApiProblemParser.Parse(HttpStatusCode.Conflict, content);

        Assert.Equal("CONSENT_VERSION_CHANGED", error.Code);
        Assert.Equal("กรุณาอ่านประกาศฉบับใหม่", error.Detail);
        Assert.Empty(error.FieldErrors);
    }

    [Fact]
    public void Parse_ShouldUseSafeFallbackForMalformedResponse()
    {
        var error = PublicApiProblemParser.Parse(
            HttpStatusCode.ServiceUnavailable,
            "<html>not-json</html>");

        Assert.Equal(503, error.StatusCode);
        Assert.Null(error.Code);
        Assert.Equal("ระบบยังไม่พร้อมให้บริการ", error.Title);
        Assert.Empty(error.FieldErrors);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("\"proxy error\"")]
    public void Parse_ShouldUseSafeFallbackForNonObjectJson(string content)
    {
        var error = PublicApiProblemParser.Parse(
            HttpStatusCode.BadGateway,
            content);

        Assert.Equal(502, error.StatusCode);
        Assert.Null(error.Code);
        Assert.Equal("ไม่สามารถดำเนินการได้", error.Title);
        Assert.Empty(error.FieldErrors);
    }
}
