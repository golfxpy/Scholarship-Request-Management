using System.ComponentModel.DataAnnotations;

namespace ScholarshipRequest.Shared.V1.ScholarshipRequests;

public sealed class CreatePublicScholarshipRequest
{
    [Required(ErrorMessage = "กรุณาระบุรหัสนักศึกษา")]
    [StringLength(20, ErrorMessage = "รหัสนักศึกษาต้องไม่เกิน 20 ตัวอักษร")]
    public string StudentId { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุชื่อ–นามสกุล")]
    [StringLength(200, ErrorMessage = "ชื่อ–นามสกุลต้องไม่เกิน 200 ตัวอักษร")]
    public string StudentName { get; set; } = string.Empty;

    public Guid? AcademicUnitId { get; set; }

    [Required(ErrorMessage = "กรุณาระบุคณะหรือหน่วยการเรียน")]
    [StringLength(200, ErrorMessage = "ชื่อคณะหรือหน่วยการเรียนต้องไม่เกิน 200 ตัวอักษร")]
    public string FacultyName { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "หลักสูตรหรือสาขาวิชาต้องไม่เกิน 150 ตัวอักษร")]
    public string? Major { get; set; }

    public int? YearLevel { get; set; }

    [StringLength(100, ErrorMessage = "ชั้นปีอื่นต้องไม่เกิน 100 ตัวอักษร")]
    public string? YearLevelOther { get; set; }

    [Range(typeof(decimal), "0.00", "4.00", ErrorMessage = "GPAX ต้องอยู่ระหว่าง 0.00–4.00")]
    public decimal Gpax { get; set; }

    [Required(ErrorMessage = "กรุณาระบุอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [StringLength(254, ErrorMessage = "อีเมลต้องไม่เกิน 254 ตัวอักษร")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาเลือกประเภททุน")]
    public Guid ScholarshipTypeId { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "9999999999.99",
        ErrorMessage = "จำนวนเงินต้องอยู่ระหว่าง 0.01–9,999,999,999.99 บาท")]
    public decimal RequestedAmount { get; set; }

    [Required(ErrorMessage = "กรุณาระบุเลขบัญชีธนาคาร")]
    [StringLength(40, ErrorMessage = "เลขบัญชีธนาคารต้องไม่เกิน 40 ตัวอักษร")]
    public string BankAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุเหตุผลในการขอรับทุน")]
    [StringLength(2000, ErrorMessage = "เหตุผลในการขอรับทุนต้องไม่เกิน 2,000 ตัวอักษร")]
    public string Reason { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "กรุณายอมรับเงื่อนไขการประมวลผลข้อมูลส่วนบุคคล")]
    public bool PdpaConsent { get; set; }

    [Required(ErrorMessage = "ไม่พบรุ่นประกาศความเป็นส่วนตัว")]
    [StringLength(30, ErrorMessage = "รุ่นประกาศความเป็นส่วนตัวต้องไม่เกิน 30 ตัวอักษร")]
    public string PdpaNoticeVersion { get; set; } = string.Empty;
}
