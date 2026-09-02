namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// ⚠️ P18-R1 (`prompts/18-ai-navbat-va-orkestratsiya.md`, MAJBURIY): `TransactionBehavior`
/// tranzaksiyasi MUVAFFAQIYATLI commit bo'lgandan KEYIN bajarilishi kerak bo'lgan amallar
/// ro'yxati. Yechim: "TransactionBehavior'ga commit'dan keyin bajariladigan amallar
/// ro'yxatini qo'shish" (`prompts/18` taklif qilingan uch variantdan biri — PM'ga hisobotda
/// tanlov sababi yozilgan).
/// <para>
/// Nega kerak: `AppDbContext.SaveChangesAsync` domen hodisalarini (`AssessmentCompletedEvent`
/// kabi) `SaveChanges` ICHIDA, ya'ni `TransactionBehavior` tranzaksiyasi hali commit
/// BO'LMASDAN turib publish qiladi. Fon ishchisi (`AnalysisWorkerBackgroundService`) esa
/// ALOHIDA DB ulanishi/tranzaksiya bilan ishlaydi — agar navbatga qo'yish darhol (hodisa
/// ichida) bajarilsa, ishchi hali COMMIT QILINMAGAN yozuvni o'qishga urinishi (yoki
/// tranzaksiya ROLLBACK bo'lsa — umuman mavjud bo'lmagan sessiya uchun vazifa navbatda
/// qolib ketishi) mumkin — klassik poyga holati.
/// </para>
/// <para>
/// Ishlatilishi: handler ichida darhol chaqirish o'rniga `Enqueue(...)` bilan navbatga
/// qo'yiladi; `TransactionBehavior` `CommitAsync()` MUVAFFAQIYATLI tugagandan keyin
/// `RunAsync(...)` chaqiradi. Tranzaksiya rollback bo'lsa (istisno otilsa) — ro'yxat hech
/// qachon ishga tushirilmaydi, oddiygina tashlab yuboriladi (P18-R2 shuni tekshiradi).
/// </para>
/// <para>
/// Request-scoped (DI, `Scoped`) — har bir MediatR so'rovi (demak, har bir `TransactionBehavior`
/// chaqiruvi) o'zining alohida ro'yxatiga ega. Ichma-ich (nested) `Send` chaqiruvlari bo'lmagani
/// uchun (`docs/06` — handler'lar bir-birini to'g'ridan-to'g'ri `ISender` orqali chaqirmaydi)
/// bitta so'rov davomida faqat bitta `TransactionBehavior` ushbu ro'yxatni ishlatadi.
/// </para>
/// </summary>
public interface IPostCommitActions
{
    /// <summary>Tranzaksiya muvaffaqiyatli commit bo'lgandan KEYIN bajariladigan amalni ro'yxatga qo'shadi.</summary>
    void Enqueue(Func<CancellationToken, Task> action);

    /// <summary>
    /// Ro'yxatdagi amallarni ketma-ket bajaradi va ro'yxatni tozalaydi — FAQAT `TransactionBehavior`
    /// tomonidan, commit muvaffaqiyatli bo'lgandan keyin chaqirilishi kerak.
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken);
}
