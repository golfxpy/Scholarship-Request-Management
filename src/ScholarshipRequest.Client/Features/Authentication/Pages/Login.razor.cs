using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ScholarshipRequest.Shared.V1.Authentication;

namespace ScholarshipRequest.Client.Features.Authentication.Pages;

public partial class Login
{
    private readonly StaffLoginRequest _model = new();
    private ElementReference _errorSummary;
    private ElementReference _validationSummary;
    private bool _focusError;
    private bool _focusValidationSummary;
    private bool _showPassword;
    private bool _submitting;
    private string? _errorMessage;

    [Inject]
    private StaffAuthenticationStateProvider AuthenticationProvider { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthenticationProvider.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated == true)
        {
            Navigation.NavigateTo(ReturnUrlValidator.Sanitize(ReturnUrl), replace: true);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusError)
        {
            _focusError = false;
            await _errorSummary.FocusAsync();
        }

        if (_focusValidationSummary)
        {
            _focusValidationSummary = false;
            await _validationSummary.FocusAsync();
        }
    }

    private void TogglePassword() => _showPassword = !_showPassword;

    private void HandleInvalidSubmit() => _focusValidationSummary = true;

    private async Task LoginAsync()
    {
        if (_submitting)
        {
            return;
        }

        _submitting = true;
        _errorMessage = null;

        try
        {
            var result = await AuthenticationProvider.LoginAsync(_model);
            _model.Password = string.Empty;
            _showPassword = false;

            if (result.IsSuccess)
            {
                Navigation.NavigateTo(ReturnUrlValidator.Sanitize(ReturnUrl), replace: true);
                return;
            }

            _errorMessage = result.Error?.Code switch
            {
                "AUTH_INVALID_CREDENTIALS" => "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง",
                "ANTIFORGERY_VALIDATION_FAILED" => "เซสชันของแบบฟอร์มหมดอายุ กรุณาลองอีกครั้ง",
                _ => result.Error?.Message ?? "ไม่สามารถเข้าสู่ระบบได้ กรุณาลองอีกครั้ง"
            };
            _focusError = true;
        }
        finally
        {
            _submitting = false;
        }
    }
}
