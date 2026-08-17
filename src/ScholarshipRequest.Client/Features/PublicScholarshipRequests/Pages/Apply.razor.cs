using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ScholarshipRequest.Shared.V1.Masters;
using ScholarshipRequest.Shared.V1.Privacy;
using ScholarshipRequest.Shared.V1.ScholarshipRequests;

namespace ScholarshipRequest.Client.Features.PublicScholarshipRequests.Pages;

public partial class Apply : IDisposable
{
    private readonly CancellationTokenSource _lifetimeCts = new();
    private readonly CreatePublicScholarshipRequest _model = new();
    private EditContext _editContext = default!;
    private ValidationMessageStore _validationMessages = default!;
    private IReadOnlyList<ScholarshipTypeResponse> _scholarshipTypes = [];
    private PdpaNoticeResponse? _pdpaNotice;
    private CreateScholarshipRequestResponse? _createdRequest;
    private AcademicUnitResponse? _selectedAcademicUnit;
    private string _confirmBankAccount = string.Empty;
    private IReadOnlyList<string> _confirmBankAccountErrors = [];
    private ElementReference _feedbackElement;
    private string? _globalErrorTitle;
    private string? _globalErrorDetail;
    private string? _academicSearchError;
    private bool _globalMessageIsWarning;
    private bool _isLoading = true;
    private bool _isSubmitting;
    private bool _useOtherFaculty;
    private bool _useOtherYear;
    private bool _showBankAccount;
    private bool _focusFeedbackAfterRender;

    [Inject]
    public required IPublicScholarshipApi PublicApi { get; init; }

    private bool _canUseForm =>
        _pdpaNotice is not null && _scholarshipTypes.Count > 0;

    private string GlobalMessageCssClass =>
        _globalMessageIsWarning ? "global-message warning-message" : "global-message error-message";

    private string BankInputType => _showBankAccount ? "text" : "password";

    private string ConfirmBankCssClass =>
        _confirmBankAccountErrors.Count > 0 ? "form-control invalid" : "form-control";

    private string BankToggleLabel =>
        _showBankAccount ? "ซ่อนเลขบัญชี" : "แสดงเลขบัญชี";

    protected override async Task OnInitializedAsync()
    {
        _editContext = new EditContext(_model);
        _validationMessages = new ValidationMessageStore(_editContext);
        _editContext.OnFieldChanged += HandleFieldChanged;
        await LoadInitialDataAsync();
    }

    private async Task LoadInitialDataAsync()
    {
        _isLoading = true;
        ClearGlobalMessage();
        _scholarshipTypes = [];
        _pdpaNotice = null;
        _model.PdpaNoticeVersion = string.Empty;
        _model.PdpaConsent = false;

        try
        {
            var scholarshipTypesTask =
                PublicApi.GetScholarshipTypesAsync(_lifetimeCts.Token);
            var pdpaNoticeTask =
                PublicApi.GetPdpaNoticeAsync(_lifetimeCts.Token);

            await Task.WhenAll(scholarshipTypesTask, pdpaNoticeTask);

            var scholarshipTypesResult = await scholarshipTypesTask;
            var pdpaNoticeResult = await pdpaNoticeTask;

            if (!scholarshipTypesResult.IsSuccess)
            {
                SetGlobalError(scholarshipTypesResult.Error!);
            }
            else
            {
                var scholarshipTypes = scholarshipTypesResult.Value ?? [];
                if (scholarshipTypes.Count == 0)
                {
                    SetGlobalError(
                        "ยังไม่มีประเภททุนที่เปิดรับ",
                        "กรุณาติดต่อเจ้าหน้าที่หรือกลับมาลองใหม่ภายหลัง");
                }
            }

            if (!pdpaNoticeResult.IsSuccess)
            {
                SetGlobalError(pdpaNoticeResult.Error!);
            }
            else
            {
                var pdpaNotice = pdpaNoticeResult.Value;
                if (pdpaNotice is null)
                {
                    SetGlobalError(
                        "ไม่พบประกาศความเป็นส่วนตัวที่ใช้งานอยู่",
                        "กรุณาติดต่อเจ้าหน้าที่หรือกลับมาลองใหม่ภายหลัง");
                }
                else if (scholarshipTypesResult.IsSuccess &&
                    (scholarshipTypesResult.Value?.Count ?? 0) > 0)
                {
                    _scholarshipTypes = scholarshipTypesResult.Value!;
                    _pdpaNotice = pdpaNotice;
                    _model.PdpaNoticeVersion = pdpaNotice.Version;
                }
            }
        }
        catch (OperationCanceledException) when (_lifetimeCts.IsCancellationRequested)
        {
            return;
        }
        finally
        {
            _isLoading = false;
            if (!_canUseForm && !string.IsNullOrWhiteSpace(_globalErrorTitle))
            {
                _focusFeedbackAfterRender = true;
            }
        }
    }

