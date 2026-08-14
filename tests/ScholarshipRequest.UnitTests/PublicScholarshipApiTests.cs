using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ScholarshipRequest.Client.Features.PublicScholarshipRequests;
using ScholarshipRequest.Shared.V1.ScholarshipRequests;

namespace ScholarshipRequest.UnitTests;

public sealed class PublicScholarshipApiTests
{
    [Fact]
    public async Task CreateRequestAsync_ShouldSendCurrentContractAndReadCreatedResponse()
    {
        string? capturedJson = null;
        var submittedAt = DateTimeOffset.Parse("2026-08-14T00:00:00+07:00");
        using var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(
                "/api/v1/public/scholarship-requests",
                request.RequestUri?.AbsolutePath);
            capturedJson = await request.Content!.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new CreateScholarshipRequestResponse(
                    Guid.Parse("d66ae59d-6ec7-4ea3-a30e-041b6f1603da"),
                    "SCH-2569-000101",
                    "Pending",
                    submittedAt))
            };
        });
        using var httpClient = CreateHttpClient(handler);
        var api = new PublicScholarshipApi(httpClient);
        var request = CreateRequest();

        var result = await api.CreateRequestAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("SCH-2569-000101", result.Value.RequestNumber);
        Assert.NotNull(capturedJson);
        using var payload = JsonDocument.Parse(capturedJson);
        Assert.Equal(
            "POC-v1",
            payload.RootElement.GetProperty("pdpaNoticeVersion").GetString());
        Assert.Equal(
            "123-456-7890",
            payload.RootElement.GetProperty("bankAccountNumber").GetString());
    }

    [Fact]
    public async Task CreateRequestAsync_ShouldMapServerValidationErrors()
    {
        const string problem = """
            {
              "title": "ข้อมูลที่ส่งไม่ถูกต้อง",
              "status": 400,
              "code": "VALIDATION_FAILED",
              "errors": {
                "Email": ["รูปแบบอีเมลไม่ถูกต้อง"]
              }
            }
            """;
        using var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(problem)
            }));
        using var httpClient = CreateHttpClient(handler);
        var api = new PublicScholarshipApi(httpClient);

        var result = await api.CreateRequestAsync(CreateRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Error?.Code);
        Assert.Equal(
            "รูปแบบอีเมลไม่ถูกต้อง",
            Assert.Single(result.Error!.FieldErrors["Email"]));
    }

    [Fact]
    public async Task GetPdpaNoticeAsync_ShouldReturnNetworkErrorWithoutThrowing()
    {
        using var handler = new StubHttpMessageHandler(
            (_, _) => throw new HttpRequestException("test-only failure"));
        using var httpClient = CreateHttpClient(handler);
        var api = new PublicScholarshipApi(httpClient);

        var result = await api.GetPdpaNoticeAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("NETWORK_ERROR", result.Error?.Code);
    }

    [Fact]
    public async Task GetScholarshipTypesAsync_ShouldRejectMalformedSuccessPayload()
    {
        using var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not-json")
            }));
        using var httpClient = CreateHttpClient(handler);
        var api = new PublicScholarshipApi(httpClient);

        var result = await api.GetScholarshipTypesAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_RESPONSE", result.Error?.Code);
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler) =>
        new(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };

    private static CreatePublicScholarshipRequest CreateRequest() =>
        new()
        {
            StudentId = "6612345678",
            StudentName = "นักศึกษาทดสอบ",
            AcademicUnitId = Guid.Parse("10000000-0000-0000-0000-000000000012"),
            FacultyName = "คณะวิศวกรรมศาสตร์",
            Major = "วิศวกรรมคอมพิวเตอร์",
            YearLevel = 3,
            Gpax = 3.25m,
            Email = "student@example.com",
            ScholarshipTypeId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            RequestedAmount = 10_000m,
            BankAccountNumber = "123-456-7890",
            Reason = "ต้องการทุนเพื่อสนับสนุนค่าใช้จ่ายด้านการศึกษา",
            PdpaConsent = true,
            PdpaNoticeVersion = "POC-v1"
        };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            sendAsync(request, cancellationToken);
    }
}
