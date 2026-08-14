using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarshipRequest.Api.Domain.Masters;
using ScholarshipRequest.Api.Domain.Privacy;

namespace ScholarshipRequest.Api.Data.Configurations;

public sealed class CampusConfiguration : IEntityTypeConfiguration<Campus>
{
    public void Configure(EntityTypeBuilder<Campus> builder)
    {
        builder.ToTable("campuses");
        builder.HasKey(campus => campus.Id);

        builder.Property(campus => campus.Id).HasColumnName("id");
        builder.Property(campus => campus.Code)
            .HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(campus => campus.Name)
            .HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(campus => campus.IsActive).HasColumnName("is_active");
        builder.Property(campus => campus.SortOrder).HasColumnName("sort_order");
        builder.Property(campus => campus.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(campus => campus.Code).IsUnique();
        builder.HasData(SeedData.Campuses);
    }
}

public sealed class AcademicUnitConfiguration : IEntityTypeConfiguration<AcademicUnit>
{
    public void Configure(EntityTypeBuilder<AcademicUnit> builder)
    {
        builder.ToTable("academic_units");
        builder.HasKey(unit => unit.Id);

        builder.Property(unit => unit.Id).HasColumnName("id");
        builder.Property(unit => unit.CampusId).HasColumnName("campus_id");
        builder.Property(unit => unit.Code)
            .HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(unit => unit.Name)
            .HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(unit => unit.IsActive).HasColumnName("is_active");
        builder.Property(unit => unit.SortOrder).HasColumnName("sort_order");
        builder.Property(unit => unit.CreatedAt).HasColumnName("created_at");

        builder.HasOne<Campus>()
            .WithMany()
            .HasForeignKey(unit => unit.CampusId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(unit => new { unit.CampusId, unit.Code }).IsUnique();
        builder.HasIndex(unit => new { unit.CampusId, unit.IsActive, unit.SortOrder });
        builder.HasData(SeedData.AcademicUnits);
    }
}

public sealed class ScholarshipTypeConfiguration : IEntityTypeConfiguration<ScholarshipType>
{
    public void Configure(EntityTypeBuilder<ScholarshipType> builder)
    {
        builder.ToTable("scholarship_types");
        builder.HasKey(type => type.Id);

        builder.Property(type => type.Id).HasColumnName("id");
        builder.Property(type => type.Code)
            .HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(type => type.Name)
            .HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(type => type.Description)
            .HasColumnName("description").HasMaxLength(1000);
        builder.Property(type => type.IsActive).HasColumnName("is_active");
        builder.Property(type => type.SortOrder).HasColumnName("sort_order");
        builder.Property(type => type.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(type => type.Code).IsUnique();
        builder.HasData(SeedData.ScholarshipTypes);
    }
}

public sealed class PdpaNoticeConfiguration : IEntityTypeConfiguration<PdpaNotice>
{
    public void Configure(EntityTypeBuilder<PdpaNotice> builder)
    {
        builder.ToTable("pdpa_notices");
        builder.HasKey(notice => notice.Id);

        builder.Property(notice => notice.Id).HasColumnName("id");
        builder.Property(notice => notice.Version)
            .HasColumnName("version").HasMaxLength(30).IsRequired();
        builder.Property(notice => notice.Content)
            .HasColumnName("content").HasColumnType("text").IsRequired();
        builder.Property(notice => notice.EffectiveAt).HasColumnName("effective_at");
        builder.Property(notice => notice.IsActive).HasColumnName("is_active");
        builder.Property(notice => notice.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(notice => notice.Version).IsUnique();
        builder.HasIndex(notice => new { notice.IsActive, notice.EffectiveAt });
        builder.HasData(SeedData.PdpaNotices);
    }
}
