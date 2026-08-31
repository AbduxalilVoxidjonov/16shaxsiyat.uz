using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>
/// `test_definitions` jadvali — `docs/05-database-schema.md` 2-bo'lim.
///
/// <para>
/// **Bilinigan tafovut (PM'ga savol, hisobotga qarang):** DDL'da `question_count int NOT NULL`
/// ustuni bor, lekin `TestDefinition.QuestionCount` (P02, Domain) hisoblanadigan get-only
/// xususiyat (`_questions.Count`) — orqa maydon yo'q, shu sabab EF unga yoza olmaydi.
/// Shu yerda `Ignore()` qilingan — DB'da bu ustun **yaratilmaydi**.
/// </para>
/// </summary>
internal sealed class TestDefinitionConfiguration : IEntityTypeConfiguration<TestDefinition>
{
    public void Configure(EntityTypeBuilder<TestDefinition> builder)
    {
        builder.ToTable("test_definitions");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.Code).HasMaxLength(20).IsRequired();
        builder.Property(t => t.NameUz).HasMaxLength(150).IsRequired();
        builder.Property(t => t.DescriptionUz);
        builder.Property(t => t.Version).IsRequired().HasDefaultValue(1);
        builder.Property(t => t.DisplayOrder).IsRequired();

        builder.Ignore(t => t.QuestionCount);

        builder.Property(t => t.EstimatedMinutes).IsRequired();
        builder.Property(t => t.ShuffleQuestions).IsRequired().HasDefaultValue(false);
        builder.Property(t => t.PageSize).IsRequired().HasDefaultValue(10);
        builder.Property(t => t.IsActive).IsRequired().HasDefaultValue(true);
        // Sentinel — enum 1 dan boshlanadi (0 hech qachon domendan kelmaydi), lekin buni
        // EF'ga aniq aytamiz — aks holda "sentinel value sozlanmagan" ogohlantirishi chiqadi.
        builder.Property(t => t.Kind).HasConversion<short>().IsRequired().HasDefaultValue(TestKind.Standard).HasSentinel(default(TestKind));
        builder.Property(t => t.IsSystem).IsRequired().HasDefaultValue(false);
        builder.Property(t => t.ScoringStrategyCode).HasColumnName("scoring_strategy").HasMaxLength(20).IsRequired();
        builder.Property(t => t.Status).HasConversion<short>().IsRequired().HasDefaultValue(TestDefinitionStatus.Published).HasSentinel(default(TestDefinitionStatus));
        builder.Property(t => t.CreatedByAdminUserId);
        builder.Property(t => t.PublishedAt);
        builder.Property(t => t.CreatedAt).IsRequired().HasDefaultValueSql("now()");
        builder.Property(t => t.UpdatedAt).IsRequired().HasDefaultValueSql("now()");

        builder.Metadata.FindNavigation(nameof(TestDefinition.Questions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(t => t.Questions)
            .WithOne()
            .HasForeignKey(q => q.TestDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.Code).IsUnique().HasDatabaseName("ux_test_definitions_code");

        builder.HasIndex(t => new { t.IsActive, t.DisplayOrder })
            .HasFilter("status = 2")
            .HasDatabaseName("ix_test_definitions_active");
    }
}
