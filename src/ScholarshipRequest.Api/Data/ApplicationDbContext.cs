using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ScholarshipRequest.Api.Data.Identity;
using ScholarshipRequest.Api.Domain.Masters;
using ScholarshipRequest.Api.Domain.Privacy;
using ScholarshipRequest.Api.Domain.ScholarshipRequests;
using ScholarshipRequestEntity = ScholarshipRequest.Api.Domain.ScholarshipRequests.ScholarshipRequest;

namespace ScholarshipRequest.Api.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Campus> Campuses => Set<Campus>();

    public DbSet<AcademicUnit> AcademicUnits => Set<AcademicUnit>();

    public DbSet<ScholarshipType> ScholarshipTypes => Set<ScholarshipType>();

    public DbSet<PdpaNotice> PdpaNotices => Set<PdpaNotice>();

    public DbSet<RequestNumberCounter> RequestNumberCounters => Set<RequestNumberCounter>();

    public DbSet<ScholarshipRequestEntity> ScholarshipRequests => Set<ScholarshipRequestEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