    private Task RetryInitialLoadAsync() => LoadInitialDataAsync();

    private async Task<IEnumerable<AcademicUnitResponse>> SearchAcademicUnitsAsync(
        string searchText,
        CancellationToken cancellationToken)
    {
        if (_useOtherFaculty)
        {
            return [];
        }

        var result = await PublicApi.SearchAcademicUnitsAsync(searchText, cancellationToken);
        if (result.IsSuccess)
        {
            _academicSearchError = null;
            return result.Value ?? [];
        }

        _academicSearchError = result.Error?.Title ?? "ค้นหาคณะไม่สำเร็จ";
        await InvokeAsync(StateHasChanged);
        return [];
    }

    private Task HandleAcademicUnitChanged(AcademicUnitResponse? academicUnit)
    {
        _selectedAcademicUnit = academicUnit;
        _model.AcademicUnitId = academicUnit?.Id;
        _model.FacultyName = academicUnit?.Name ?? string.Empty;
        NotifyFieldChanged(nameof(_model.FacultyName));
        return Task.CompletedTask;
    }

    private void HandleOtherFacultyChanged(ChangeEventArgs args)
    {
        _useOtherFaculty = args.Value is bool value && value;
        _selectedAcademicUnit = null;
        _model.AcademicUnitId = null;
        _model.FacultyName = string.Empty;
        _academicSearchError = null;
        NotifyFieldChanged(nameof(_model.FacultyName));
    }

    private void SetYearMode(bool useOtherYear)
    {
        _useOtherYear = useOtherYear;
        if (_useOtherYear)
        {
            _model.YearLevel = null;
        }
        else
        {
            _model.YearLevelOther = null;
        }

        NotifyFieldChanged(nameof(_model.YearLevel));
        NotifyFieldChanged(nameof(_model.YearLevelOther));
    }

