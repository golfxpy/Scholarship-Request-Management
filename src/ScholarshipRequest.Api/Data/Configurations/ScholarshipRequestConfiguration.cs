using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarshipRequest.Api.Domain.Masters;
using ScholarshipRequest.Api.Domain.Privacy;
using ScholarshipRequest.Api.Domain.ScholarshipRequests;
using ScholarshipRequestEntity = ScholarshipRequest.Api.Domain.ScholarshipRequests.ScholarshipRequest;

namespace ScholarshipRequest.Api.Data.Configurations;

public sealed class RequestNumberCounterConfiguration : IEntityTypeConfiguration<RequestNumberCounter>
{
    public void Configure(EntityTypeBuilder<RequestNumberCounter> builder)
    {
        builder.ToTable("request_number_counters");
        builder.HasKey(counter => counter.BuddhistYear);
        builder.Property(counter => counter.BuddhistYear).HasColumnName("buddhist_year");
        builder.Property(counter => counter.LastValue).HasColumnName("last_value");

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_request_number_counters_last_value",
            "last_value >= 0"));
    }
}

public sealed class ScholarshipRequestConfiguration : IEntityTypeConfiguration<ScholarshipRequestEntity>
{
    public void Configure(EntityTypeBuilder<ScholarshipRequestEntity> builder)
    {
        builder.ToTable("scholarship_requests", table =>
        {
            table.HasCheckConstraint("ck_scholarship_requests_gpax", "gpax >= 0.00 AND gpax <= 4.00");
            table.HasCheckConstraint("ck_scholarship_requests_amount", "requested_amount > 0");
            table.HasCheckConstraint(
                "ck_scholarship_requests_year_level",
                "(year_level BETWEEN 1 AND 6 AND year_level_other IS NULL) OR " +
                "(year_level IS NULL AND year_level_other IS NOT NULL)");
            table.HasCheckConstraint(
                "ck_scholarship_requests_status",
                "status IN ('Pending', 'Approved', 'Rejected')");
            table.HasCheckConstraint(
                "ck_scholarship_requests_source",
                "submission_source IN ('Public', 'Staff')");
            table.HasCheckConstraint(
                "ck_scholarship_requests_consent_method",
                "consent_method IN ('Self', 'Document', 'Verbal', 'Other')");
            table.HasCheckConstraint(
                "ck_scholarship_requests_bank_last_four",
                "bank_account_last_four ~ '^[0-9]{4}$'");
        });

        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id).HasColumnName("id");
        builder.Property(request => request.RequestNumber)
            .HasColumnName("request_no").HasMaxLength(30).IsRequired();
        builder.Property(request => request.StudentId)
            .HasColumnName("student_id").HasMaxLength(20).IsRequired();
        builder.Property(request => request.StudentName)
            .HasColumnName("student_name").HasMaxLength(200).IsRequired();
        builder.Property(request => request.CampusId).HasColumnName("campus_id");
        builder.Property(request => request.AcademicUnitId).HasColumnName("academic_unit_id");
        builder.Property(request => request.FacultyNameSnapshot)
            .HasColumnName("faculty_name_snapshot").HasMaxLength(200).IsRequired();
        builder.Property(request => request.Major)
            .HasColumnName("major").HasMaxLength(150);
        builder.Property(request => request.EducationLevel)
            .HasColumnName("education_level").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(request => request.YearLevel).HasColumnName("year_level");
        builder.Property(request => request.YearLevelOther)
            .HasColumnName("year_level_other").HasMaxLength(100);
        builder.Property(request => request.Gpax)
            .HasColumnName("gpax").HasPrecision(3, 2);
        builder.Property(request => request.Email)
            .HasColumnName("email").HasMaxLength(254).IsRequired();
        builder.Property(request => request.ScholarshipTypeId).HasColumnName("scholarship_type_id");
        builder.Property(request => request.RequestedAmount)
            .HasColumnName("requested_amount").HasPrecision(12, 2);
        builder.Property(request => request.ProtectedBankAccountNumber)
            .HasColumnName("protected_bank_account_number").HasColumnType("text").IsRequired();
        builder.Property(request => request.BankAccountLastFour)
            .HasColumnName("bank_account_last_four").HasMaxLength(4).IsFixedLength().IsRequired();
        builder.Property(request => request.Reason)
            .HasColumnName("reason").HasMaxLength(2000).IsRequired();
        builder.Property(request => request.Status)
            .HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(request => request.DecisionNote)
            .HasColumnName("decision_note").HasMaxLength(2000);
        builder.Property(request => request.DecidedAt).HasColumnName("decided_at");
        builder.Property(request => request.DecidedById).HasColumnName("decided_by_id");
        builder.Property(request => request.SubmissionSource)
            .HasColumnName("submission_source").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(request => request.CreatedById).HasColumnName("created_by_id");
        builder.Property(request => request.PdpaNoticeId).HasColumnName("pdpa_notice_id");
        builder.Property(request => request.ConsentMethod)
            .HasColumnName("consent_method").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(request => request.ConsentEvidenceNote)
            .HasColumnName("consent_evidence_note").HasMaxLength(500);
        builder.Property(request => request.ConsentObtainedAt).HasColumnName("consent_obtained_at");
        builder.Property(request => request.SubmittedAt).HasColumnName("submitted_at");
        builder.Property(request => request.CreatedAt).HasColumnName("created_at");
        builder.Property(request => request.UpdatedAt).HasColumnName("updated_at");
        builder.Property(request => request.UpdatedById).HasColumnName("updated_by_id");
        builder.Property(request => request.DeletedAt).HasColumnName("deleted_at");
        builder.Property(request => request.DeletedById).HasColumnName("deleted_by_id");

        builder.HasOne<Campus>()
            .WithMany()
            .HasForeignKey(request => request.CampusId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AcademicUnit>()
            .WithMany()
            .HasForeignKey(request => request.AcademicUnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ScholarshipType>()
            .WithMany()
            .HasForeignKey(request => request.ScholarshipTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PdpaNotice>()
            .WithMany()
            .HasForeignKey(request => request.PdpaNoticeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(request => request.RequestNumber).IsUnique();
        builder.HasIndex(request => request.StudentId);
        builder.HasIndex(request => new { request.Status, request.SubmittedAt });
        builder.HasIndex(request => new { request.ScholarshipTypeId, request.SubmittedAt });
        builder.HasIndex(request => new { request.CampusId, request.SubmittedAt });
        builder.HasQueryFilter(request => request.DeletedAt == null);
    }
}
