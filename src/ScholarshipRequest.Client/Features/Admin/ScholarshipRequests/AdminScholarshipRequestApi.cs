using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ScholarshipRequest.Shared.V1.Admin.Dashboard;
using ScholarshipRequest.Shared.V1.Admin.ScholarshipRequests;
using ScholarshipRequest.Shared.V1.Authentication;
using ScholarshipRequest.Shared.V1.ScholarshipRequests;

namespace ScholarshipRequest.Client.Features.Admin.ScholarshipRequests;

public sealed class AdminScholarshipRequestApi(HttpClient httpClient)
    : IAdminScholarshipRequestApi
{
    public Task<AdminApiResult<AdminDashboardSummaryResponse>> GetDashboardAsync(
        CancellationToken cancellationToken = default) =>
        GetAsync<AdminDashboardSummaryResponse>(
            "/api/v1/admin/dashboard",
            cancellationToken);

    public Task<AdminApiResult<AdminScholarshipRequestListResponse>> GetListAsync(
        AdminScholarshipRequestQuery query,
        CancellationToken cancellationToken = default) =>
        GetAsync<AdminScholarshipRequestListResponse>(query.ToApiUri(), cancellationToken);

    public Task<AdminApiResult<AdminScholarshipRequestDetailResponse>> GetDetailAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        GetAsync<AdminScholarshipRequestDetailResponse>(
            $"/api/v1/admin/scholarship-requests/{id}",
            cancellationToken);

    public async Task<AdminApiResult<IReadOnlyList<AdminScholarshipTypeOptionResponse>>>
        GetScholarshipTypesAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<AdminScholarshipTypeOptionResponse[]>(
            "/api/v1/admin/scholarship-types",
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? AdminApiResult<IReadOnlyList<AdminScholarshipTypeOptionResponse>>.Success(result.Value)
            : AdminApiResult<IReadOnlyList<AdminScholarshipTypeOptionResponse>>.Failure(
                result.Error ?? AdminApiError.InvalidResponse());
    }

    public async Task<AdminApiResult<CreateScholarshipRequestResponse>> CreateAsync(
        CreateAdminScholarshipRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await SendMutationAsync(
            HttpMethod.Post,
            "/api/v1/admin/scholarship-requests",
            request,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return AdminApiResult<CreateScholarshipRequestResponse>.Failure(result.Error!);
        }

        try
        {
            var created = JsonSerializer.Deserialize<CreateScholarshipRequestResponse>(
                result.Value ?? string.Empty,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return created is null
                ? AdminApiResult<CreateScholarshipRequestResponse>.Failure(
                    AdminApiError.InvalidResponse())
                : AdminApiResult<CreateScholarshipRequestResponse>.Success(created);
        }
        catch (JsonException)
        {
            return AdminApiResult<CreateScholarshipRequestResponse>.Failure(
                AdminApiError.InvalidResponse());
        }
    }

    public async Task<AdminApiResult<bool>> UpdateAsync(
        Guid id,
        UpdateAdminScholarshipRequest request,
        CancellationToken cancellationToken = default) =>
        ToBoolean(await SendMutationAsync(
            HttpMethod.Put,
            $"/api/v1/admin/scholarship-requests/{id}",
            request,
            cancellationToken));

    public async Task<AdminApiResult<bool>> DeleteAsync(
        Guid id,
        DateTimeOffset expectedUpdatedAt,
        CancellationToken cancellationToken = default) =>
        ToBoolean(await SendMutationAsync<object?>(
            HttpMethod.Delete,
            $"/api/v1/admin/scholarship-requests/{id}" +
                $"?expectedUpdatedAt={Uri.EscapeDataString(expectedUpdatedAt.ToString("O"))}",
            null,
            cancellationToken));

    public async Task<AdminApiResult<bool>> DecideAsync(
        Guid id,
        AdminScholarshipRequestDecisionRequest request,
        CancellationToken cancellationToken = default) =>
        ToBoolean(await SendMutationAsync(
            HttpMethod.Post,
            $"/api/v1/admin/scholarship-requests/{id}/decision",
            request,
            cancellationToken));

    private async Task<AdminApiResult<T>> GetAsync<T>(
        string requestUri,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return AdminApiResult<T>.Failure(ParseProblem(response.StatusCode, content));
            }

            try
            {
                var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
                return value is null
                    ? AdminApiResult<T>.Failure(AdminApiError.InvalidResponse())
                    : AdminApiResult<T>.Success(value);
            }
            catch (JsonException)
            {
                return AdminApiResult<T>.Failure(AdminApiError.InvalidResponse());
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminApiResult<T>.Failure(AdminApiError.Network());
        }
        catch (HttpRequestException)
        {
            return AdminApiResult<T>.Failure(AdminApiError.Network());
        }
    }

    private async Task<AdminApiResult<string?>> SendMutationAsync<TRequest>(
        HttpMethod method,
        string requestUri,
        TRequest content,
        CancellationToken cancellationToken)
    {
        try
        {
            using var tokenResponse = await httpClient.GetAsync(
                "/api/v1/auth/antiforgery-token",
                cancellationToken);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                var tokenError = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
                return AdminApiResult<string?>.Failure(
                    ParseProblem(tokenResponse.StatusCode, tokenError));
            }

            AntiforgeryTokenResponse? token;
            try
            {
                token = await tokenResponse.Content.ReadFromJsonAsync<AntiforgeryTokenResponse>(
                    cancellationToken);
            }
            catch (JsonException)
            {
                return AdminApiResult<string?>.Failure(AdminApiError.InvalidResponse());
            }

            if (token is null ||
                string.IsNullOrWhiteSpace(token.HeaderName) ||
                string.IsNullOrWhiteSpace(token.RequestToken))
            {
                return AdminApiResult<string?>.Failure(AdminApiError.InvalidResponse());
            }

            using var message = new HttpRequestMessage(method, requestUri);
            if (content is not null)
            {
                message.Content = JsonContent.Create(content);
            }

            message.Headers.TryAddWithoutValidation(token.HeaderName, token.RequestToken);
            using var response = await httpClient.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return AdminApiResult<string?>.Failure(
                    ParseProblem(response.StatusCode, responseContent));
            }

            var successContent = response.Content.Headers.ContentLength == 0
                ? null
                : await response.Content.ReadAsStringAsync(cancellationToken);
            return AdminApiResult<string?>.Success(successContent);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AdminApiResult<string?>.Failure(AdminApiError.Network());
        }
        catch (HttpRequestException)
        {
            return AdminApiResult<string?>.Failure(AdminApiError.Network());
        }
    }

    private static AdminApiResult<bool> ToBoolean(AdminApiResult<string?> result) =>
        result.IsSuccess
            ? AdminApiResult<bool>.Success(true)
            : AdminApiResult<bool>.Failure(result.Error!);

    private static AdminApiError ParseProblem(HttpStatusCode statusCode, string? content)
    {
        var fallbackTitle = statusCode switch
        {
            HttpStatusCode.BadRequest => "ข้อมูลที่ส่งไม่ถูกต้อง",
            HttpStatusCode.Unauthorized => "เซสชันหมดอายุ",
            HttpStatusCode.Forbidden => "คุณไม่มีสิทธิ์ดำเนินการนี้",
            HttpStatusCode.NotFound => "ไม่พบข้อมูลที่ต้องการ",
            HttpStatusCode.Conflict => "ข้อมูลมีการเปลี่ยนแปลง",
            _ => "ไม่สามารถดำเนินการได้"
        };
        if (string.IsNullOrWhiteSpace(content))
        {
            return CreateFallback();
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return CreateFallback();
            }

            var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
            if (root.TryGetProperty("errors", out var errorsElement) &&
                errorsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in errorsElement.EnumerateObject())
                {
                    var messages = property.Value.ValueKind == JsonValueKind.Array
                        ? property.Value.EnumerateArray()
                            .Where(item => item.ValueKind == JsonValueKind.String)
                            .Select(item => item.GetString())
                            .Where(message => !string.IsNullOrWhiteSpace(message))
                            .Select(message => message!)
                            .ToArray()
                        : [];
                    if (messages.Length > 0)
                    {
                        errors[property.Name] = messages;
                    }
                }
            }

            return new AdminApiError(
                (int)statusCode,
                ReadString(root, "code"),
                ReadString(root, "title") ?? fallbackTitle,
                ReadString(root, "detail"),
                errors);
        }
        catch (JsonException)
        {
            return CreateFallback();
        }

        AdminApiError CreateFallback() => new(
            (int)statusCode,
            null,
            fallbackTitle,
            null,
            new Dictionary<string, string[]>(StringComparer.Ordinal));
    }

    private static string? ReadString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var element) &&
        element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
}
