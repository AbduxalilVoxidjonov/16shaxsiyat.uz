using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Settings;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>
/// `registration_form_settings` — bitta qator (singleton, `RegistrationFormSettings.SingletonId`
/// bilan qulflangan), 2026-09-11/12 (egasining talabi, `docs/18` §9.6).
/// </summary>
internal sealed class RegistrationFormSettingsConfiguration : IEntityTypeConfiguration<RegistrationFormSettings>
{
    public void Configure(EntityTypeBuilder<RegistrationFormSettings> builder)
    {
        builder.ToTable("registration_form_settings");

        builder.HasKey(s => s.Id);
        // `gen_random_uuid()` YO'Q — yagona qator DOIM `RegistrationFormSettings.SingletonId`
        // bilan yaratiladi (domen tomonidan berilgan qiymat), avtogenatsiya kerak emas.
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Definition)
            .HasColumnName("definition")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasConversion(
                definition => RegistrationFormDefinitionJson.Serialize(definition),
                json => RegistrationFormDefinitionJson.Deserialize(json)!);

        builder.Property(s => s.UpdatedAt).IsRequired().HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedByAdminUserId);
    }
}
