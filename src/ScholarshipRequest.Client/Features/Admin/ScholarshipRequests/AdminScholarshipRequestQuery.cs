namespace ScholarshipRequest.Client.Features.Admin.ScholarshipRequests;

public sealed record AdminScholarshipRequestQuery(
    int Page = 1,
    string? Search = null,
    string? Status = null,
    string? ScholarshipTypeId = null)
{
    public const int PageSize = 10;

    public static AdminScholarshipRequestQuery FromQueryStrings(
        string? page,
        string? search,
        string? status,
        string? scholarshipTypeId) =>
        new(
            int.TryParse(page, out var parsedPage) && parsedPage > 0 ? parsedPage : 1,
            NullIfWhiteSpace(search),
            NullIfWhiteSpace(status),
            NullIfWhiteSpace(scholarshipTypeId));

    public string ToApiUri() => BuildUri("/api/v1/admin/scholarship-requests");

    public string ToPageUri() => BuildUri("/admin/requests");

    private string BuildUri(string path)
    {
        var values = new List<KeyValuePair<string, string>>();
        if (Page > 1)
        {
            values.Add(new("page", Page.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        AddIfPresent(values, "search", Search);
        AddIfPresent(values, "status", Status);
        AddIfPresent(values, "scholarshipTypeId", ScholarshipTypeId);
        if (values.Count == 0)
        {
            return path;
        }

        return path + "?" + string.Join(
            "&",
            values.Select(value =>
                $"{Uri.EscapeDataString(value.Key)}={Uri.EscapeDataString(value.Value)}"));
    }

    private static void AddIfPresent(
        ICollection<KeyValuePair<string, string>> values,
        string key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values.Add(new(key, value.Trim()));
        }
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
