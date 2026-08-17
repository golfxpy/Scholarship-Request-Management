using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ScholarshipRequest.Client.Features.Authentication;
using ScholarshipRequest.Client.Features.PublicScholarshipRequests;
using ScholarshipRequest.Shared.V1.Admin.ScholarshipRequests;
using ScholarshipRequest.Shared.V1.Privacy;

namespace ScholarshipRequest.Client.Features.Admin.ScholarshipRequests.Pages;

public partial class RequestForm
{
    private AdminRequestFormModel _model = new();
    private CancellationTokenSource? _loadCancellation;
    private CancellationTokenSource? _submitCancellation;
    private IReadOnlyList<AdminScholarshipTypeOptionResponse> _scholarshipTypes = [];
    private PdpaNoticeResponse? _pdpaNotice;
    private AdminApiError? _loadError;
    private AdminApiError? _serverError;
    private ElementReference _errorSummary;
    private ElementReference _validationSummary;
    private bool _focusServerError;
    private bool _focusValidation;
    private bool _loading;
    private bool _submitting;
    private long _loadGeneration;

    [Parameter]
    public Guid? Id { get; set; }

    [SupplyParameterFromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    [Inject]
    private IAdminScholarshipRequestApi Api { get; set; } = default!;

    [Inject]
    private IPublicScholarshipApi PublicApi { get; set; } = default!;

    [Inject]
    private StaffAuthenticationStateProvider AuthenticationProvider { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private bool IsEdit => Id is not null;

    private string SafeReturnUrl =>
        !string.IsNullOrWhiteSpace(ReturnUrl) &&
        ReturnUrl.StartsWith("/admin/requests", StringComparison.Ordinal) &&
        !ReturnUrl.StartsWith("//", StringComparison.Ordinal) &&
        !ReturnUrl.Contains('\\')
            ? ReturnUrl
            : "/admin/requests";

    private string BackUrl => IsEdit
        ? $"/admin/requests/{Id}?returnUrl={Uri.EscapeDataString(SafeReturnUrl)}"
        : "/admin/requests";

    protected override Task OnParametersSetAsync() => LoadAsync();

    public ValueTask DisposeAsync()
    {
        ++_loadGeneration;
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _submitCancellation?.Cancel();
        _submitCancellation?.Dispose();
        return ValueTask.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusServerError)
        {
            _focusServerError = false;
            await _errorSummary.FocusAsync();
        }

        if (_focusValidation)
        {
            _focusValidation = false;
            await _validationSummary.FocusAsync();
        }
    }

    private async Task LoadAsync()
    {
        _submitCancellation?.Cancel();
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();
        var cancellationToken = _loadCancellation.Token;
        var generation = ++_loadGeneration;
        var requestedId = Id;
        _model = new AdminRequestFormModel { IsEdit = requestedId.HasValue };
        _loading = true;
        _loadError = null;
        _serverError = null;

        try
        {
            var typesResult = await Api.GetScholarshipTypesAsync(cancellationToken);
            if (generation != _loadGeneration)
            {
                return;
            }
            if (!typesResult.IsSuccess || typesResult.Value is null)
            {
                HandleLoadError(typesResult.Error ?? AdminApiError.InvalidResponse());
                return;
            }

            _scholarshipTypes = typesResult.Value;
            if (requestedId.HasValue)
            {
                var detailResult = await Api.GetDetailAsync(
                    requestedId.Value,
                    cancellationToken);
                if (generation != _loadGeneration)
                {
                    return;
                }
                if (!detailResult.IsSuccess || detailResult.Value is null)
                {
                    HandleLoadError(detailResult.Error ?? AdminApiError.InvalidResponse());
                    return;
                }

                if (!detailResult.Value.CanEdit)
                {
                    Navigation.NavigateTo(BackUrl, replace: true);
                    return;
                }

                _model.Populate(detailResult.Value);
            }
            else
            {
                _model.IsEdit = false;
                var noticeResult = await PublicApi.GetPdpaNoticeAsync(cancellationToken);
                if (generation != _loadGeneration)
                {
                    return;
                }
                if (!noticeResult.IsSuccess || noticeResult.Value is null)
                {
                    _loadError = new AdminApiError(
                        noticeResult.Error?.StatusCode,
                        noticeResult.Error?.Code,
                        noticeResult.Error?.Title ?? "ไม่พบประกาศ PDPA",
                        noticeResult.Error?.Detail,
                        noticeResult.Error?.FieldErrors ??
                            new Dictionary<string, string[]>(StringComparer.Ordinal));
                    return;
                }

                _pdpaNotice = noticeResult.Value;
            }
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

    private void HandleInvalidSubmit() => _focusValidation = true;

    private async Task SubmitAsync()
    {
        if (_submitting)
        {
            return;
        }

        _submitting = true;
        _serverError = null;
        _submitCancellation?.Cancel();
        _submitCancellation?.Dispose();
        _submitCancellation = new CancellationTokenSource();
        var cancellationToken = _submitCancellation.Token;
        var submissionUri = Navigation.Uri;
        try
        {
            if (IsEdit)
            {
                var result = await Api.UpdateAsync(
                    Id!.Value,
                    _model.ToUpdate(),
                    cancellationToken);
                if (result.IsSuccess)
                {
                    if (!cancellationToken.IsCancellationRequested &&
                        string.Equals(Navigation.Uri, submissionUri, StringComparison.Ordinal))
                    {
                        Navigation.NavigateTo(BackUrl, replace: true);
                    }
                    return;
                }

                HandleSubmitError(result.Error ?? AdminApiError.InvalidResponse());
                return;
            }

            if (_pdpaNotice is null)
            {
                _loadError = AdminApiError.InvalidResponse();
                return;
            }

            var createResult = await Api.CreateAsync(
                _model.ToCreate(_pdpaNotice.Version),
                cancellationToken);
            if (createResult.IsSuccess && createResult.Value is not null)
            {
                ClearBankFields();
                if (!cancellationToken.IsCancellationRequested &&
                    string.Equals(Navigation.Uri, submissionUri, StringComparison.Ordinal))
                {
                    Navigation.NavigateTo(
                        $"/admin/requests/{createResult.Value.Id}",
                        replace: true);
                }
                return;
            }

            HandleSubmitError(createResult.Error ?? AdminApiError.InvalidResponse());
            if (createResult.Error?.Code == "CONSENT_VERSION_CHANGED")
            {
                _model.ConsentMethod = string.Empty;
                _model.ConsentEvidenceNote = string.Empty;
                var noticeResult = await PublicApi.GetPdpaNoticeAsync(cancellationToken);
                if (noticeResult.IsSuccess)
                {
                    _pdpaNotice = noticeResult.Value;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _submitting = false;
            }
        }
    }

    private void HandleLoadError(AdminApiError error)
    {
        _loadError = error;
        if (error.StatusCode == 401)
        {
            AuthenticationProvider.InvalidateSession();
        }
    }

    private void HandleSubmitError(AdminApiError error)
    {
        _serverError = error;
        _focusServerError = true;
        if (error.StatusCode == 401)
        {
            AuthenticationProvider.InvalidateSession();
        }
        else if (IsEdit &&
            error.Code is "SCHOLARSHIP_REQUEST_NOT_PENDING" or
                "SCHOLARSHIP_REQUEST_VERSION_CONFLICT")
        {
            Navigation.NavigateTo(BackUrl, replace: true);
        }
    }

    private void ClearBankFields()
    {
        _model.BankAccountNumber = null;
        _model.BankAccountConfirmation = null;
    }
}
