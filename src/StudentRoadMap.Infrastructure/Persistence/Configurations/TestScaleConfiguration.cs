using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Infrastructure.Persistence.Converters;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>
/// `test_scales` jadvali — `docs/05-database-schema.md` 2-bo'lim (DDL allaqachon hujjatda bor,
/// migratsiyaga P37 (`prompts/37-katalog-crud-backend.md`) da qo'shiladi — `docs/06` §8
/// 2026-08-31 qarori: "`test_scales` ... jadvallari `InitialCreate` da yo'q, domenda mos
/// entity hali yo'q edi").
/// </summary>
internal sealed class TestScaleConfiguration : IEntityTypeConfiguration<TestScale>
{
    public void Configure(EntityTypeBuilder<TestScale> builder)
    {
        builder.ToTable("test_scales");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.TestDefinitionId).IsRequired();
        builder.Property(s => s.Code).HasMaxLength(10).IsRequired();
        builder.Property(s => s.NameUz).HasMaxLength(120).IsRequired();
        builder.Property(s => s.DescriptionUz);
        builder.Property(s => s.DisplayOrder).IsRequired();

        builder.Property(s => s.InterpretationBands)
            .HasConversion(JsonValueConverters.InterpretationBandListConverter)
            .HasColumnName("interpretation_bands_json")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'[]'")
            .Metadata.SetValueComparer(JsonValueConverters.InterpretationBandListComparer);

        // Bog'lanish (`HasMany(t => t.Scales).WithOne()`) `TestDefinitionConfiguration`da
        // e'lon qilingan — `Questions`/`QuestionConfiguration` bilan bir xil naqsh (bitta
        // tomon egalik qiladi, ikkinchisida takrorlanmaydi).
        builder.HasIndex(s => new { s.TestDefinitionId, s.Code }).IsUnique().HasDatabaseName("ux_test_scales");
    }
}
