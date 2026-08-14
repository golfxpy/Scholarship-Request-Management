using System.Net;
using System.Text.Json;

namespace ScholarshipRequest.Client.Features.PublicScholarshipRequests;

public static class PublicApiProblemParser
{
    public static PublicApiError Parse(HttpStatusCode statusCode, string? content)
    {
        var fallbackTitle = statusCode switch
        {
            HttpStatusCode.BadRequest => "ข้อมูลที่ส่งไม่ถูกต้อง",
            HttpStatusCode.Conflict => "ข้อมูลของระบบมีการเปลี่ยนแปลง",
            HttpStatusCode.ServiceUnavailable => "ระบบยังไม่พร้อมให้บริการ",
            _ => "ไม่สามารถดำเนินการได้"
        };

        if (string.IsNullOrWhiteSpace(content))
        {
            return new PublicApiError(
                (int)statusCode,
                null,
                fallbackTitle,
                null,
                EmptyFieldErrors());
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return new PublicApiError(
                    (int)statusCode,
                    null,
                    fallbackTitle,
                    null,
                    EmptyFieldErrors());
            }

            var title = ReadString(root, "title") ?? fallbackTitle;
            var detail = ReadString(root, "detail");
            var code = ReadString(root, "code");
            var fieldErrors = new Dictionary<string, string[]>(StringComparer.Ordinal);

            if (root.TryGetProperty("errors", out var errorsElement) &&
                errorsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in errorsElement.EnumerateObject())
                {
                    var messages = property.Value.ValueKind switch
                    {
                        JsonValueKind.Array => property.Value
                            .EnumerateArray()
                            .Where(item => item.ValueKind == JsonValueKind.String)
                            .Select(item => item.GetString())
                            .Where(message => !string.IsNullOrWhiteSpace(message))
                            .Select(message => message!)
                            .ToArray(),
                        JsonValueKind.String when !string.IsNullOrWhiteSpace(property.Value.GetString()) =>
                            [property.Value.GetString()!],
                        _ => []
                    };

                    if (messages.Length > 0)
                    {
                        fieldErrors[property.Name] = messages;
                    }
                }
            }

            return new PublicApiError(
                (int)statusCode,
                code,
                title,
                detail,
                fieldErrors);
        }
        catch (JsonException)
        {
            return new PublicApiError(
                (int)statusCode,
                null,
                fallbackTitle,
                null,
                EmptyFieldErrors());
        }
    }

    private static string? ReadString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static IReadOnlyDictionary<string, string[]> EmptyFieldErrors() =>
        new Dictionary<string, string[]>(StringComparer.Ordinal);
}
