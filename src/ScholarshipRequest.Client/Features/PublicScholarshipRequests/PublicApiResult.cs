namespace ScholarshipRequest.Client.Features.PublicScholarshipRequests;

public sealed record PublicApiError(
    int? StatusCode,
    string? Code,
    string Title,
    string? Detail,
    IReadOnlyDictionary<string, string[]> FieldErrors)
{
    public static PublicApiError Network() => new(
        null,
        "NETWORK_ERROR",
        "ไม่สามารถติดต่อระบบได้",
        "กรุณาตรวจสอบการเชื่อมต่อแล้วลองอีกครั้ง",
        new Dictionary<string, string[]>(StringComparer.Ordinal));
}

public sealed record PublicApiResult<T>(T? Value, PublicApiError? Error)
{
    public bool IsSuccess => Error is null;

    public static PublicApiResult<T> Success(T value) => new(value, null);

    public static PublicApiResult<T> Failure(PublicApiError error) => new(default, error);
}
