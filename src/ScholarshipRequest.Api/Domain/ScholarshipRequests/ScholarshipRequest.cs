namespace ScholarshipRequest.Api.Domain.ScholarshipRequests;

public sealed class ScholarshipRequest
{
    public Guid Id { get; set; }

    public string RequestNumber { get; set; } = string.Empty;

    public string StudentId { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public Guid CampusId { get; set; }

    public Guid? AcademicUnitId { get; set; }

    public string FacultyNameSnapshot { get; set; } = string.Empty;

    public string? Major { get; set; }

    public EducationLevel EducationLevel { get; set; } = EducationLevel.Undergraduate;

    public int? YearLevel { get; set; }

    public string? YearLevelOther { get; set; }

    public decimal Gpax { get; set; }

    public string Email { get; set; } = string.Empty;

    public Guid ScholarshipTypeId { get; set; }

    public decimal RequestedAmount { get; set; }

    public string ProtectedBankAccountNumber { get; set; } = string.Empty;

    public string BankAccountLastFour { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public ScholarshipRequestStatus Status { get; set; } = ScholarshipRequestStatus.Pending;

    public string? DecisionNote { get; set; }

    public DateTimeOffset? DecidedAt { get; set; }

    public Guid? DecidedById { get; set; }

    public SubmissionSource SubmissionSource { get; set; }

    public Guid? CreatedById { get; set; }

    public Guid PdpaNoticeId { get; set; }

    public ConsentMethod ConsentMethod { get; set; }

    public string? ConsentEvidenceNote { get; set; }

    public DateTimeOffset ConsentObtainedAt { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Guid? UpdatedById { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedById { get; set; }
}
