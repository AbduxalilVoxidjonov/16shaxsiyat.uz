using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`answer_options` jadvali — `docs/05-database-schema.md` 2-bo'lim.</summary>
internal sealed class AnswerOptionConfiguration : IEntityTypeConfiguration<AnswerOption>
{
    public void Configure(EntityTypeBuilder<AnswerOption> builder)
    {
        builder.ToTable("answer_options");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(o => o.QuestionId).IsRequired();
        builder.Property(o => o.TextUz).IsRequired();
        builder.Property(o => o.Value).IsRequired();
        builder.Property(o => o.Scale).HasMaxLength(10);
        builder.Property(o => o.DisplayOrder).IsRequired();

        builder.HasIndex(o => new { o.QuestionId, o.DisplayOrder })
            .HasDatabaseName("ix_answer_options_question");
    }
}
