using ScholarshipRequest.Api.Features.PublicScholarshipRequests;
using ScholarshipRequest.Shared.V1.ScholarshipRequests;

namespace ScholarshipRequest.UnitTests;

public sealed class PublicScholarshipRequestValidatorTests
{
    private readonly PublicScholarshipRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldAcceptValidPublicRequest()
    {
        var request = CreateValidRequest();

        var errors = _validator.Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldRequireExactlyOneYearRepresentation()
    {
        var request = CreateValidRequest();
        request.YearLevel = 2;
        request.YearLevelOther = "เกินแผนการศึกษา";

        var errors = _validator.Validate(request);

        Assert.Contains(nameof(request.YearLevel), errors.Keys);
    }

    [Fact]
    public void Validate_ShouldAcceptOtherYearWithoutNumericYear()
    {
        var request = CreateValidRequest();
        request.YearLevel = null;
        request.YearLevelOther = "เกินแผนการศึกษา";

        var errors = _validator.Validate(request);

        Assert.DoesNotContain(nameof(request.YearLevel), errors.Keys);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234A67890")]
    [InlineData("1234567890123456789012345678901")]
    public void Validate_ShouldRejectInvalidBankAccount(string bankAccountNumber)
    {
        var request = CreateValidRequest();
        request.BankAccountNumber = bankAccountNumber;

        var errors = _validator.Validate(request);

        Assert.Contains(nameof(request.BankAccountNumber), errors.Keys);
    }

    [Fact]
    public void Validate_ShouldRejectMissingConsentAndScholarshipType()
    {
        var request = CreateValidRequest();
        request.PdpaConsent = false;
        request.ScholarshipTypeId = Guid.Empty;

        var errors = _validator.Validate(request);

        Assert.Contains(nameof(request.PdpaConsent), errors.Keys);
        Assert.Contains(nameof(request.ScholarshipTypeId), errors.Keys);
    }

    [Fact]
    public void NormalizeBankAccount_ShouldRemoveSpacesAndHyphens()
    {
        var normalized = PublicScholarshipRequestRules.NormalizeBankAccount("123-456 7890");

        Assert.Equal("1234567890", normalized);
    }

    private static CreatePublicScholarshipRequest CreateValidRequest() =>
        new()
        {
            StudentId = "6612345678",
            StudentName = "นักศึกษาทดสอบ",
            AcademicUnitId = Guid.NewGuid(),
            FacultyName = "คณะวิศวกรรมศาสตร์",
            Major = "วิศวกรรมคอมพิวเตอร์",
            YearLevel = 3,
            Gpax = 3.25m,
            Email = "student@example.com",
            ScholarshipTypeId = Guid.NewGuid(),
            RequestedAmount = 10_000m,
            BankAccountNumber = "123-456-7890",
            Reason = "ต้องการทุนเพื่อสนับสนุนค่าใช้จ่ายด้านการศึกษา",
            PdpaConsent = true,
            PdpaNoticeVersion = "POC-v1"
        };
}
