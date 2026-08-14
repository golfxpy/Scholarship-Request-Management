using Microsoft.AspNetCore.Components;
using ScholarshipRequest.Client.Features.Authentication;

namespace ScholarshipRequest.Client.Layout;

public partial class AdminLayout
{
    private bool _loggingOut;
    private string? _logoutError;

    [Inject]
    private StaffAuthenticationStateProvider AuthenticationProvider { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private string DisplayName =>
        AuthenticationProvider.CurrentSession?.FullName ??
        AuthenticationProvider.CurrentSession?.UserName ??
        "เจ้าหน้าที่";

    private async Task LogoutAsync()
    {
        if (_loggingOut)
        {
            return;
        }

        _loggingOut = true;
        _logoutError = null;

        try
        {
            var result = await AuthenticationProvider.LogoutAsync();
            if (result.IsSuccess)
            {
                Navigation.NavigateTo("/admin/login", replace: true);
                return;
            }

            _logoutError = result.Error?.Message ??
                "ยังไม่สามารถยืนยันการออกจากระบบได้ กรุณาลองอีกครั้ง";
        }
        finally
        {
            _loggingOut = false;
        }
    }
}
