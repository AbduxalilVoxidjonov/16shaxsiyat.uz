using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Catalog.Branching;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`question_sections` jadvali — `docs/18-tarmoqlanuvchi-sorovnoma.md` §3.1.</summary>
internal sealed class QuestionSectionConfiguration : IEntityTypeConfiguration<QuestionSection>
{
    public void Configure(EntityTypeBuilder<QuestionSection> builder)
    {
        builder.ToTable("question_sections");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.TestDefinitionId).IsRequired();
        builder.Property(s => s.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(s => s.TitleUz).HasColumnName("title_uz").HasMaxLength(200).IsRequired();
        builder.Property(s => s.DescriptionUz).HasColumnName("description_uz").HasMaxLength(1000);
        builder.Property(s => s.DisplayOrder).HasColumnName("display_order").IsRequired();

        // `jsonb` — `TestResultConfiguration`dagi kabi xom ustun emas, bu yerda to'g'ridan-to'g'ri
        // `VisibilityRule` qiymat obyekti `VisibilityRuleJson` orqali (de)serializatsiya qilinadi
        // (`docs/18` §2.4/§3.1).
        builder.Property(s => s.VisibilityRule)
            .HasColumnName("visibility_rule")
            .HasColumnType("jsonb")
            .HasConversion(
                rule => VisibilityRuleJson.Serialize(rule),
                json => VisibilityRuleJson.Deserialize(json));

        // Bog'lanish (`HasMany(t => t.Sections).WithOne()`) `TestDefinitionConfiguration`da
        // (`Questions`/`Scales` bilan bir xil naqsh — bitta tomon egalik qiladi).
        builder.HasIndex(s => new { s.TestDefinitionId, s.Code }).IsUnique().HasDatabaseName("ux_question_sections_test_code");
        builder.HasIndex(s => new { s.TestDefinitionId, s.DisplayOrder }).HasDatabaseName("ix_question_sections_test_order");
    }
}
