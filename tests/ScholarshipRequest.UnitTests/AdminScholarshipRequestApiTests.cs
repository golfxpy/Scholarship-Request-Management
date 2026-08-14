using System.Net;
using System.Net.Http.Json;
using ScholarshipRequest.Client.Features.Admin.ScholarshipRequests;
using ScholarshipRequest.Shared.V1.Admin.ScholarshipRequests;
using ScholarshipRequest.Shared.V1.Authentication;
using ScholarshipRequest.Shared.V1.ScholarshipRequests;

namespace ScholarshipRequest.UnitTests;

public sealed class AdminScholarshipRequestApiTests
{
    [Fact]
    public void Query_ShouldEncodeCombinedFiltersAndKeepPageSizeServerControlled()
    {
        var typeId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var query = new AdminScholarshipRequestQuery(
            2,
            " นักศึกษา 01 & 02 ",
            "Pending",
            typeId.ToString());

        var uri = query.ToApiUri();

        Assert.StartsWith("/api/v1/admin/scholarship-requests?", uri, StringComparison.Ordinal);
        Assert.Contains("page=2", uri, StringComparison.Ordinal);
        Assert.Contains(Uri.EscapeDataString("นักศึกษา 01 & 02"), uri, StringComparison.Ordinal);
        Assert.Contains("status=Pending", uri, StringComparison.Ordinal);
        Assert.Contains($"scholarshipTypeId={typeId}", uri, StringComparison.Ordinal);
        Assert.DoesNotContain("pageSize", uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Query_ShouldResetInvalidPageAndTrimOptionalValues()
    {
        var query = AdminScholarshipRequestQuery.FromQueryStrings(
            "invalid",
            "   ",
            " Approved ",
            null);

        Assert.Equal(1, query.Page);
        Assert.Null(query.Search);
        Assert.Equal("Approved", query.Status);
        Assert.Equal("/admin/requests?status=Approved", query.ToPageUri());
    }

    [Fact]
    public async Task GetListAsync_ShouldReadPagedResponseAndUseExpectedUri()
    {
        var expected = new AdminScholarshipRequestListResponse([], 1, 10, 0, 0);
        using var handler = new StubHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(
                "/api/v1/admin/scholarship-requests?status=Pending",
                request.RequestUri?.PathAndQuery);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expected)
            });
        });
        using var client = CreateClient(handler);
        var api = new AdminScholarshipRequestApi(client);

        var result = await api.GetListAsync(new AdminScholarshipRequestQuery(Status: "Pending"));

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value?.PageSize);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("null")]
    public async Task GetListAsync_ShouldRejectMalformedSuccessPayload(string payload)
    {
        using var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload)
            }));
        using var client = CreateClient(handler);
        var api = new AdminScholarshipRequestApi(client);

        var result = await api.GetListAsync(new AdminScholarshipRequestQuery());

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_RESPONSE", result.Error?.Code);
    }

    [Fact]
    public async Task GetDetailAsync_ShouldPreserveNotFoundProblemCode()
    {
        const string problem = """
            {
              "title": "ไม่พบคำขอทุนการศึกษา",
              "status": 404,
              "code": "SCHOLARSHIP_REQUEST_NOT_FOUND"
            }
            """;
        using var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(problem)
            }));
        using var client = CreateClient(handler);
        var api = new AdminScholarshipRequestApi(client);

        var result = await api.GetDetailAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.Error?.StatusCode);
        Assert.Equal("SCHOLARSHIP_REQUEST_NOT_FOUND", result.Error?.Code);
    }

    [Fact]
    public async Task GetListAsync_ShouldMapNetworkFailureWithoutThrowing()
    {
        using var handler = new StubHttpMessageHandler(
            (_, _) => throw new HttpRequestException("test-only failure"));
        using var client = CreateClient(handler);
        var api = new AdminScholarshipRequestApi(client);

        var result = await api.GetListAsync(new AdminScholarshipRequestQuery());

        Assert.False(result.IsSuccess);
        Assert.Equal("NETWORK_ERROR", result.Error?.Code);
    }

    [Fact]
    public async Task GetListAsync_ShouldPropagateCallerCancellation()
    {
        using var handler = new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = CreateClient(handler);
        var api = new AdminScholarshipRequestApi(client);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            api.GetListAsync(new AdminScholarshipRequestQuery(), cancellation.Token));
    }

    [Fact]
    public void DetailContract_ShouldNotExposeRawOrProtectedBankFields()
    {
        var propertyNames = typeof(AdminScholarshipRequestDetailResponse)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Contains("MaskedBankAccountNumber", propertyNames);
        Assert.DoesNotContain("BankAccountNumber", propertyNames);
        Assert.DoesNotContain("ProtectedBankAccountNumber", propertyNames);
        Assert.DoesNotContain("BankAccountLastFour", propertyNames);
    }

    [Fact]
    public async Task CreateAsync_ShouldFetchAntiforgeryTokenAndAttachItToRequest()
    {
        var calls = 0;
        var createdAt = DateTimeOffset.Parse("2026-08-14T12:00:00+07:00");
        var createdId = Guid.NewGuid();
        using var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            calls++;
            if (calls == 1)
            {
                Assert.Equal("/api/v1/auth/antiforgery-token", request.RequestUri?.AbsolutePath);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new AntiforgeryTokenResponse(
                        "admin-token",
                        AuthenticationConstants.AntiforgeryHeaderName))
                };
            }

            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/v1/admin/scholarship-requests", request.RequestUri?.AbsolutePath);
            Assert.Equal(
                "admin-token",
                Assert.Single(request.Headers.GetValues(AuthenticationConstants.AntiforgeryHeaderName)));
            var json = await request.Content!.ReadAsStringAsync(cancellationToken);
            Assert.Contains("\"consentMethod\":\"Document\"", json, StringComparison.Ordinal);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new CreateScholarshipRequestResponse(
                    createdId,
                    "SCH-2569-000001",
                    "Pending",
                    createdAt))
            };
        });
        using var client = CreateClient(handler);
        var api = new AdminScholarshipRequestApi(client);

        var result = await api.CreateAsync(new CreateAdminScholarshipRequest
        {
            ConsentMethod = "Document"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(createdId, result.Value?.Id);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task DecideAsync_ShouldMapServerFieldErrors()
    {
        var calls = 0;
        using var handler = new StubHttpMessageHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(calls == 1
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new AntiforgeryTokenResponse(
                        "token",
                        AuthenticationConstants.AntiforgeryHeaderName))
                }
                : new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("""
                        {
                          "title": "ข้อมูลไม่ถูกต้อง",
                          "code": "VALIDATION_FAILED",
                          "errors": { "Note": ["การปฏิเสธคำขอต้องระบุหมายเหตุ"] }
                        }
                        """)
                });
        });
        using var client = CreateClient(handler);
        var api = new AdminScholarshipRequestApi(client);

        var result = await api.DecideAsync(
            Guid.NewGuid(),
            new AdminScholarshipRequestDecisionRequest { Decision = "Rejected" });

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Error?.Code);
        Assert.Equal(
            "การปฏิเสธคำขอต้องระบุหมายเหตุ",
            Assert.Single(result.Error!.FieldErrors["Note"]));
    }

    private static HttpClient CreateClient(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://example.test") };

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
