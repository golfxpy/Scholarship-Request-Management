namespace ScholarshipRequest.Client.Features.Authentication;

public sealed record AuthenticationApiError(
    int? StatusCode,
    string Code,
    string Message);

public sealed record AuthenticationApiResult<T>(
    T? Value,
    AuthenticationApiError? Error)
{
    public bool IsSuccess => Error is null;

    public static AuthenticationApiResult<T> Success(T value) => new(value, null);

    public static AuthenticationApiResult<T> Failure(AuthenticationApiError error) =>
        new(default, error);
}
