using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Infrastructure.Ai;

/// <summary>
/// Tarmoqqa chiqmaydigan, oldindan tayyorlangan (`AnalysisJsonSchema`ga to'liq mos) JSON
/// qaytaruvchi soxta provider — testlar va lokal ishlab chiqish uchun (`prompts/16` MAXSUS
/// DIQQAT #5). **FAQAT Development/Test muhitida ro'yxatga olinadi** — bu qaror composition
/// root'da (`Api/Program.cs`, muhitga qarab shartli DI) qabul qilinadi, `Infrastructure`ning
/// o'zi buni bilmaydi — aks holda production'da haqiqiy AI tahlil o'rniga soxta javob ketib,
/// buni hech kim sezmasligi mumkin edi.
/// </summary>
public sealed class MockAiProvider : IAiAnalysisProvider
{
    /// <summary>
    /// Shartli qiymat: `MockAiProvider` haqiqiy `AiProviderConfig` yozuviga bog'lanmagan (odatda
    /// to'g'ridan-to'g'ri `IAiAnalysisProvider` sifatida in'yeksiya qilinadi, `IAiProviderResolver`
    /// orqali emas) — `Gemini` faqat interfeys talabini qondirish uchun tanlangan.
    /// </summary>
    public AiProvider Kind => AiProvider.Gemini;

    public Task<AiCompletionResult> CompleteJsonAsync(AiCompletionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.FromResult(new AiCompletionResult(
            Success: true,
            RawJson: SampleAnalysisJson,
            InputTokens: 1800,
            OutputTokens: 2200,
            DurationMs: 5,
            ErrorMessage: null,
            ErrorKind: AiErrorKind.None));
    }

