using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`prompt_templates` jadvali — `docs/05-database-schema.md` 2-bo'lim.</summary>
internal sealed class PromptTemplateConfiguration : IEntityTypeConfiguration<PromptTemplate>
{
    public void Configure(EntityTypeBuilder<PromptTemplate> builder)
    {
        builder.ToTable("prompt_templates");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Key).HasColumnName("key").HasMaxLength(50).IsRequired();
        builder.Property(p => p.Version).HasMaxLength(20).IsRequired();
        builder.Property(p => p.SystemText).IsRequired();
        builder.Property(p => p.UserText).IsRequired();
        builder.Property(p => p.JsonSchema).HasColumnType("jsonb").IsRequired();
        builder.Property(p => p.IsActive).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.CreatedAt).IsRequired().HasDefaultValueSql("now()");

        builder.HasIndex(p => new { p.Key, p.Version }).IsUnique().HasDatabaseName("ux_prompt_templates");
    }
}
