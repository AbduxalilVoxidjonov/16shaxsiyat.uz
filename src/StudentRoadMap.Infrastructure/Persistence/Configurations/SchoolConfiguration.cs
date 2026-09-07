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

        // `docs/05` 3-bo'lim: 1 = School, 2 = PublicSpace. Ustunga DB DEFAULT ATAYLAB
        // QO'YILMAYDI: `SchoolKind.School = 1`, CLR standarti esa 0 — EF bunday holatda
        // "sentinel" ogohlantirishini beradi va 0 qiymatli yozuvda DB defaultiga o'tib
        // ketardi. Mavjud qatorlar migratsiyaning O'ZIDA (nullable qo'sh → backfill →
        // `SET NOT NULL`) to'ldiriladi, domen esa `Kind` ni har doim aniq beradi.
        builder.Property(s => s.Kind).HasConversion<short>().IsRequired();

        builder.Property(s => s.AccessToken).HasMaxLength(64).IsRequired();
        builder.Property(s => s.AccessCode).HasMaxLength(6);

        // Maktab kodi (`docs/08` 3a): 8 belgi, saqlashda defissiz. Nullable — ommaviy makon
        // (`kind = 2`) uchun `null`; `kind = 1` uchun domen (`School.Create`) har doim beradi.
        builder.Property(s => s.EntryCode).HasMaxLength(SchoolEntryCode.Length);
        builder.Property(s => s.DailyRegistrationLimit).IsRequired().HasDefaultValue(500);
        builder.Property(s => s.IsActive).IsRequired().HasDefaultValue(true);

        // Ilgari global `App:ShowResultToStudent` (standart `false`) edi — mavjud maktablar
        // uchun standart qiymat AYNAN o'sha bo'lib qoladi, ommaviy makon esa `true` bilan
        // seed qilinadi (`School.CreatePublicSpace`).
        builder.Property(s => s.ShowResultToStudent).IsRequired().HasDefaultValue(false);
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

        // Kod maktabni ANIQLAYDI (`resolve-code` shu ustun bo'yicha qidiradi) — unikal bo'lishi
        // shart. Qisman: `NULL` (ommaviy makon) unikallikka kirmaydi. `is_deleted` filtri YO'Q —
        // o'chirilgan maktab kodi ham qayta berilmaydi (eski qog'oz/ekranlardagi kod boshqa
        // maktabga olib bormasin).
        builder.HasIndex(s => s.EntryCode)
            .IsUnique()
            .HasFilter("entry_code IS NOT NULL")
            .HasDatabaseName("ux_schools_entry_code");

        // Bazada AYNAN BITTA ommaviy makon bo'lishining HAQIQIY kafolati. Domen tekshiruvi
        // (`School.CreatePublicSpace` yagona kirish nuqtasi + `Deactivate`/`MarkDeleted`
        // qulflari) NIYATNI ifodalaydi, lekin ikkita parallel tranzaksiya bir vaqtda ikkita
        // yozuv qo'shishini TO'XTATA OLMAYDI — buni faqat DB cheklovi qiladi. Qisman
        // (filtrli) indeks: `kind = 2` bo'lgan, o'chirilmagan qatorlar orasida yagona.
        builder.HasIndex(s => s.Kind)
            .IsUnique()
            .HasFilter("kind = 2 AND is_deleted = false")
            .HasDatabaseName("ux_schools_public_space");

        builder.HasIndex(s => new { s.Region, s.District })
            .HasDatabaseName("ix_schools_region_dist");

        builder.HasIndex(s => s.Name)
            .HasDatabaseName("ix_schools_name_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
    }
}
