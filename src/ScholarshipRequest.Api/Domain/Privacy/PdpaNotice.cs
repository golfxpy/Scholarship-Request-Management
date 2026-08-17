namespace ScholarshipRequest.Api.Domain.Privacy;

public sealed class PdpaNotice
{
    public Guid Id { get; set; }

    public string Version { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset EffectiveAt { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
