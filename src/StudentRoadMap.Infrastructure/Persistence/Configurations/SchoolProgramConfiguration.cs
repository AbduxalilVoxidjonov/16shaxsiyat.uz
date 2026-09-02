using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`school_programs` jadvali — `docs/06-arxitektura.md` 8-bo'lim, `prompts/34` B6-band.</summary>
internal sealed class SchoolProgramConfiguration : IEntityTypeConfiguration<SchoolProgram>
{
    public void Configure(EntityTypeBuilder<SchoolProgram> builder)
    {
        builder.ToTable("school_programs");

        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(sp => sp.SchoolId).IsRequired();
        builder.Property(sp => sp.ProgramId).IsRequired();
        builder.Property(sp => sp.CreatedAt).IsRequired().HasDefaultValueSql("now()");

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(sp => sp.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AssessmentProgram>()
            .WithMany()
            .HasForeignKey(sp => sp.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sp => new { sp.SchoolId, sp.ProgramId }).IsUnique().HasDatabaseName("ux_school_programs");
        builder.HasIndex(sp => sp.ProgramId).HasDatabaseName("ix_school_programs_program");
    }
}
