using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`ai_provider_configs` jadvali — `docs/05-database-schema.md` 2-bo'lim.</summary>
internal sealed class AiProviderConfigConfiguration : IEntityTypeConfiguration<AiProviderConfig>
{
    public void Configure(EntityTypeBuilder<AiProviderConfig> builder)
    {
        builder.ToTable("ai_provider_configs");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.Provider).HasConversion<short>().IsRequired();
        builder.Property(c => c.DisplayName).HasMaxLength(80).IsRequired();
        builder.Property(c => c.ApiKeyEncrypted);
        builder.Property(c => c.Model).HasMaxLength(80).IsRequired();
        builder.Property(c => c.BaseUrl).HasMaxLength(200);
        builder.Property(c => c.MaxOutputTokens).IsRequired().HasDefaultValue(4096);
        builder.Property(c => c.Temperature).HasColumnType("numeric(3,2)").IsRequired().HasDefaultValue(0.4m);
        builder.Property(c => c.IsDefault).IsRequired().HasDefaultValue(false);
        builder.Property(c => c.IsActive).IsRequired().HasDefaultValue(false);
        builder.Property(c => c.FallbackOrder).IsRequired().HasDefaultValue(100);
        builder.Property(c => c.LastCheckedAt);
        builder.Property(c => c.LastCheckStatus).HasMaxLength(200);
        builder.Property(c => c.CreatedAt).IsRequired().HasDefaultValueSql("now()");
        builder.Property(c => c.UpdatedAt).IsRequired().HasDefaultValueSql("now()");

        builder.HasIndex(c => c.IsDefault)
            .IsUnique()
            .HasFilter("is_default = true")
            .HasDatabaseName("ux_ai_provider_default");

        builder.HasIndex(c => c.Provider).IsUnique().HasDatabaseName("ux_ai_provider_kind");
    }
}
