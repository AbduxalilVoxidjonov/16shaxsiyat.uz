using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`registration_counters` jadvali — `docs/05-database-schema.md` "RATE LIMIT" bo'limi.</summary>
internal sealed class RegistrationCounterConfiguration : IEntityTypeConfiguration<RegistrationCounter>
{
    public void Configure(EntityTypeBuilder<RegistrationCounter> builder)
    {
        builder.ToTable("registration_counters");

        builder.HasKey(r => new { r.SchoolId, r.DateUtc });

        builder.Property(r => r.SchoolId).IsRequired();
        builder.Property(r => r.DateUtc).IsRequired().HasColumnType("date");
        builder.Property(r => r.Count).IsRequired().HasDefaultValue(0);

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(r => r.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
