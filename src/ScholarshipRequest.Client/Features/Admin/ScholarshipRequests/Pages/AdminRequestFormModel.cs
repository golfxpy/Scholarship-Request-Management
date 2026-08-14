using System.ComponentModel.DataAnnotations;
using ScholarshipRequest.Shared.V1.Admin.ScholarshipRequests;
using ScholarshipRequest.Shared.V1.ScholarshipRequests;

namespace ScholarshipRequest.Client.Features.Admin.ScholarshipRequests.Pages;

public sealed class AdminRequestFormModel : IValidatableObject
{
    [Required(ErrorMessage = "กรุณาระบุรหัสนักศึกษา")]
    [StringLength(20, ErrorMessage = "รหัสนักศึกษาต้องยาวไม่เกิน 20 ตัวอักษร")]
    public string StudentId { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุชื่อ-นามสกุล")]
    [StringLength(200, ErrorMessage = "ชื่อต้องยาวไม่เกิน 200 ตัวอักษร")]
    public string StudentName { get; set; } = string.Empty;

    public Guid? AcademicUnitId { get; set; }

    [Required(ErrorMessage = "กรุณาระบุคณะ/หน่วยการเรียน")]
    [StringLength(200, ErrorMessage = "คณะ/หน่วยการเรียนต้องยาวไม่เกิน 200 ตัวอักษร")]
    public string FacultyName { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "หลักสูตร/สาขาวิชาต้องยาวไม่เกิน 150 ตัวอักษร")]
    public string? Major { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกชั้นปี")]
    public string YearSelection { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "รายละเอียดชั้นปีต้องยาวไม่เกิน 100 ตัวอักษร")]
    public string? YearLevelOther { get; set; }

    [Range(0, 4, ErrorMessage = "GPAX ต้องอยู่ระหว่าง 0.00 ถึง 4.00")]
    public decimal Gpax { get; set; }

    [Required(ErrorMessage = "กรุณาระบุอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [StringLength(254, ErrorMessage = "อีเมลต้องยาวไม่เกิน 254 ตัวอักษร")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาเลือกประเภททุน")]
    public string ScholarshipTypeId { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999.99", ErrorMessage = "จำนวนเงินต้องมากกว่า 0")]
    public decimal RequestedAmount { get; set; }

    [StringLength(40, ErrorMessage = "เลขบัญชีก่อนปรับรูปแบบต้องยาวไม่เกิน 40 ตัวอักษร")]
    public string? BankAccountNumber { get; set; }

    public string? BankAccountConfirmation { get; set; }

    [Required(ErrorMessage = "กรุณาระบุเหตุผลในการขอทุน")]
    [StringLength(2000, ErrorMessage = "เหตุผลต้องยาวไม่เกิน 2,000 ตัวอักษร")]
    public string Reason { get; set; } = string.Empty;

    public string ConsentMethod { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "หลักฐาน consent ต้องยาวไม่เกิน 500 ตัวอักษร")]
    public string ConsentEvidenceNote { get; set; } = string.Empty;

    public bool IsEdit { get; set; }