    public Task<AiHealthResult> CheckHealthAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new AiHealthResult(true, "MockAiProvider — tarmoqqa chiqmaydi, doim sog'lom."));

    /// <summary>
    /// `AnalysisJsonSchema` (`docs/09` 5-bo'lim)ga to'liq mos, taqiqlangan atamasiz, lotin
    /// o'zbekcha, shaxsiy ma'lumotsiz namuna javob.
    /// </summary>
    private const string SampleAnalysisJson = """
        {
          "summary": "Bu o'quvchi ilmiy qiziqishga moyil, tartibli va mas'uliyatli. Ijtimoiy faollik o'rtacha, lekin o'z ustida ishlash istagi yuqori. Umumiy portret barqaror va rivojlanishga ochiq.",
          "personalityPortrait": "O'quvchi tahliliy fikrlashga moyil, mustaqil qaror qabul qilishni yaxshi ko'radi va yangi g'oyalarga qiziqadi. Vijdonlilik ko'rsatkichi yuqori bo'lgani uchun rejalashtirilgan ishni oxiriga yetkazish unga xos. Bir vaqtning o'zida u ancha introvert bo'lib, chuqur, ammo kam sonli munosabatlarni afzal ko'radi. Bu ikki xususiyat birgalikda — mustaqil tadqiqot yoki loyiha ishlarida yuqori natija ko'rsatishi mumkinligini ko'rsatadi. Emotsional barqarorligi yaxshi darajada, ya'ni stressli vaziyatlarda ham nisbatan tinch qola oladi.",
          "strengths": [
            { "title": "Tahliliy fikrlash", "description": "Murakkab masalalarni qismlarga bo'lib, tizimli yechim topa oladi.", "evidence": "Intuitiv (N) o'qi 71% — mavhum g'oyalar bilan ishlashga moyillik." },
            { "title": "Mas'uliyatlilik", "description": "Boshlagan ishini oxiriga yetkazishga intiladi, muddatlarga rioya qiladi.", "evidence": "Vijdonlilik 77% — yuqori daraja." },
            { "title": "Mustaqillik", "description": "Tashqi nazoratsiz ham o'z ustida ishlay oladi.", "evidence": "O'z-o'zini boshqarish ko'rsatkichi yuqori." },
            { "title": "Ilmiy qiziqish", "description": "Yangi bilim va tadqiqot yo'nalishlariga tabiiy qiziqish bildiradi.", "evidence": "Intellektual (I) tipi Holland kodida yetakchi." }
          ],
          "growthAreas": [
            { "title": "Ijtimoiy faollik", "description": "Guruh ishlarida ba'zan chetda qolishi mumkin.", "actionStep": "Haftasiga bir marta kichik guruh loyihasida faol ishtirok etsin." },
            { "title": "Fikrni ochiq bildirish", "description": "O'z fikrini boshqalar oldida ifodalashda tortinishi mumkin.", "actionStep": "Sinf muhokamalarida haftada kamida bir marta savol yoki fikr bildirsin." },
            { "title": "Vaqtni boshqarish", "description": "Ko'p vazifa bo'lganda ustuvorlik belgilashda qiynalishi mumkin.", "actionStep": "Har hafta boshida 3 ta asosiy vazifani yozib chiqsin." }
          ],
          "learningStyle": "Bu o'quvchi mustaqil, chuqur va tizimli o'rganishni afzal ko'radi. Nazariy tushuntirishdan keyin amaliy mashq bilan mustahkamlash yaxshi natija beradi. Shovqinli, tez almashinuvchan muhitdan ko'ra, tinch va diqqatni jamlash mumkin bo'lgan sharoit unga mos.",
          "motivationProfile": "Uni ichki qiziqish va shaxsiy o'sish harakatga keltiradi — tashqi mukofotdan ko'ra, natijaning o'zi muhimroq. Aniq maqsad va mazmunli vazifa berilganda motivatsiyasi ortadi. Maqsadsiz yoki mazmunsiz ko'rinadigan takroriy ishlar uni to'xtatib qo'yishi mumkin.",
          "activityAssessment": "Hozirgi faollik darajasi o'rtacha — o'quv va qiziqish sohalarida barqaror, ammo ijtimoiy/jamoat faoliyatida ko'proq ishtirok etishi foydali bo'lardi. Kichik qadamlar bilan boshlash tavsiya etiladi.",
          "careerSuggestions": [
            { "field": "Muhandislik", "why": "Tahliliy fikrlash va aniq fanlarga qiziqish bu yo'nalishga mos keladi.", "exampleProfessions": ["Dasturiy injiniring", "Robototexnika"], "nextSteps": ["Robototexnika to'garagiga yozilish", "Onlayn dasturlash kursi boshlash"] },
            { "field": "Ilmiy tadqiqot", "why": "Mustaqil va chuqur o'rganishga moyillik ilmiy ishga mos.", "exampleProfessions": ["Tadqiqotchi", "Laborant"], "nextSteps": ["Fan olimpiadasida qatnashish", "Kichik tadqiqot loyihasi tanlash"] },
            { "field": "IT", "why": "Intellektual qiziqish va tizimli fikrlash dasturlashga mos.", "exampleProfessions": ["Backend dasturchi", "Ma'lumotlar tahlilchisi"], "nextSteps": ["Python asoslarini o'rganish", "Kichik loyiha qilib ko'rish"] }
          ],
          "studentRecommendations": [
            "Har kuni 20-30 daqiqa qiziqqan mavzuni chuqurroq o'rganishga vaqt ajrat.",
            "Guruh ishlarida kamida bitta fikringni ochiq aytishga harakat qil.",
            "Haftalik rejangni yozma tuzib, muhim vazifalarni belgila.",
            "O'z natijalaringni oldingi haftang bilan solishtir, boshqalar bilan emas.",
            "Yangi to'garak yoki qiziqish klubiga qo'shilib ko'r.",
            "Dam olish vaqtini ham rejangga qo'sh — bu samaradorlikni oshiradi."
          ],
          "teacherNotes": [
            "Mustaqil topshiriqlarda yaxshi natija ko'rsatadi — shunday vazifalarni ko'proq bering.",
            "Guruh ishida faollashtirish uchun aniq rol biriktiring.",
            "Ijodiy yoki tadqiqot yo'nalishidagi loyihalarga jalb qiling."
          ],
          "parentNotes": [
            "Farzandingizning mustaqil qiziqishlarini qo'llab-quvvatlang, tanlov erkinligini bering.",
            "Ijtimoiy faoliyatga (to'garak, jamoat ishi) asta-sekin jalb qilishga yordam bering.",
            "Yutuqlarini boshqalar bilan emas, o'zining oldingi natijasi bilan solishtiring."
          ],
          "attentionFlags": [],
          "disclaimer": "Bu tahlil hozirgi test natijalari asosidagi surat — o'zgarmas xususiyat emas, vaqt o'tishi bilan yangilanishi mumkin."
        }
        """;
}
