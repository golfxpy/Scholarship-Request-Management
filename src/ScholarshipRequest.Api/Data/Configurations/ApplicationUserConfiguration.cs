using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarshipRequest.Api.Data.Identity;

namespace ScholarshipRequest.Api.Data.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("users");

        builder.Property(user => user.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.IsActive)
            .HasColumnName("is_active");

        builder.Property(user => user.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(user => user.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
