using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.PublicUsers;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`students` jadvali — `docs/05-database-schema.md` 2-bo'lim.</summary>
internal sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        // `0` — `Student.NoGrade` (maktabda o'qimaydigan ommaviy foydalanuvchi); `1..11` — sinf.
        builder.ToTable("students", t => t.HasCheckConstraint("ck_students_grade", "grade BETWEEN 0 AND 11"));

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.SchoolId).IsRequired();

        // Maktab oqimida `null`, ommaviy makonda — akkaunt identifikatori.
        builder.Property(s => s.PublicUserId);

        // P52 (2026-09-11): ro'yxatdan o'tishsiz dastur (`RegistrationMode.None`) orqali
        // yaratilgan anonim yozuv — `BirthDate`/`Phone` bunday yozuvda DOIM `null`.
        builder.Property(s => s.IsAnonymous).IsRequired().HasDefaultValue(false);

        builder.Property(s => s.FullName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.NormalizedName).HasMaxLength(200).IsRequired();
        // NULLABLE (P52) — anonim o'quvchida tug'ilgan sana yo'q. Domen invarianti
        // (`Student` konstruktori) anonim BO'LMAGAN yozuvda buni majburiy qiladi.
        builder.Property(s => s.BirthDate).HasColumnType("date");
        builder.Property(s => s.Gender).HasConversion<short>().IsRequired().HasDefaultValue(Gender.Unspecified);
        builder.Property(s => s.Grade).IsRequired();
        builder.Property(s => s.ClassLetter).HasMaxLength(2);

        // NULLABLE (P52) — anonim o'quvchida telefon yo'q; konversiya `ParentPhone`dagi bilan
        // bir xil naqsh (`value != null ? ... : null`).
        builder.Property(s => s.Phone)
            .HasConversion(
                phone => phone != null ? phone.Value : null,
                value => value != null ? PhoneNumber.Create(value).Value : null)
            .HasMaxLength(20);

        builder.Property(s => s.ParentPhone)
            .HasConversion(
                phone => phone != null ? phone.Value : null,
                value => value != null ? PhoneNumber.Create(value).Value : null)
            .HasMaxLength(20);

        builder.Property(s => s.Email).HasMaxLength(150);
        builder.Property(s => s.ConsentGivenAt).IsRequired();
        builder.Property(s => s.ConsentVersion).HasMaxLength(30);
        builder.Property(s => s.ParentalConsent).IsRequired().HasDefaultValue(false);

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

        builder.HasOne<PublicUser>()
            .WithMany()
            .HasForeignKey(s => s.PublicUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ⚠️ O'ZGARTIRILDI: filtrga `public_user_id IS NULL` qo'shildi.
        //
        // Sabab: ommaviy makonda BARCHA tashqi foydalanuvchi bitta `SchoolId` ostida turadi.
        // Eski filtr bilan bir xil F.I.Sh. + tug'ilgan sanali IKKI XIL odam (butun mamlakat
        // miqyosida — ehtimolligi nolga teng emas) bir-birini bloklab qo'yardi va ikkinchisi
        // umuman ro'yxatdan o'ta olmasdi. Endi bu unikallik faqat MAKTAB oqimiga tegishli
        // (u yerda F.I.Sh.+sana bitta maktab ichida haqiqatan ham identifikator) —
        // maktab oqimi uchun xatti-harakat AYNAN o'zgarishsiz qoladi.
        builder.HasIndex(s => new { s.SchoolId, s.NormalizedName, s.BirthDate })
            .IsUnique()
            .HasFilter("is_deleted = false AND public_user_id IS NULL")
            .HasDatabaseName("ux_students_identity");

        // Ommaviy makonda identifikator — Telegram akkaunti, ism emas: bitta akkauntga
        // BITTA o'quvchi profili (kabinet egasi), unga bir necha sessiya bog'lanadi.
        // Shu sabab 90 kunlik qayta-topshirish oynasi (`StartSessionCommandHandler`) ommaviy
        // oqimda `public_user_id` orqali topilgan profil bo'yicha ishlaydi — oyna qoidasi
        // BUZILMAYDI, faqat o'quvchini topish kaliti almashadi.
        builder.HasIndex(s => s.PublicUserId)
            .IsUnique()
            .HasFilter("public_user_id IS NOT NULL AND is_deleted = false")
            .HasDatabaseName("ux_students_public_user");

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
