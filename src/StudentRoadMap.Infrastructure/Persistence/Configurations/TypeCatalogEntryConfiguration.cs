using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Infrastructure.Persistence.Converters;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>
/// `type_catalog` jadvali — `docs/05-database-schema.md` 2-bo'lim. `TypeCatalogEntry` — `ValueObject`
/// (parametrsiz konstruktori yo'q), EF Core uni konstruktor bog'lash (constructor binding) orqali
/// materializatsiya qiladi.
/// </summary>
internal sealed class TypeCatalogEntryConfiguration : IEntityTypeConfiguration<TypeCatalogEntry>
{
    public void Configure(EntityTypeBuilder<TypeCatalogEntry> builder)
    {
        builder.ToTable("type_catalog");

        builder.HasKey(t => t.Code);
        builder.Property(t => t.Code).HasMaxLength(4).ValueGeneratedNever();

        builder.Property(t => t.NameUz).HasMaxLength(80).IsRequired();
        builder.Property(t => t.ShortDescriptionUz).HasMaxLength(300).IsRequired();
        builder.Property(t => t.LongDescriptionUz).IsRequired();

        builder.Property(t => t.Strengths)
            .HasConversion(JsonValueConverters.StringListConverter)
            .HasColumnName("strengths_json")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'[]'")
            .Metadata.SetValueComparer(JsonValueConverters.StringListComparer);

        builder.Property(t => t.GrowthAreas)
            .HasConversion(JsonValueConverters.StringListConverter)
            .HasColumnName("growth_areas_json")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'[]'")
            .Metadata.SetValueComparer(JsonValueConverters.StringListComparer);

        builder.Property(t => t.CareerHints)
            .HasConversion(JsonValueConverters.StringListConverter)
            .HasColumnName("career_hints_json")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'[]'")
            .Metadata.SetValueComparer(JsonValueConverters.StringListComparer);
    }
}
