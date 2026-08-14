namespace ScholarshipRequest.Api.Data;

public sealed class DevelopmentDemoSeedOptions
{
    public const string SectionName = "DevelopmentDemoSeed";

    public bool Enabled { get; set; }

    public string AdminUserName { get; set; } = string.Empty;

    public string AdminPassword { get; set; } = string.Empty;
}
