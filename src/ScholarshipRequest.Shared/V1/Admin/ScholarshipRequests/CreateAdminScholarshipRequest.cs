namespace ScholarshipRequest.Shared.V1.Admin.ScholarshipRequests;

public sealed class CreateAdminScholarshipRequest
{
    public string StudentId { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public Guid? AcademicUnitId { get; set; }

    public string FacultyName { get; set; } = string.Empty;

    public string? Major { get; set; }

    public int? YearLevel { get; set; }

    public string? YearLevelOther { get; set; }

    public decimal Gpax { get; set; }

    public string Email { get; set; } = string.Empty;

    public Guid ScholarshipTypeId { get; set; }

    public decimal RequestedAmount { get; set; }

    public string BankAccountNumber { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string PdpaNoticeVersion { get; set; } = string.Empty;

    public string ConsentMethod { get; set; } = string.Empty;

    public string ConsentEvidenceNote { get; set; } = string.Empty;
}
