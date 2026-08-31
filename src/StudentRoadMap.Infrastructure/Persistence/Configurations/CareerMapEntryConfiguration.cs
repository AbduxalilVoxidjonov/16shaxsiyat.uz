using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Infrastructure.Persistence.Converters;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`career_map` jadvali — `docs/05-database-schema.md` 2-bo'lim.</summary>
internal sealed class CareerMapEntryConfiguration : IEntityTypeConfiguration<CareerMapEntry>
{
    public void Configure(EntityTypeBuilder<CareerMapEntry> builder)
    {
        builder.ToTable("career_map");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.HollandCode).HasMaxLength(2).IsRequired();
        builder.Property(c => c.FieldNameUz).HasMaxLength(150).IsRequired();
        builder.Property(c => c.DescriptionUz);

        builder.Property(c => c.ExampleProfessions)
            .HasConversion(JsonValueConverters.StringListConverter)
            .HasColumnName("example_professions_json")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'[]'")
            .Metadata.SetValueComparer(JsonValueConverters.StringListComparer);

        builder.Property(c => c.RelevanceOrder).IsRequired().HasDefaultValue(1);

        builder.HasIndex(c => new { c.HollandCode, c.RelevanceOrder })
            .HasDatabaseName("ix_career_map_code");
    }
}
