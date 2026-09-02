using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`school_link_views` jadvali — `registration_counters` bilan bir xil naqsh (`docs/05-database-schema.md`, `prompts/15` kengaytmasi).</summary>
internal sealed class SchoolLinkViewConfiguration : IEntityTypeConfiguration<SchoolLinkView>
{
    public void Configure(EntityTypeBuilder<SchoolLinkView> builder)
    {
        builder.ToTable("school_link_views");

        builder.HasKey(v => new { v.SchoolId, v.DateUtc });

        builder.Property(v => v.SchoolId).IsRequired();
        builder.Property(v => v.DateUtc).IsRequired().HasColumnType("date");
        builder.Property(v => v.Count).IsRequired().HasDefaultValue(0);

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(v => v.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
