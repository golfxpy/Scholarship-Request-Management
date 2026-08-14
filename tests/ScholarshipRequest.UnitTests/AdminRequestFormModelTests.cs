using System.ComponentModel.DataAnnotations;
using ScholarshipRequest.Client.Features.Admin.ScholarshipRequests.Pages;
using ScholarshipRequest.Shared.V1.Admin.ScholarshipRequests;

namespace ScholarshipRequest.UnitTests;

public sealed class AdminRequestFormModelTests
{
    [Fact]
    public void CreateMode_ShouldRequireNonSelfConsentAndEvidence()
    {
        var model = CreateValidModel();
        model.ConsentMethod = "Self";
        model.ConsentEvidenceNote = "   ";

        var results = Validate(model);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.ConsentMethod)));
        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(model.ConsentEvidenceNote)));
    }

    [Fact]
    public void EditMode_ShouldAllowBankFieldsToRemainEmpty()
    {
        var model = CreateValidModel();
        model.IsEdit = true;
        model.BankAccountNumber = null;
        model.BankAccountConfirmation = null;
        model.ConsentMethod = string.Empty;
        model.ConsentEvidenceNote = string.Empty;

        var results = Validate(model);

        Assert.Empty(results);
        Assert.Null(model.ToUpdate().BankAccountNumber);
    }

    [Fact]
    public void BankConfirmation_ShouldRejectLettersEvenWhenDigitsMatch()
    {
        var model = CreateValidModel();
        model.BankAccountConfirmation = "123A4567890";

        var results = Validate(model);

        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(model.BankAccountConfirmation)));
    }

    [Fact]
    public void OtherYear_ShouldRequireExplanation()
    {
        var model = CreateValidModel();
        model.YearSelection = "Other";
        model.YearLevelOther = null;

        var results = Validate(model);

        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(model.YearLevelOther)));
    }

    [Fact]
    public void EditMode_ShouldRoundTripAcademicUnitId()
    {
        var academicUnitId = Guid.NewGuid();
        var model = new AdminRequestFormModel();
        model.Populate(new AdminScholarshipRequestDetailResponse(
            Guid.NewGuid(),
            "SCH-2569-000001",
            "6612345678",
            "นักศึกษาทดสอบ",
            academicUnitId,
            "คณะทดสอบ",
            null,
            "Undergraduate",
            3,
            null,
            3.25m,
            "student@example.invalid",
            Guid.NewGuid(),
            "TYPE",
            "ทุนทดสอบ",
            10_000m,
            "******7890",
            "เหตุผลทดสอบ",
            "Pending",
            null,
            null,
            null,
            null,
            "Staff",
            Guid.NewGuid(),
            "เจ้าหน้าที่ทดสอบ",
            "POC-v1",
            "Document",
            "หลักฐาน consent",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            true,
            true,
            true));

        Assert.Equal(academicUnitId, model.AcademicUnitId);
        Assert.Equal(academicUnitId, model.ToUpdate().AcademicUnitId);
    }

    private static AdminRequestFormModel CreateValidModel() => new()
    {
        StudentId = "6612345678",
        StudentName = "นักศึกษาทดสอบ",
        FacultyName = "คณะทดสอบ",
        Major = "หลักสูตรทดสอบ",
        YearSelection = "3",
        Gpax = 3.25m,
        Email = "student@example.invalid",
        ScholarshipTypeId = Guid.NewGuid().ToString(),
        RequestedAmount = 10_000m,
        BankAccountNumber = "123-456-7890",
        BankAccountConfirmation = "1234567890",
        Reason = "เหตุผลสำหรับการทดสอบ",
        ConsentMethod = "Document",
        ConsentEvidenceNote = "ได้รับเอกสาร consent แล้ว"
    };

    private static IReadOnlyList<ValidationResult> Validate(AdminRequestFormModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            results,
            validateAllProperties: true);
        return results;
    }
}
