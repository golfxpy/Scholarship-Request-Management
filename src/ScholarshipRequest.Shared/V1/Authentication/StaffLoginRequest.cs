using System.ComponentModel.DataAnnotations;

namespace ScholarshipRequest.Shared.V1.Authentication;

public sealed class StaffLoginRequest
{
    [Required(ErrorMessage = "กรุณาระบุชื่อผู้ใช้")]
    [StringLength(100, ErrorMessage = "ชื่อผู้ใช้ต้องไม่เกิน 100 ตัวอักษร")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุรหัสผ่าน")]
    [StringLength(200, ErrorMessage = "รหัสผ่านต้องไม่เกิน 200 ตัวอักษร")]
    public string Password { get; set; } = string.Empty;
}
