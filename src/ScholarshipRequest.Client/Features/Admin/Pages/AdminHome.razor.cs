using System.Globalization;
using Microsoft.AspNetCore.Components;
using ScholarshipRequest.Client.Features.Admin.ScholarshipRequests;
using ScholarshipRequest.Client.Features.Authentication;
using ScholarshipRequest.Shared.V1.Admin.Dashboard;

namespace ScholarshipRequest.Client.Features.Admin.Pages;

public partial class AdminHome
{
    private static readonly CultureInfo ThaiCulture = CultureInfo.GetCultureInfo("th-TH");

    private AdminDashboardSummaryResponse? _summary;
    private AdminApiError? _error;
    private bool _loading;

    [Inject]
    private IAdminScholarshipRequestApi Api { get; set; } = default!;

    [Inject]
    private StaffAuthenticationStateProvider AuthenticationProvider { get; set; } = default!;

    private IReadOnlyList<StatusChartItem> StatusChartItems => _summary is null
        ? []
        :
        [
            new("รอพิจารณา", _summary.PendingRequests, "bar-pending"),
            new("อนุมัติ", _summary.ApprovedRequests, "bar-approved"),
            new("ปฏิเสธ", _summary.RejectedRequests, "bar-rejected")
        ];

    private int MaximumStatusCount => StatusChartItems.Count == 0
        ? 0
        : StatusChartItems.Max(item => item.Value);

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            var result = await Api.GetDashboardAsync();
            if (result.IsSuccess && result.Value is not null)
            {
                _summary = result.Value;
                return;
            }

            _error = result.Error ?? AdminApiError.InvalidResponse();
            if (_error.StatusCode == 401)
            {
                AuthenticationProvider.InvalidateSession();
            }
        }
        finally
        {
            _loading = false;
        }
    }

    private decimal BarWidth(int value) => MaximumStatusCount == 0
        ? 0
        : Math.Max(4, value * 100m / MaximumStatusCount);

    private static string TypeFilterUrl(Guid typeId) =>
        $"/admin/requests?scholarshipTypeId={typeId}";

    private static string FormatMoney(decimal amount) =>
        $"{amount.ToString("N2", ThaiCulture)} บาท";

    private sealed record StatusChartItem(string Label, int Value, string CssClass);
}
