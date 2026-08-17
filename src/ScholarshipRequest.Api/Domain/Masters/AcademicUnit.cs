namespace ScholarshipRequest.Api.Domain.Masters;

public sealed class AcademicUnit
{
    public Guid Id { get; set; }

    public Guid CampusId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
