namespace ScholarshipRequest.Client.Features.Admin.ScholarshipRequests;

public sealed record AdminApiError(
    int? StatusCode,
    string? Code,
    string Title,
    string? Detail,
    IReadOnlyDictionary<string, string[]> FieldErrors)
{
    public static AdminApiError Network() => new(
        null,
        "NETWORK_ERROR",
        "ไม่สามารถติดต่อระบบได้",
        "กรุณาตรวจสอบการเชื่อมต่อแล้วลองอีกครั้ง",
        new Dictionary<string, string[]>(StringComparer.Ordinal));

    public static AdminApiError InvalidResponse() => new(
        null,
        "INVALID_RESPONSE",
        "ระบบตอบกลับในรูปแบบที่ไม่ถูกต้อง",
        "กรุณาลองอีกครั้ง หากปัญหายังคงอยู่โปรดแจ้งผู้ดูแลระบบ",
        new Dictionary<string, string[]>(StringComparer.Ordinal));
}

public sealed record AdminApiResult<T>(T? Value, AdminApiError? Error)
{
    public bool IsSuccess => Error is null;

    public static AdminApiResult<T> Success(T value) => new(value, null);

    public static AdminApiResult<T> Failure(AdminApiError error) => new(default, error);
}
