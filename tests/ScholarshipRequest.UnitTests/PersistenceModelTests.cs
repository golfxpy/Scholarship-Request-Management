using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ScholarshipRequest.Api.Data;
using ScholarshipRequest.Api.Domain.Masters;
using ScholarshipRequest.Api.Domain.Privacy;
using ScholarshipRequest.Api.Domain.ScholarshipRequests;
using ScholarshipRequestEntity = ScholarshipRequest.Api.Domain.ScholarshipRequests.ScholarshipRequest;

namespace ScholarshipRequest.UnitTests;

public sealed class PersistenceModelTests
{
    [Fact]
    public void Model_ShouldContainRequiredMasterData()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;

        var campusSeeds = model.FindEntityType(typeof(Campus))!.GetSeedData();
        var academicUnitSeeds = model.FindEntityType(typeof(AcademicUnit))!.GetSeedData();
        var scholarshipTypeSeeds = model.FindEntityType(typeof(ScholarshipType))!.GetSeedData();
        var pdpaNoticeSeeds = model.FindEntityType(typeof(PdpaNotice))!.GetSeedData();

        var campus = Assert.Single(campusSeeds);
        Assert.Equal(SeedData.HatYaiCampusId, campus[nameof(Campus.Id)]);
        Assert.Equal("HATYAI", campus[nameof(Campus.Code)]);
        Assert.Equal(16, academicUnitSeeds.Count());
        Assert.Equal(5, scholarshipTypeSeeds.Count());

        var pdpaNotice = Assert.Single(pdpaNoticeSeeds);
        Assert.Equal("POC-v1", pdpaNotice[nameof(PdpaNotice.Version)]);
        Assert.Equal(true, pdpaNotice[nameof(PdpaNotice.IsActive)]);
    }

    [Fact]
    public void ScholarshipRequestModel_ShouldEnforceCoreDatabaseRules()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entityType = model.FindEntityType(typeof(ScholarshipRequestEntity));

        Assert.NotNull(entityType);
        Assert.NotEmpty(entityType.GetDeclaredQueryFilters());
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.IsUnique &&
                index.Properties.Single().Name == nameof(ScholarshipRequestEntity.RequestNumber));

        var checkConstraintNames = entityType.GetCheckConstraints()
            .Select(constraint => constraint.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ck_scholarship_requests_gpax", checkConstraintNames);
        Assert.Contains("ck_scholarship_requests_amount", checkConstraintNames);
        Assert.Contains("ck_scholarship_requests_year_level", checkConstraintNames);
        Assert.Contains("ck_scholarship_requests_status", checkConstraintNames);
        Assert.Contains("ck_scholarship_requests_consent_method", checkConstraintNames);
        Assert.Contains("ck_scholarship_requests_bank_last_four", checkConstraintNames);
    }

    [Fact]
    public void ScholarshipRequest_ShouldDefaultToPendingUndergraduate()
    {
        var request = new ScholarshipRequestEntity();

        Assert.Equal(ScholarshipRequestStatus.Pending, request.Status);
        Assert.Equal(EducationLevel.Undergraduate, request.EducationLevel);
    }

    [Fact]
    public void AllAcademicUnits_ShouldBelongToHatYaiCampus()
    {
        Assert.All(
            SeedData.AcademicUnits,
            unit => Assert.Equal(SeedData.HatYaiCampusId, unit.CampusId));
        Assert.Equal(
            SeedData.AcademicUnits.Count,
            SeedData.AcademicUnits.Select(unit => unit.Code).Distinct(StringComparer.Ordinal).Count());
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=scholarship_model_tests;Username=postgres;Password=postgres")
            .Options;

        return new ApplicationDbContext(options);
    }
}
