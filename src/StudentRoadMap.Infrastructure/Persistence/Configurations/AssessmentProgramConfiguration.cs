using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`assessment_programs` jadvali — `docs/06-arxitektura.md` 8-bo'lim (2026-09-02 qaror), `prompts/34` B6-band.</summary>
internal sealed class AssessmentProgramConfiguration : IEntityTypeConfiguration<AssessmentProgram>
{
    public void Configure(EntityTypeBuilder<AssessmentProgram> builder)
    {
        builder.ToTable("assessment_programs");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Code).HasMaxLength(30).IsRequired();
        builder.Property(p => p.NameUz).HasMaxLength(150).IsRequired();
        builder.Property(p => p.DescriptionUz);
        builder.Property(p => p.Kind).HasConversion<short>().IsRequired().HasDefaultValue(ProgramKind.Custom).HasSentinel(default(ProgramKind));
        builder.Property(p => p.Visibility).HasConversion<short>().IsRequired().HasDefaultValue(ProgramVisibility.Assigned).HasSentinel(default(ProgramVisibility));
        // P52 (2026-09-11): mavjud dasturlar `Full` bo'lib qoladi (default `1`) — xatti-harakati
        // o'zgarmaydi (`docs/05` migratsiya siyosati).
        builder.Property(p => p.RegistrationMode).HasConversion<short>().IsRequired().HasDefaultValue(RegistrationMode.Full).HasSentinel(default(RegistrationMode));
        builder.Property(p => p.Status).HasConversion<short>().IsRequired().HasDefaultValue(ProgramStatus.Draft).HasSentinel(default(ProgramStatus));
        builder.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(p => p.DisplayOrder).IsRequired();
        builder.Property(p => p.IsSystem).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.CreatedByAdminUserId);
        builder.Property(p => p.CreatedAt).IsRequired().HasDefaultValueSql("now()");
        builder.Property(p => p.UpdatedAt).IsRequired().HasDefaultValueSql("now()");

        builder.Metadata.FindNavigation(nameof(AssessmentProgram.Tests))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.Tests)
            .WithOne()
            .HasForeignKey(t => t.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.Code).IsUnique().HasDatabaseName("ux_assessment_programs_code");

        builder.HasIndex(p => new { p.IsActive, p.DisplayOrder })
            .HasFilter("status = 2")
            .HasDatabaseName("ix_assessment_programs_active");
    }
}
