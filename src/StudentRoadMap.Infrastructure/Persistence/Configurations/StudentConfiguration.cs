using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`students` jadvali — `docs/05-database-schema.md` 2-bo'lim.</summary>
internal sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students", t => t.HasCheckConstraint("ck_students_grade", "grade BETWEEN 1 AND 11"));

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.SchoolId).IsRequired();

        builder.Property(s => s.FullName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.NormalizedName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.BirthDate).HasColumnType("date").IsRequired();
        builder.Property(s => s.Gender).HasConversion<short>().IsRequired().HasDefaultValue(Gender.Unspecified);
        builder.Property(s => s.Grade).IsRequired();
        builder.Property(s => s.ClassLetter).HasMaxLength(2);

        builder.Property(s => s.Phone)
            .HasConversion(phone => phone.Value, value => PhoneNumber.Create(value).Value)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.ParentPhone)
            .HasConversion(
                phone => phone != null ? phone.Value : null,
                value => value != null ? PhoneNumber.Create(value).Value : null)
            .HasMaxLength(20);

        builder.Property(s => s.Email).HasMaxLength(150);
        builder.Property(s => s.ConsentGivenAt).IsRequired();

        builder.Property(s => s.LastPersonalityType).HasMaxLength(4);

        builder.Property(s => s.LastMaturityIndex)
            .HasConversion(v => (decimal?)v, v => v != null ? (double?)v : null)
            .HasColumnType("numeric(5,2)");

        builder.Property(s => s.LastActivityIndex)
            .HasConversion(v => (decimal?)v, v => v != null ? (double?)v : null)
            .HasColumnType("numeric(5,2)");

        builder.Property(s => s.LastActivityLevel).HasConversion<short?>();
        builder.Property(s => s.LastHollandCode).HasMaxLength(3);
        builder.Property(s => s.NeedsAttention).IsRequired().HasDefaultValue(false);
        builder.Property(s => s.CompletedAssessmentCount).IsRequired().HasDefaultValue(0);

        builder.Property(s => s.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(s => s.CreatedAt).IsRequired().HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedAt).IsRequired().HasDefaultValueSql("now()");

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(s => s.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.SchoolId, s.NormalizedName, s.BirthDate })
            .IsUnique()
            .HasFilter("is_deleted = false")
            .HasDatabaseName("ux_students_identity");

        builder.HasIndex(s => new { s.SchoolId, s.Grade })
            .HasDatabaseName("ix_students_school_grade");

        builder.HasIndex(s => s.FullName)
            .HasDatabaseName("ix_students_name_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasIndex(s => s.NeedsAttention)
            .HasFilter("needs_attention = true")
            .HasDatabaseName("ix_students_attention");

        // `docs/05` §2, §5: `(last_assessment_at DESC NULLS LAST)` — hech qachon test
        // topshirmagan o'quvchi admin "so'nggi faollik" ro'yxati OXIRIDA turishi kerak.
        // EF fluent API'da NULLS tartibi sozlanmaydi — shu sabab bu indeks model'da
        // (snapshot mosligi uchun) DESC sifatida qoladi, lekin FIZIK ravishda xom SQL bilan
        // `Migrations/*_InitialCreate.cs` da `NULLS LAST` bilan qayta yaratiladi.
        builder.HasIndex(s => s.LastAssessmentAt)
            .HasDatabaseName("ix_students_last_at")
            .IsDescending();
    }
}
