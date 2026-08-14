using System.Net.Http.Json;
using System.Text.Json;
using ScholarshipRequest.Shared.V1.Masters;
using ScholarshipRequest.Shared.V1.Privacy;
using ScholarshipRequest.Shared.V1.ScholarshipRequests;

namespace ScholarshipRequest.Client.Features.PublicScholarshipRequests;

public sealed class PublicScholarshipApi(HttpClient httpClient) : IPublicScholarshipApi
{
    private const string PublicBasePath = "api/v1/public";

    public Task<PublicApiResult<IReadOnlyList<ScholarshipTypeResponse>>> GetScholarshipTypesAsync(
        CancellationToken cancellationToken = default) =>
        GetListAsync<ScholarshipTypeResponse>(
            $"{PublicBasePath}/scholarship-types",
            cancellationToken);

    public Task<PublicApiResult<IReadOnlyList<AcademicUnitResponse>>> SearchAcademicUnitsAsync(
        string? query,
        CancellationToken cancellationToken = default)
    {
        var encodedQuery = Uri.EscapeDataString(query?.Trim() ?? string.Empty);
        return GetListAsync<AcademicUnitResponse>(
            $"{PublicBasePath}/academic-units?query={encodedQuery}",
            cancellationToken);
    }

    public Task<PublicApiResult<PdpaNoticeResponse>> GetPdpaNoticeAsync(
        CancellationToken cancellationToken = default) =>
        GetAsync<PdpaNoticeResponse>($"{PublicBasePath}/pdpa-notice", cancellationToken);

    public async Task<PublicApiResult<CreateScholarshipRequestResponse>> CreateRequestAsync(
        CreatePublicScholarshipRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                $"{PublicBasePath}/scholarship-requests",
                request,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return PublicApiResult<CreateScholarshipRequestResponse>.Failure(
                    await ReadErrorAsync(response, cancellationToken));
            }

            var value = await response.Content
                .ReadFromJsonAsync<CreateScholarshipRequestResponse>(cancellationToken);
            return value is null
                ? PublicApiResult<CreateScholarshipRequestResponse>.Failure(InvalidResponse())
                : PublicApiResult<CreateScholarshipRequestResponse>.Success(value);
        }
        catch (HttpRequestException)
        {
            return PublicApiResult<CreateScholarshipRequestResponse>.Failure(PublicApiError.Network());
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return PublicApiResult<CreateScholarshipRequestResponse>.Failure(PublicApiError.Network());
        }
        catch (JsonException)
        {
            return PublicApiResult<CreateScholarshipRequestResponse>.Failure(InvalidResponse());
        }
        catch (NotSupportedException)
        {
            return PublicApiResult<CreateScholarshipRequestResponse>.Failure(InvalidResponse());
        }
    }

    private async Task<PublicApiResult<IReadOnlyList<T>>> GetListAsync<T>(
        string requestUri,
        CancellationToken cancellationToken)
    {
        var result = await GetAsync<T[]>(requestUri, cancellationToken);
        return result.IsSuccess
            ? PublicApiResult<IReadOnlyList<T>>.Success(result.Value ?? [])
            : PublicApiResult<IReadOnlyList<T>>.Failure(result.Error!);
    }

    private async Task<PublicApiResult<T>> GetAsync<T>(
        string requestUri,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return PublicApiResult<T>.Failure(await ReadErrorAsync(response, cancellationToken));
            }

            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            return value is null
                ? PublicApiResult<T>.Failure(InvalidResponse())
                : PublicApiResult<T>.Success(value);
        }
        catch (HttpRequestException)
        {
            return PublicApiResult<T>.Failure(PublicApiError.Network());
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return PublicApiResult<T>.Failure(PublicApiError.Network());
        }
        catch (JsonException)
        {
            return PublicApiResult<T>.Failure(InvalidResponse());
        }
        catch (NotSupportedException)
        {
            return PublicApiResult<T>.Failure(InvalidResponse());
        }
    }

    private static async Task<PublicApiError> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return PublicApiProblemParser.Parse(response.StatusCode, content);
    }

    private static PublicApiError InvalidResponse() => new(
        null,
        "INVALID_RESPONSE",
        "ระบบตอบกลับข้อมูลไม่ครบถ้วน",
        "กรุณาลองใหม่อีกครั้ง",
        new Dictionary<string, string[]>(StringComparer.Ordinal));
}
