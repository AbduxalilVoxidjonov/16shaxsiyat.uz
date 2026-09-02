using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Jobs;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>
/// `analysis_jobs` jadvali — P18 fon navbati (`prompts/18`, `docs/09-ai-analiz-moduli.md`
/// 8-bo'lim). `docs/05-database-schema.md`da hali YO'Q (bu jadval P18da qo'shildi) — PM'ga
/// hisobotda qayd etilgan, `docs/05` yangilanishi PM zimmasida (`docs/` bu promptning fayl
/// egaligi tashqarisida).
/// </summary>
internal sealed class AnalysisJobConfiguration : IEntityTypeConfiguration<AnalysisJob>
{
    public void Configure(EntityTypeBuilder<AnalysisJob> builder)
    {
        builder.ToTable("analysis_jobs");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(j => j.AssessmentId).IsRequired();
        builder.Property(j => j.RequestedProvider).HasConversion<short?>();
        builder.Property(j => j.RequestedPromptVersion).HasMaxLength(20);
        builder.Property(j => j.Status).HasConversion<short>().IsRequired().HasDefaultValue(AnalysisJobStatus.Pending);
        builder.Property(j => j.AttemptCount).IsRequired().HasDefaultValue(0);
        builder.Property(j => j.LastError).HasMaxLength(2000);
        builder.Property(j => j.CreatedAt).IsRequired().HasDefaultValueSql("now()");
        builder.Property(j => j.UpdatedAt).IsRequired().HasDefaultValueSql("now()");

        builder.HasOne<Assessment>()
            .WithMany()
            .HasForeignKey(j => j.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // `AnalysisJobQueue.EnqueueAiAnalysisAsync` dublikat oldini olish — bitta sessiya uchun
        // bir vaqtda faqat bitta faol (`Pending`/`Running`, qiymat 0/1) vazifa (`prompts/18`
        // Cheklovlar: "Bir sessiya uchun bir vaqtda bitta job"). DB darajasidagi cheklov —
        // `AiProviderConfigConfiguration.ux_ai_provider_default` bilan bir xil naqsh.
        builder.HasIndex(j => j.AssessmentId)
            .IsUnique()
            .HasFilter("status IN (0, 1)")
            .HasDatabaseName("ux_analysis_jobs_active_per_assessment");

        builder.HasIndex(j => new { j.Status, j.CreatedAt }).HasDatabaseName("ix_analysis_jobs_status");
    }
}
