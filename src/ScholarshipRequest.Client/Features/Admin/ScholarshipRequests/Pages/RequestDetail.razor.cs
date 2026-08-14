using System.Globalization;
using Microsoft.AspNetCore.Components;
using ScholarshipRequest.Client.Features.Authentication;
using ScholarshipRequest.Shared.V1.Admin.ScholarshipRequests;

namespace ScholarshipRequest.Client.Features.Admin.ScholarshipRequests.Pages;

public partial class RequestDetail
{
    private static readonly CultureInfo ThaiCulture = CultureInfo.GetCultureInfo("th-TH");

    private CancellationTokenSource? _loadCancellation;
    private AdminScholarshipRequestDetailResponse? _detail;
    private AdminApiError? _error;
    private string? _activeAction;
    private string? _decisionNote;
    private string? _actionError;
    private bool _actionSubmitting;
    private bool _loading;

    [Parameter]
    public Guid Id { get; set; }

    [SupplyParameterFromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    [Inject]
    private IAdminScholarshipRequestApi Api { get; set; } = default!;

    [Inject]
    private StaffAuthenticationStateProvider AuthenticationProvider { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private string BackUrl =>
        !string.IsNullOrWhiteSpace(ReturnUrl) &&
        ReturnUrl.StartsWith("/admin/requests", StringComparison.Ordinal) &&
        !ReturnUrl.StartsWith("//", StringComparison.Ordinal) &&
        !ReturnUrl.Contains('\\')
            ? ReturnUrl
            : "/admin/requests";

    private string EditUrl => _detail is null
        ? "/admin/requests"
        : $"/admin/requests/{_detail.Id}/edit?returnUrl={Uri.EscapeDataString(BackUrl)}";

    private string ActionTitle => _activeAction switch
    {
        "Approved" => "ยืนยันการอนุมัติคำขอ",
        "Rejected" => "ยืนยันการปฏิเสธคำขอ",
        "Delete" => "ยืนยันการลบคำขอ",
        _ => string.Empty
    };

    private string ActionDescription => _activeAction switch
    {
        "Approved" => "เมื่ออนุมัติแล้ว คำขอจะเป็นข้อมูลอ่านอย่างเดียวและเปิดกลับไม่ได้",
        "Rejected" => "กรุณาระบุเหตุผล คำขอที่ปฏิเสธแล้วจะเปิดกลับไม่ได้",
        "Delete" => $"ระบบจะซ่อนคำขอ {_detail?.RequestNumber} ออกจากรายการปกติ",
        _ => string.Empty
    };

    private string ConfirmActionLabel => _activeAction switch
    {
        "Approved" => "ยืนยันอนุมัติ",
        "Rejected" => "ยืนยันปฏิเสธ",
        "Delete" => "ยืนยันลบ",
        _ => "ยืนยัน"
    };

    protected override Task OnParametersSetAsync() => LoadAsync();

    public ValueTask DisposeAsync()
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task LoadAsync()
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();
        var cancellationToken = _loadCancellation.Token;
        _loading = true;
        _error = null;

        try
        {
            var result = await Api.GetDetailAsync(Id, cancellationToken);
            if (result.IsSuccess && result.Value is not null)
            {
                _detail = result.Value;
                _activeAction = null;
                _actionError = null;
                return;
            }

            _detail = null;
            _error = result.Error ?? AdminApiError.InvalidResponse();
            if (_error.StatusCode == 401)
            {
                AuthenticationProvider.InvalidateSession();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _loading = false;
            }
        }
    }

    private void OpenAction(string action)
    {
        _activeAction = action;
        _decisionNote = null;
        _actionError = null;
    }

    private void CloseAction()
    {
        if (_actionSubmitting)
        {
            return;
        }

        _activeAction = null;
        _decisionNote = null;
        _actionError = null;
    }

    private async Task SubmitActionAsync()
    {
        if (_actionSubmitting || _detail is null || _activeAction is null)
        {
            return;
        }

        if (_activeAction == "Rejected" && string.IsNullOrWhiteSpace(_decisionNote))
        {
            _actionError = "การปฏิเสธคำขอต้องระบุหมายเหตุ";
            return;
        }

        _actionSubmitting = true;
        _actionError = null;
        try
        {
            AdminApiResult<bool> result;
            if (_activeAction == "Delete")
            {
                result = await Api.DeleteAsync(_detail.Id, _detail.UpdatedAt);
                if (result.IsSuccess)
                {
                    Navigation.NavigateTo(BackUrl, replace: true);
                    return;
                }
            }
            else
            {
                result = await Api.DecideAsync(
                    _detail.Id,
                    new AdminScholarshipRequestDecisionRequest
                    {
                        ExpectedUpdatedAt = _detail.UpdatedAt,
                        Decision = _activeAction,
                        Note = _decisionNote
                    });
                if (result.IsSuccess)
                {
                    await LoadAsync();
                    return;
                }
            }

            var error = result.Error ?? AdminApiError.InvalidResponse();
            _actionError = error.Detail ?? error.Title;
            if (error.StatusCode == 401)
            {
                AuthenticationProvider.InvalidateSession();
            }
            else if (error.Code == "SCHOLARSHIP_REQUEST_NOT_PENDING")
            {
                await LoadAsync();
            }
            else if (error.Code == "SCHOLARSHIP_REQUEST_VERSION_CONFLICT")
            {
                await LoadAsync();
            }
        }
        finally
        {
            _actionSubmitting = false;
        }
    }

    private static string DisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value;

    private static string YearLabel(AdminScholarshipRequestDetailResponse detail) =>
        detail.YearLevel is not null
            ? $"ชั้นปี {detail.YearLevel}"
            : DisplayValue(detail.YearLevelOther);

    private static string SourceLabel(string source) => source switch
    {
        "Public" => "นักศึกษายื่นด้วยตนเอง",
        "Staff" => "เจ้าหน้าที่บันทึกแทน",
        _ => source
    };

    private static string ConsentLabel(string method) => method switch
    {
        "Self" => "ยืนยันด้วยตนเองในระบบ",
        "Document" => "เอกสาร",
        "Verbal" => "วาจา",
        "Other" => "วิธีอื่น",
        _ => method
    };

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

    private static string FormatOptionalDate(DateTimeOffset? value) =>
        value is null ? "—" : FormatDate(value.Value);
}