    private async Task HandleSubmitAsync(EditContext _)
    {
        if (_isSubmitting)
        {
            return;
        }

        ClearValidationMessages();
        ClearGlobalMessage();

        var localErrors = PublicScholarshipRequestRules.Validate(_model);
        AddFieldErrors(localErrors);

        _confirmBankAccountErrors = PublicScholarshipRequestRules
            .ValidateBankAccount(_confirmBankAccount)
            .ToList();
        if (_confirmBankAccountErrors.Count == 0 &&
            !string.Equals(
                PublicScholarshipRequestRules.NormalizeBankAccount(_model.BankAccountNumber),
                PublicScholarshipRequestRules.NormalizeBankAccount(_confirmBankAccount),
                StringComparison.Ordinal))
        {
            _confirmBankAccountErrors = ["เลขบัญชีและการยืนยันเลขบัญชีไม่ตรงกัน"];
            _editContext.NotifyValidationStateChanged();
        }

        if (_editContext.GetValidationMessages().Any() ||
            _confirmBankAccountErrors.Count > 0)
        {
            SetGlobalError(
                "กรุณาตรวจสอบข้อมูลในแบบฟอร์ม",
                "แก้ไขช่องที่มีข้อความแจ้งเตือนแล้วลองส่งอีกครั้ง");
            return;
        }

        _isSubmitting = true;
        try
        {
            var result = await PublicApi.CreateRequestAsync(_model, _lifetimeCts.Token);
            if (result.IsSuccess)
            {
                _createdRequest = result.Value;
                _model.BankAccountNumber = string.Empty;
                _confirmBankAccount = string.Empty;
                _focusFeedbackAfterRender = true;
                return;
            }

            var error = result.Error!;
            AddFieldErrors(error.FieldErrors);
            await RefreshStaleMasterDataAsync(error);

            if (string.Equals(
                    error.Code,
                    "CONSENT_VERSION_CHANGED",
                    StringComparison.Ordinal))
            {
                await RefreshPdpaAfterConflictAsync();
                return;
            }

            SetGlobalError(error);
        }
        catch (OperationCanceledException) when (_lifetimeCts.IsCancellationRequested)
        {
            return;
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task RefreshPdpaAfterConflictAsync()
    {
        _model.PdpaConsent = false;
        NotifyFieldChanged(nameof(_model.PdpaConsent));

        var result = await PublicApi.GetPdpaNoticeAsync(_lifetimeCts.Token);
        if (!result.IsSuccess || result.Value is null)
        {
            _pdpaNotice = null;
            SetGlobalError(
                result.Error?.Title ?? "โหลดประกาศฉบับใหม่ไม่สำเร็จ",
                result.Error?.Detail ?? "กรุณาลองโหลดแบบฟอร์มใหม่");
            return;
        }

        _pdpaNotice = result.Value;
        _model.PdpaNoticeVersion = result.Value.Version;
        _globalMessageIsWarning = true;
        _globalErrorTitle = "ประกาศความเป็นส่วนตัวมีการเปลี่ยนแปลง";
        _globalErrorDetail = "กรุณาอ่านประกาศฉบับปัจจุบันและยอมรับอีกครั้งก่อนส่งคำขอ";
        _focusFeedbackAfterRender = true;
    }

    private async Task RefreshStaleMasterDataAsync(PublicApiError error)
    {
        if (error.FieldErrors.ContainsKey(nameof(_model.ScholarshipTypeId)))
        {
            var result = await PublicApi.GetScholarshipTypesAsync(_lifetimeCts.Token);
            if (result.IsSuccess)
            {
                _scholarshipTypes = result.Value ?? [];
            }

            _model.ScholarshipTypeId = Guid.Empty;
            NotifyFieldChanged(nameof(_model.ScholarshipTypeId));
        }

        if (error.FieldErrors.ContainsKey(nameof(_model.AcademicUnitId)))
        {
            _selectedAcademicUnit = null;
            _model.AcademicUnitId = null;
            if (!_useOtherFaculty)
            {
                _model.FacultyName = string.Empty;
            }

            NotifyFieldChanged(nameof(_model.FacultyName));
        }
    }

    private void AddFieldErrors(IReadOnlyDictionary<string, string[]> errors)
    {
        foreach (var (propertyName, messages) in errors)
        {
            var mappedPropertyName = propertyName switch
            {
                nameof(_model.AcademicUnitId) => nameof(_model.FacultyName),
                _ => propertyName
            };
            var field = new FieldIdentifier(_model, mappedPropertyName);
            var existingMessages = _editContext
                .GetValidationMessages(field)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var message in messages.Where(message => !existingMessages.Contains(message)))
            {
                _validationMessages.Add(field, message);
            }
        }

        _editContext.NotifyValidationStateChanged();
    }

    private void ClearValidationMessages()
    {
        _validationMessages.Clear();
        _confirmBankAccountErrors = [];
        _editContext.NotifyValidationStateChanged();
    }

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs args)
    {
        _validationMessages.Clear(args.FieldIdentifier);
        if (args.FieldIdentifier.FieldName == nameof(_model.BankAccountNumber))
        {
            _confirmBankAccountErrors = [];
        }

        _editContext.NotifyValidationStateChanged();
    }

    private void ClearConfirmBankErrors() =>
        _confirmBankAccountErrors = [];

    private void HandleTextInput(
        ChangeEventArgs args,
        string propertyName,
        Action<string> assignValue)
    {
        assignValue(args.Value?.ToString() ?? string.Empty);
        NotifyFieldChanged(propertyName);
    }

    private void HandleDecimalInput(
        ChangeEventArgs args,
        string propertyName,
        Action<decimal> assignValue)
    {
        var text = args.Value?.ToString();
        assignValue(decimal.TryParse(
            text,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var value)
                ? value
                : 0m);
        NotifyFieldChanged(propertyName);
    }

    private void NotifyFieldChanged(string propertyName) =>
        _editContext.NotifyFieldChanged(new FieldIdentifier(_model, propertyName));

    private void SetGlobalError(PublicApiError error) =>
        SetGlobalError(error.Title, error.Detail);

    private void SetGlobalError(string title, string? detail)
    {
        _globalMessageIsWarning = false;
        _globalErrorTitle = title;
        _globalErrorDetail = detail;
        _focusFeedbackAfterRender = !_isLoading;
    }

    private void ClearGlobalMessage()
    {
        _globalMessageIsWarning = false;
        _globalErrorTitle = null;
        _globalErrorDetail = null;
    }

    private void ToggleBankVisibility() =>
        _showBankAccount = !_showBankAccount;

    private static string FormatAcademicUnit(AcademicUnitResponse? academicUnit) =>
        academicUnit is null ? string.Empty : $"{academicUnit.Name} ({academicUnit.Code})";

    private static string FormatSubmittedAt(DateTimeOffset submittedAt) =>
        submittedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm น.");

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusFeedbackAfterRender)
        {
            _focusFeedbackAfterRender = false;
            await _feedbackElement.FocusAsync();
        }
    }

    public void Dispose()
    {
        _editContext.OnFieldChanged -= HandleFieldChanged;
        _lifetimeCts.Cancel();
        _lifetimeCts.Dispose();
    }
}
