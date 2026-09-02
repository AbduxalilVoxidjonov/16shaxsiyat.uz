using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`program_tests` jadvali — `docs/06-arxitektura.md` 8-bo'lim, `prompts/34` B6-band.</summary>
internal sealed class ProgramTestConfiguration : IEntityTypeConfiguration<ProgramTest>
{
    public void Configure(EntityTypeBuilder<ProgramTest> builder)
    {
        builder.ToTable("program_tests");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.ProgramId).IsRequired();
        builder.Property(t => t.TestDefinitionId).IsRequired();
        builder.Property(t => t.DisplayOrder).IsRequired();

        builder.HasOne<TestDefinition>()
            .WithMany()
            .HasForeignKey(t => t.TestDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.ProgramId, t.TestDefinitionId }).IsUnique().HasDatabaseName("ux_program_tests");
        builder.HasIndex(t => new { t.ProgramId, t.DisplayOrder }).HasDatabaseName("ix_program_tests_order");
    }
}
