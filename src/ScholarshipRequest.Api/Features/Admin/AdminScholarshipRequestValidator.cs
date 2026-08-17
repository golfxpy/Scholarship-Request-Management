using ScholarshipRequest.Api.Domain.ScholarshipRequests;
using ScholarshipRequest.Api.Features.PublicScholarshipRequests;
using ScholarshipRequest.Shared.V1.Admin.ScholarshipRequests;
using ScholarshipRequest.Shared.V1.ScholarshipRequests;

namespace ScholarshipRequest.Api.Features.Admin;

public sealed class AdminScholarshipRequestValidator(
    PublicScholarshipRequestValidator publicValidator)
{
    public Dictionary<string, string[]> Validate(CreateAdminScholarshipRequest request)
    {
        var errors = publicValidator.Validate(ToPublicRequest(
            request.StudentId,
            request.StudentName,
            request.AcademicUnitId,
            request.FacultyName,
            request.Major,
            request.YearLevel,
            request.YearLevelOther,
            request.Gpax,
            request.Email,
            request.ScholarshipTypeId,
            request.RequestedAmount,
            request.BankAccountNumber,
            request.Reason,
            request.PdpaNoticeVersion));

        ValidateConsent(request.ConsentMethod, request.ConsentEvidenceNote, errors);
        return errors;
    }

    public Dictionary<string, string[]> Validate(UpdateAdminScholarshipRequest request)
    {
        var errors = publicValidator.Validate(ToPublicRequest(
            request.StudentId,
            request.StudentName,
            request.AcademicUnitId,
            request.FacultyName,
            request.Major,
            request.YearLevel,
            request.YearLevelOther,
            request.Gpax,
            request.Email,
            request.ScholarshipTypeId,
            request.RequestedAmount,
            string.IsNullOrWhiteSpace(request.BankAccountNumber)
                ? "000000"
                : request.BankAccountNumber,
            request.Reason,
            "existing-consent"));

        if (request.ExpectedUpdatedAt is null)
        {
            errors[nameof(request.ExpectedUpdatedAt)] =
                ["ต้องระบุเวอร์ชันข้อมูลที่ใช้เริ่มแก้ไข"];
        }

        return errors;
    }

    private static CreatePublicScholarshipRequest ToPublicRequest(
        string studentId,
        string studentName,
        Guid? academicUnitId,
        string facultyName,
        string? major,
        int? yearLevel,
        string? yearLevelOther,
        decimal gpax,
        string email,
        Guid scholarshipTypeId,
        decimal requestedAmount,
        string bankAccountNumber,
        string reason,
        string pdpaNoticeVersion) =>
        new()
        {
            StudentId = studentId,
            StudentName = studentName,
            AcademicUnitId = academicUnitId,
            FacultyName = facultyName,
            Major = major,
            YearLevel = yearLevel,
            YearLevelOther = yearLevelOther,
            Gpax = gpax,
            Email = email,
            ScholarshipTypeId = scholarshipTypeId,
            RequestedAmount = requestedAmount,
            BankAccountNumber = bankAccountNumber,
            Reason = reason,
            PdpaConsent = true,
            PdpaNoticeVersion = pdpaNoticeVersion
        };

    private static void ValidateConsent(
        string consentMethod,
        string consentEvidenceNote,
        IDictionary<string, string[]> errors)
    {
        var validMethod = Enum.GetNames<ConsentMethod>()
            .Where(name => !string.Equals(
                name,
                nameof(ConsentMethod.Self),
                StringComparison.Ordinal))
            .Any(name => string.Equals(name, consentMethod, StringComparison.OrdinalIgnoreCase));
        if (!validMethod)
        {
            errors[nameof(CreateAdminScholarshipRequest.ConsentMethod)] =
                ["เจ้าหน้าที่ต้องระบุวิธีรับ consent เป็น Document, Verbal หรือ Other"];
        }

        if (string.IsNullOrWhiteSpace(consentEvidenceNote))
        {
            errors[nameof(CreateAdminScholarshipRequest.ConsentEvidenceNote)] =
                ["กรุณาระบุหลักฐานหรือบันทึกประกอบ consent"];
        }
        else if (consentEvidenceNote.Trim().Length > 500)
        {
            errors[nameof(CreateAdminScholarshipRequest.ConsentEvidenceNote)] =
                ["หลักฐานหรือบันทึกประกอบ consent ต้องยาวไม่เกิน 500 ตัวอักษร"];
        }
    }
}
