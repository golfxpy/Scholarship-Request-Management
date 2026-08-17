namespace ScholarshipRequest.Shared.V1.Privacy;

public sealed record PdpaNoticeResponse(
    Guid Id,
    string Version,
    string Content,
    DateTimeOffset EffectiveAt);
