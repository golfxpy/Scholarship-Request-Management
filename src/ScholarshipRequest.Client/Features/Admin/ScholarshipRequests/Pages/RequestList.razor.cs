using System.Globalization;
using Microsoft.AspNetCore.Components;
using ScholarshipRequest.Client.Features.Authentication;
using ScholarshipRequest.Shared.V1.Admin.ScholarshipRequests;

namespace ScholarshipRequest.Client.Features.Admin.ScholarshipRequests.Pages;

public partial class RequestList
{
    private static readonly CultureInfo ThaiCulture = CultureInfo.GetCultureInfo("th-TH");

    private CancellationTokenSource? _loadCancellation;
    private IReadOnlyList<AdminScholarshipTypeOptionResponse> _scholarshipTypes = [];
    private AdminScholarshipRequestListResponse? _result;
    private AdminApiError? _error;
    private AdminScholarshipRequestQuery _currentQuery = new();
    private string? _searchInput;
    private string? _statusInput;
    private string? _typeInput;
    private long _loadGeneration;
    private bool _typesLoaded;
    private bool _loading;

    [Inject]
    private IAdminScholarshipRequestApi Api { get; set; } = default!;

    [Inject]
    private StaffAuthenticationStateProvider AuthenticationProvider { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "page")]
    public string? PageQuery { get; set; }

    [SupplyParameterFromQuery(Name = "search")]
    public string? SearchQuery { get; set; }

    [SupplyParameterFromQuery(Name = "status")]
    public string? StatusQuery { get; set; }

    [SupplyParameterFromQuery(Name = "scholarshipTypeId")]
    public string? ScholarshipTypeQuery { get; set; }

    private bool HasFilters =>
        !string.IsNullOrWhiteSpace(_currentQuery.Search) ||
        !string.IsNullOrWhiteSpace(_currentQuery.Status) ||
        !string.IsNullOrWhiteSpace(_currentQuery.ScholarshipTypeId);

    protected override async Task OnParametersSetAsync()
    {
        _currentQuery = AdminScholarshipRequestQuery.FromQueryStrings(
            PageQuery,
            SearchQuery,
            StatusQuery,
            ScholarshipTypeQuery);
        _searchInput = _currentQuery.Search;
        _statusInput = _currentQuery.Status;
        _typeInput = _currentQuery.ScholarshipTypeId;
        await LoadAsync(_currentQuery);
    }

    public ValueTask DisposeAsync()
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task LoadAsync(AdminScholarshipRequestQuery query, bool reloadTypes = false)
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();
        var cancellationToken = _loadCancellation.Token;
        var generation = ++_loadGeneration;
        _loading = true;
        _error = null;

        try
        {
            var listTask = Api.GetListAsync(query, cancellationToken);
            Task<AdminApiResult<IReadOnlyList<AdminScholarshipTypeOptionResponse>>>? typesTask = null;
            if (!_typesLoaded || reloadTypes)
            {
                typesTask = Api.GetScholarshipTypesAsync(cancellationToken);
            }

            var listResult = await listTask;
            var typesResult = typesTask is null ? null : await typesTask;
            if (generation != _loadGeneration)
            {
                return;
            }

            if (!listResult.IsSuccess || listResult.Value is null)
            {
                HandleError(listResult.Error ?? AdminApiError.InvalidResponse());
                return;
            }

            if (typesResult is { IsSuccess: false } ||
                typesResult is { IsSuccess: true, Value: null })
            {
                HandleError(typesResult.Error ?? AdminApiError.InvalidResponse());
                return;
            }

            if (typesResult?.Value is not null)
            {
                _scholarshipTypes = typesResult.Value;
                _typesLoaded = true;
            }

            _result = listResult.Value;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            if (generation == _loadGeneration)
            {
                _loading = false;
            }
        }
    }

    private Task ApplyFiltersAsync()
    {
        var query = new AdminScholarshipRequestQuery(
            1,
            _searchInput,
            _statusInput,
            _typeInput);
        Navigate(query);
        return Task.CompletedTask;
    }

    private Task ClearFiltersAsync()
    {
        _searchInput = null;
        _statusInput = null;
        _typeInput = null;
        Navigate(new AdminScholarshipRequestQuery());
        return Task.CompletedTask;
    }

    private Task GoToPageAsync(int page)
    {
        Navigate(_currentQuery with { Page = page });
        return Task.CompletedTask;
    }

    private Task RetryAsync() => LoadAsync(_currentQuery, reloadTypes: true);

    private void Navigate(AdminScholarshipRequestQuery query) =>
        Navigation.NavigateTo(query.ToPageUri());

    private void HandleError(AdminApiError error)
    {
        _error = error;
        if (error.StatusCode == 401)
        {
            AuthenticationProvider.InvalidateSession();
        }
    }

    private string DetailUrl(Guid id)
    {
        var returnUrl = "/" + Navigation.ToBaseRelativePath(Navigation.Uri);
        return $"/admin/requests/{id}?returnUrl={Uri.EscapeDataString(returnUrl)}";
    }

    private static string StatusLabel(string status) => status switch
    {
        "Pending" => "รอพิจารณา",
        "Approved" => "อนุมัติ",
        "Rejected" => "ปฏิเสธ",
        _ => status
    };

    private static string StatusClass(string status) => status switch
    {
        "Approved" => "status-approved",
        "Rejected" => "status-rejected",
        _ => "status-pending"
    };

    private static string FormatMoney(decimal amount) =>
        $"{amount.ToString("N2", ThaiCulture)} บาท";

    private static string FormatDate(DateTimeOffset value) =>
        value.ToOffset(TimeSpan.FromHours(7)).ToString("d MMM yyyy HH:mm", ThaiCulture);
}
