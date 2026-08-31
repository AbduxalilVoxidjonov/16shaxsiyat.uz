using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`schools` jadvali — `docs/05-database-schema.md` 2-bo'lim.</summary>
internal sealed class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    public void Configure(EntityTypeBuilder<School> builder)
    {
        builder.ToTable("schools");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Region).HasMaxLength(100).IsRequired();
        builder.Property(s => s.District).HasMaxLength(100).IsRequired();
        builder.Property(s => s.SchoolNumber).HasMaxLength(20);
        builder.Property(s => s.ContactPerson).HasMaxLength(150);
        builder.Property(s => s.ContactPhone).HasMaxLength(20);

        builder.Property(s => s.Slug)
            .HasConversion(slug => slug.Value, value => SchoolSlug.FromExisting(value))
            .HasMaxLength(SchoolSlug.MaxLength)
            .IsRequired();

        builder.Property(s => s.AccessToken).HasMaxLength(64).IsRequired();
        builder.Property(s => s.AccessCode).HasMaxLength(6);
        builder.Property(s => s.DailyRegistrationLimit).IsRequired().HasDefaultValue(500);
        builder.Property(s => s.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(s => s.Notes).HasMaxLength(1000);
        builder.Property(s => s.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(s => s.CreatedAt).IsRequired().HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedAt).IsRequired().HasDefaultValueSql("now()");

        builder.HasIndex(s => s.Slug)
            .IsUnique()
            .HasFilter("is_deleted = false")
            .HasDatabaseName("ux_schools_slug");

        builder.HasIndex(s => s.AccessToken)
            .IsUnique()
            .HasDatabaseName("ux_schools_token");

        builder.HasIndex(s => new { s.Region, s.District })
            .HasDatabaseName("ix_schools_region_dist");

        builder.HasIndex(s => s.Name)
            .HasDatabaseName("ix_schools_name_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
    }
}
