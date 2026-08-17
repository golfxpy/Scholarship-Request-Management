using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ScholarshipRequest.Shared.V1.Authentication;

namespace ScholarshipRequest.Client.Features.Authentication;

public sealed class StaffAuthenticationApi(HttpClient httpClient) : IStaffAuthenticationApi
{
    private const string AuthBasePath = "/api/v1/auth";

    public async Task<AuthenticationApiResult<StaffSessionResponse>> GetSessionAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync(
                $"{AuthBasePath}/session",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return AuthenticationApiResult<StaffSessionResponse>.Success(
                    StaffSessionResponse.Anonymous);
            }

            if (!response.IsSuccessStatusCode)
            {
                return AuthenticationApiResult<StaffSessionResponse>.Failure(
                    await ReadErrorAsync(response, cancellationToken));
            }

            var session = await response.Content.ReadFromJsonAsync<StaffSessionResponse>(
                cancellationToken);
            if (session is { IsAuthenticated: true } && !IsValidAuthenticatedSession(session))
            {
                return InvalidResponse<StaffSessionResponse>();
            }

            return session is null
                ? InvalidResponse<StaffSessionResponse>()
                : AuthenticationApiResult<StaffSessionResponse>.Success(session);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return NetworkFailure<StaffSessionResponse>();
        }
        catch (HttpRequestException)
        {
            return NetworkFailure<StaffSessionResponse>();
        }
        catch (JsonException)
        {
            return InvalidResponse<StaffSessionResponse>();
        }
        catch (NotSupportedException)
        {
            return InvalidResponse<StaffSessionResponse>();
        }
    }

    public async Task<AuthenticationApiResult<StaffSessionResponse>> LoginAsync(
        StaffLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenResult = await GetAntiforgeryTokenAsync(cancellationToken);
            if (!tokenResult.IsSuccess)
            {
                return AuthenticationApiResult<StaffSessionResponse>.Failure(tokenResult.Error!);
            }

            using var message = new HttpRequestMessage(HttpMethod.Post, $"{AuthBasePath}/login")
            {
                Content = JsonContent.Create(request)
            };
            message.Headers.TryAddWithoutValidation(
                tokenResult.Value!.HeaderName,
                tokenResult.Value.RequestToken);

            using var response = await httpClient.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return AuthenticationApiResult<StaffSessionResponse>.Failure(
                    await ReadErrorAsync(response, cancellationToken));
            }

            var session = await response.Content.ReadFromJsonAsync<StaffSessionResponse>(
                cancellationToken);
            return IsValidAuthenticatedSession(session)
                ? AuthenticationApiResult<StaffSessionResponse>.Success(session!)
                : InvalidResponse<StaffSessionResponse>();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return NetworkFailure<StaffSessionResponse>();
        }
        catch (HttpRequestException)
        {
            return NetworkFailure<StaffSessionResponse>();
        }
        catch (JsonException)
        {
            return InvalidResponse<StaffSessionResponse>();
        }
        catch (NotSupportedException)
        {
            return InvalidResponse<StaffSessionResponse>();
        }
    }

    public async Task<AuthenticationApiResult<bool>> LogoutAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenResult = await GetAntiforgeryTokenAsync(cancellationToken);
            if (!tokenResult.IsSuccess)
            {
                return AuthenticationApiResult<bool>.Failure(tokenResult.Error!);
            }

            using var message = new HttpRequestMessage(HttpMethod.Post, $"{AuthBasePath}/logout");
            message.Headers.TryAddWithoutValidation(
                tokenResult.Value!.HeaderName,
                tokenResult.Value.RequestToken);

            using var response = await httpClient.SendAsync(message, cancellationToken);
            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return AuthenticationApiResult<bool>.Success(true);
            }

            return AuthenticationApiResult<bool>.Failure(
                await ReadErrorAsync(response, cancellationToken));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return NetworkFailure<bool>();
        }
        catch (HttpRequestException)
        {
            return NetworkFailure<bool>();
        }
    }

    private async Task<AuthenticationApiResult<AntiforgeryTokenResponse>> GetAntiforgeryTokenAsync(
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"{AuthBasePath}/antiforgery-token",
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return AuthenticationApiResult<AntiforgeryTokenResponse>.Failure(
                await ReadErrorAsync(response, cancellationToken));
        }

        try
        {
            var token = await response.Content.ReadFromJsonAsync<AntiforgeryTokenResponse>(
                cancellationToken);
            return token is not null &&
                !string.IsNullOrWhiteSpace(token.RequestToken) &&
                !string.IsNullOrWhiteSpace(token.HeaderName)
                ? AuthenticationApiResult<AntiforgeryTokenResponse>.Success(token)
                : InvalidResponse<AntiforgeryTokenResponse>();
        }
        catch (JsonException)
        {
            return InvalidResponse<AntiforgeryTokenResponse>();
        }
        catch (NotSupportedException)
        {
            return InvalidResponse<AntiforgeryTokenResponse>();
        }
    }

    private static async Task<AuthenticationApiError> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;
        var fallbackMessage = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง",
            HttpStatusCode.Forbidden => "บัญชีนี้ไม่มีสิทธิ์เข้าถึงส่วนเจ้าหน้าที่",
            HttpStatusCode.BadRequest => "คำขอไม่ถูกต้อง กรุณาโหลดหน้าใหม่แล้วลองอีกครั้ง",
            _ when statusCode >= 500 => "ระบบยืนยันตัวตนยังไม่พร้อม กรุณาลองอีกครั้ง",
            _ => "ไม่สามารถดำเนินการได้ กรุณาลองอีกครั้ง"
        };

        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new AuthenticationApiError(statusCode, "HTTP_ERROR", fallbackMessage);
            }

            var root = document.RootElement;
            var code = TryGetString(root, "code") ?? "HTTP_ERROR";
            var message = TryGetString(root, "detail") ??
                TryGetString(root, "title") ??
                fallbackMessage;
            return new AuthenticationApiError(statusCode, code, message);
        }
        catch (JsonException)
        {
            return new AuthenticationApiError(statusCode, "HTTP_ERROR", fallbackMessage);
        }
    }

    private static string? TryGetString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static bool IsValidAuthenticatedSession(StaffSessionResponse? session) =>
        session is
        {
            IsAuthenticated: true,
            UserId: not null,
            Roles: not null
        } &&
        !string.IsNullOrWhiteSpace(session.UserName) &&
        session.Roles.Contains(AuthenticationConstants.StaffRole, StringComparer.Ordinal);

    private static AuthenticationApiResult<T> NetworkFailure<T>() =>
        AuthenticationApiResult<T>.Failure(new AuthenticationApiError(
            null,
            "NETWORK_ERROR",
            "ไม่สามารถเชื่อมต่อระบบยืนยันตัวตนได้ กรุณาตรวจสอบเครือข่ายแล้วลองอีกครั้ง"));

    private static AuthenticationApiResult<T> InvalidResponse<T>() =>
        AuthenticationApiResult<T>.Failure(new AuthenticationApiError(
            null,
            "INVALID_RESPONSE",
            "ระบบตอบกลับไม่ถูกต้อง กรุณาลองอีกครั้ง"));
}