    public DateTimeOffset? ExpectedUpdatedAt { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (YearSelection == "Other" && string.IsNullOrWhiteSpace(YearLevelOther))
        {
            yield return new ValidationResult(
                "กรุณาระบุชั้นปีหรือกรณีเกินระยะเวลาหลักสูตร",
                [nameof(YearLevelOther)]);
        }
        else if (YearSelection != "Other" &&
            (!int.TryParse(YearSelection, out var year) || year is < 1 or > 6))
        {
            yield return new ValidationResult("กรุณาเลือกชั้นปี 1–6 หรืออื่น ๆ", [nameof(YearSelection)]);
        }

        if (!Guid.TryParse(ScholarshipTypeId, out var typeId) || typeId == Guid.Empty)
        {
            yield return new ValidationResult("กรุณาเลือกประเภททุน", [nameof(ScholarshipTypeId)]);
        }

        if (HasMoreThanTwoDecimals(Gpax))
        {
            yield return new ValidationResult("GPAX ต้องมีทศนิยมไม่เกิน 2 ตำแหน่ง", [nameof(Gpax)]);
        }

        if (HasMoreThanTwoDecimals(RequestedAmount))
        {
            yield return new ValidationResult(
                "จำนวนเงินต้องมีทศนิยมไม่เกิน 2 ตำแหน่ง",
                [nameof(RequestedAmount)]);
        }

        var bankRequired = !IsEdit;
        if (bankRequired && string.IsNullOrWhiteSpace(BankAccountNumber))
        {
            yield return new ValidationResult("กรุณาระบุเลขบัญชี", [nameof(BankAccountNumber)]);
        }

        if (!string.IsNullOrWhiteSpace(BankAccountNumber))
        {
            if (PublicScholarshipRequestRules.ValidateBankAccount(BankAccountNumber).Length > 0)
            {
                foreach (var message in PublicScholarshipRequestRules.ValidateBankAccount(
                    BankAccountNumber))
                {
                    yield return new ValidationResult(message, [nameof(BankAccountNumber)]);
                }
            }

            var normalizedBank = PublicScholarshipRequestRules.NormalizeBankAccount(BankAccountNumber);
            if (PublicScholarshipRequestRules.ValidateBankAccount(
                    BankAccountConfirmation ?? string.Empty).Length > 0 ||
                !string.Equals(
                    normalizedBank,
                    PublicScholarshipRequestRules.NormalizeBankAccount(
                        BankAccountConfirmation ?? string.Empty),
                    StringComparison.Ordinal))
            {
                yield return new ValidationResult(
                    "เลขบัญชีที่ยืนยันไม่ตรงกัน",
                    [nameof(BankAccountConfirmation)]);
            }
        }

        if (!IsEdit)
        {
            if (ConsentMethod is not ("Document" or "Verbal" or "Other"))
            {
                yield return new ValidationResult(
                    "กรุณาเลือกวิธีรับ consent",
                    [nameof(ConsentMethod)]);
            }

            if (string.IsNullOrWhiteSpace(ConsentEvidenceNote))
            {
                yield return new ValidationResult(
                    "กรุณาระบุหลักฐานหรือบันทึกประกอบ consent",
                    [nameof(ConsentEvidenceNote)]);
            }
        }
    }

    public CreateAdminScholarshipRequest ToCreate(string pdpaNoticeVersion) => new()
    {
        StudentId = StudentId,
        StudentName = StudentName,
        AcademicUnitId = AcademicUnitId,
        FacultyName = FacultyName,
        Major = Major,
        YearLevel = ParseYearLevel(),
        YearLevelOther = YearSelection == "Other" ? YearLevelOther : null,
        Gpax = Gpax,
        Email = Email,
        ScholarshipTypeId = Guid.Parse(ScholarshipTypeId),
        RequestedAmount = RequestedAmount,
        BankAccountNumber = BankAccountNumber ?? string.Empty,
        Reason = Reason,
        PdpaNoticeVersion = pdpaNoticeVersion,
        ConsentMethod = ConsentMethod,
        ConsentEvidenceNote = ConsentEvidenceNote
    };

    public UpdateAdminScholarshipRequest ToUpdate() => new()
    {
        ExpectedUpdatedAt = ExpectedUpdatedAt,
        StudentId = StudentId,
        StudentName = StudentName,
        AcademicUnitId = AcademicUnitId,
        FacultyName = FacultyName,
        Major = Major,
        YearLevel = ParseYearLevel(),
        YearLevelOther = YearSelection == "Other" ? YearLevelOther : null,
        Gpax = Gpax,
        Email = Email,
        ScholarshipTypeId = Guid.Parse(ScholarshipTypeId),
        RequestedAmount = RequestedAmount,
        BankAccountNumber = string.IsNullOrWhiteSpace(BankAccountNumber)
            ? null
            : BankAccountNumber,
        Reason = Reason
    };

    public void Populate(AdminScholarshipRequestDetailResponse detail)
    {
        IsEdit = true;
        ExpectedUpdatedAt = detail.UpdatedAt;
        StudentId = detail.StudentId;
        StudentName = detail.StudentName;
        AcademicUnitId = detail.AcademicUnitId;
        FacultyName = detail.FacultyName;
        Major = detail.Major;
        YearSelection = detail.YearLevel?.ToString() ?? "Other";
        YearLevelOther = detail.YearLevelOther;
        Gpax = detail.Gpax;
        Email = detail.Email;
        ScholarshipTypeId = detail.ScholarshipTypeId.ToString();
        RequestedAmount = detail.RequestedAmount;
        Reason = detail.Reason;
    }

    private int? ParseYearLevel() =>
        int.TryParse(YearSelection, out var year) ? year : null;

    private static bool HasMoreThanTwoDecimals(decimal value) =>
        decimal.Round(value, 2) != value;
}
