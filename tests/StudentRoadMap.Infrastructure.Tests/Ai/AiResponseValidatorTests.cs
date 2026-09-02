using FluentAssertions;
using Microsoft.Extensions.Configuration;
using StudentRoadMap.Infrastructure.Ai;

namespace StudentRoadMap.Infrastructure.Tests.Ai;

/// <summary>
/// `AiResponseValidator` — `docs/09-ai-analiz-moduli.md` 6-bo'lim, 5 bosqich: parse → schema →
/// taqiqlangan atamalar → til/uzunlik → ism sizmasligi. DoD talabi: "to'g'ri JSON, maydon
/// yetishmasligi, taqiqlangan so'z, kiril matn" testlari — barchasi shu klassda.
/// </summary>
public sealed class AiResponseValidatorTests
{
    private static IConfiguration EmptyConfiguration() => new ConfigurationBuilder().Build();

    private static AiResponseValidator NewValidator() => new(EmptyConfiguration());

    /// <summary>`AnalysisJsonSchema`ning barcha `required` maydonlariga to'liq mos, taqiqlangan atamasiz, lotin o'zbekcha to'g'ri javob.</summary>
    private const string ValidJson = """
        {
          "summary": "Bu o'quvchi ilmiy qiziqishga moyil, tartibli va mas'uliyatli. Ijtimoiy faollik o'rtacha darajada.",
          "personalityPortrait": "O'quvchi tahliliy fikrlashga moyil, mustaqil qaror qabul qilishni yaxshi ko'radi va yangi g'oyalarga qiziqadi. Vijdonlilik ko'rsatkichi yuqori bo'lgani uchun rejalashtirilgan ishni oxiriga yetkazish unga xos. Emotsional barqarorligi yaxshi darajada, stressli vaziyatlarda ham nisbatan tinch qola oladi va atrofdagilar bilan muloqotni saqlab qoladi.",
          "strengths": [
            { "title": "Tahliliy fikrlash", "description": "Murakkab masalalarni qismlarga bo'lib yechadi.", "evidence": "Intuitiv o'q 71%." },
            { "title": "Mas'uliyatlilik", "description": "Ishni oxiriga yetkazishga intiladi.", "evidence": "Vijdonlilik 77%." },
            { "title": "Mustaqillik", "description": "Tashqi nazoratsiz ham ishlay oladi.", "evidence": "O'z-o'zini boshqarish yuqori." },
            { "title": "Ilmiy qiziqish", "description": "Yangi bilimga tabiiy qiziqish bildiradi.", "evidence": "Intellektual tip yetakchi." }
          ],
          "growthAreas": [
            { "title": "Ijtimoiy faollik", "description": "Guruh ishida chetda qolishi mumkin.", "actionStep": "Kichik guruh loyihasida ishtirok etsin." },
            { "title": "Fikr bildirish", "description": "O'z fikrini ochiq aytishda tortinadi.", "actionStep": "Haftada bir marta fikr bildirsin." },
            { "title": "Vaqtni boshqarish", "description": "Ustuvorlik belgilashda qiynaladi.", "actionStep": "Haftalik reja tuzsin." }
          ],
          "learningStyle": "Mustaqil va tizimli o'rganishni afzal ko'radi.",
          "motivationProfile": "Ichki qiziqish uni harakatga keltiradi.",
          "activityAssessment": "Hozirgi faollik o'rtacha darajada.",
          "careerSuggestions": [
            { "field": "Muhandislik", "why": "Tahliliy fikrlash mos keladi.", "nextSteps": ["To'garakka yozilish", "Kurs boshlash"] },
            { "field": "IT", "why": "Tizimli fikrlash dasturlashga mos.", "nextSteps": ["Asoslarni o'rganish", "Loyiha qilish"] },
            { "field": "Ilmiy tadqiqot", "why": "Mustaqil o'rganishga moyillik.", "nextSteps": ["Olimpiadada qatnashish", "Tadqiqot tanlash"] }
          ],
          "studentRecommendations": [
            "Har kuni qiziqqan mavzuni o'rgan.",
            "Guruh ishida fikringni ayt.",
            "Haftalik reja tuz.",
            "Natijangni o'zing bilan solishtir.",
            "Yangi to'garakka qo'shil."
          ],
          "teacherNotes": [
            "Mustaqil topshiriqda yaxshi natija ko'rsatadi.",
            "Guruh ishida rol biriktiring.",
            "Tadqiqot loyihasiga jalb qiling."
          ],
          "parentNotes": [
            "Mustaqil qiziqishni qo'llab-quvvatlang.",
            "Ijtimoiy faoliyatga asta jalb qiling.",
            "Natijasini o'zi bilan solishtiring."
          ],
          "attentionFlags": [],
          "disclaimer": "Bu tahlil hozirgi holat surati, o'zgarmas xususiyat emas."
        }
        """;

    /// <summary>
    /// Xuddi shu sxemaga mos, lekin deyarli barcha matn KIRIL alifbosida (rus tilida) — `Validate_WithHighCyrillicRatio...`
    /// testi uchun. Faqat bitta katta so'zma-so'z bo'lakni almashtirish YETARLI EMAS edi (butun
    /// hujjatdagi ko'p sonli lotin harflari umumiy nisbatni 30% dan pastda ushlab turadi), shu
    /// sabab alohida, to'liq kiril hujjat kerak bo'ldi.
    /// </summary>
    private const string CyrillicJson = """
        {
          "summary": "Этот ученик склонен к научному интересу, организован и ответственен. Общественная активность средняя.",
          "personalityPortrait": "Ученик склонен к аналитическому мышлению, любит принимать самостоятельные решения и интересуется новыми идеями. Показатель добросовестности высокий, поэтому доведение запланированной работы до конца ему свойственно. Эмоциональная устойчивость на хорошем уровне, в стрессовых ситуациях он остаётся относительно спокойным.",
          "strengths": [
            { "title": "Аналитическое мышление", "description": "Разбивает сложные задачи на части и решает их системно.", "evidence": "Интуитивная ось 71%." },
            { "title": "Ответственность", "description": "Стремится довести начатое дело до конца.", "evidence": "Добросовестность 77%." },
            { "title": "Самостоятельность", "description": "Может работать без внешнего контроля.", "evidence": "Показатель саморегуляции высокий." },
            { "title": "Научный интерес", "description": "Проявляет естественный интерес к новым знаниям.", "evidence": "Интеллектуальный тип лидирует." }
          ],
          "growthAreas": [
            { "title": "Общественная активность", "description": "Может оставаться в стороне в групповой работе.", "actionStep": "Пусть участвует в небольшом групповом проекте." },
            { "title": "Выражение мнения", "description": "Стесняется открыто высказывать своё мнение.", "actionStep": "Пусть раз в неделю высказывает мнение." },
            { "title": "Управление временем", "description": "Затрудняется расставлять приоритеты.", "actionStep": "Пусть составляет недельный план." }
          ],
          "learningStyle": "Предпочитает самостоятельное и системное обучение.",
          "motivationProfile": "Его побуждает к действию внутренний интерес.",
          "activityAssessment": "Текущий уровень активности средний.",
          "careerSuggestions": [
            { "field": "Инженерия", "why": "Аналитическое мышление подходит.", "nextSteps": ["Записаться в кружок", "Начать курс"] },
            { "field": "ИТ", "why": "Системное мышление подходит программированию.", "nextSteps": ["Изучить основы", "Сделать проект"] },
            { "field": "Научные исследования", "why": "Склонность к самостоятельному изучению.", "nextSteps": ["Участвовать в олимпиаде", "Выбрать исследование"] }
          ],
          "studentRecommendations": [
            "Каждый день изучай интересную тему.",
            "Высказывай своё мнение в групповой работе.",
            "Составляй недельный план.",
            "Сравнивай результат с собой.",
            "Запишись в новый кружок."
          ],
          "teacherNotes": [
            "Хорошо показывает себя в самостоятельных заданиях.",
            "Назначайте роль в групповой работе.",
            "Привлекайте к исследовательскому проекту."
          ],
          "parentNotes": [
            "Поддерживайте самостоятельный интерес.",
            "Постепенно вовлекайте в общественную деятельность.",
            "Сравнивайте результат с его же прошлым результатом."
          ],
          "attentionFlags": [],
          "disclaimer": "Этот анализ — снимок текущего состояния, а не неизменная характеристика."
        }
        """;

    [Fact]
    public void Validate_WithValidJson_ReturnsOk()
    {
        var validator = NewValidator();

        var result = validator.Validate(ValidJson, piiTokens: []);

        result.Outcome.Should().Be(ValidationOutcome.Ok);
        result.FailedStage.Should().BeNull();
        result.ParsedJson.Should().NotBeNull();
    }

    [Fact]
    public void Validate_WithNullOrEmptyRawJson_ReturnsRetryAtParseStage()
    {
        var validator = NewValidator();

        var result = validator.Validate(null, piiTokens: []);

        result.Outcome.Should().Be(ValidationOutcome.Retry);
        result.FailedStage.Should().Be(AiValidationStage.Parse);
    }

    [Fact]
    public void Validate_WithMalformedJson_ReturnsRetryAtParseStage()
    {
        var validator = NewValidator();

        var result = validator.Validate("{ this is not valid json", piiTokens: []);

        result.Outcome.Should().Be(ValidationOutcome.Retry);
        result.FailedStage.Should().Be(AiValidationStage.Parse);
    }

    [Fact]
    public void Validate_WithMissingRequiredField_ReturnsRetryAtSchemaStage()
    {
        var validator = NewValidator();

        // Majburiy "summary" maydonini butunlay olib tashlaydi — qolgani schema bo'yicha to'g'ri.
        var missingField = System.Text.RegularExpressions.Regex.Replace(
            ValidJson, "\"summary\":\\s*\"[^\"]*\",\\s*", string.Empty);
        missingField.Should().NotContain("\"summary\"", "test tayyorlovi to'g'ri ishlagani uchun oldindan tekshiruv");

        var result = validator.Validate(missingField, piiTokens: []);

        result.Outcome.Should().Be(ValidationOutcome.Retry);
        result.FailedStage.Should().Be(AiValidationStage.Schema);
    }

    [Theory]
    [InlineData(1, ValidationOutcome.Retry)]
    [InlineData(2, ValidationOutcome.Moderated)]
    public void Validate_WithBannedTerm_ReturnsRetryOnFirstAttemptAndModeratedOnSecond(int attemptNumber, ValidationOutcome expected)
    {
        var validator = NewValidator();
        var withBannedTerm = ValidJson.Replace(
            "Bu tahlil hozirgi holat surati, o'zgarmas xususiyat emas.",
            "Bu tahlil hozirgi holat surati. Ehtimol tashxis qo'yish kerak bo'lar.",
            StringComparison.Ordinal);

        var result = validator.Validate(withBannedTerm, piiTokens: [], attemptNumber: attemptNumber);

        result.Outcome.Should().Be(expected);
        result.FailedStage.Should().Be(AiValidationStage.BannedTerms);

        if (expected == ValidationOutcome.Moderated)
        {
            result.ModeratedFields.Should().ContainSingle(f => f == "tashxis");
            result.ParsedJson.Should().NotBeNull("moderatsiya qilinganda ham JSON saqlanadi");
        }
    }

    [Fact]
    public void Validate_WithConfiguredBannedTerms_OverridesDefaultList()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:BannedTerms:0"] = "juda-noyob-soz",
            })
            .Build();
        var validator = new AiResponseValidator(configuration);

        // Standart ro'yxatdagi so'z ("dori") endi TEKSHIRILMAYDI, chunki konfiguratsiya berilgan.
        var withDefaultBannedWord = ValidJson.Replace(
            "Bu tahlil hozirgi holat surati, o'zgarmas xususiyat emas.",
            "Bu tahlil hozirgi holat surati. Shifokor dori tavsiya qilishi mumkin.",
            StringComparison.Ordinal);

        var result = validator.Validate(withDefaultBannedWord, piiTokens: []);
        result.Outcome.Should().Be(ValidationOutcome.Ok, "konfiguratsiyadagi ro'yxat standartni almashtiradi, 'dori' endi taqiqlanmagan");

        var withConfiguredWord = ValidJson.Replace(
            "Bu tahlil hozirgi holat surati, o'zgarmas xususiyat emas.",
            "Bu tahlil hozirgi holat surati. Bu yerda juda-noyob-soz bor.",
            StringComparison.Ordinal);

        var result2 = validator.Validate(withConfiguredWord, piiTokens: []);
        result2.Outcome.Should().Be(ValidationOutcome.Retry);
        result2.FailedStage.Should().Be(AiValidationStage.BannedTerms);
    }

    [Fact]
    public void Validate_WithHighCyrillicRatio_ReturnsRetryAtLanguageLengthStage()
    {
        var validator = NewValidator();

        var result = validator.Validate(CyrillicJson, piiTokens: []);

        result.Outcome.Should().Be(ValidationOutcome.Retry);
        result.FailedStage.Should().Be(AiValidationStage.LanguageLength);
    }

    [Fact]
    public void Validate_WithLeakedStudentName_ReturnsRetryAtNameLeakStage_NeverModerated()
    {
        var validator = NewValidator();
        var withLeakedName = ValidJson.Replace(
            "Bu o'quvchi ilmiy qiziqishga moyil,",
            "Sardorbek ilmiy qiziqishga moyil,",
            StringComparison.Ordinal);

        // Ikkinchi urinishda ham — ism sizishi HECH QACHON Moderated'ga aylanmasligi kerak.
        var result = validator.Validate(withLeakedName, piiTokens: ["Sardorbek"], attemptNumber: 2);

        result.Outcome.Should().Be(ValidationOutcome.Retry);
        result.FailedStage.Should().Be(AiValidationStage.NameLeak);
    }

    [Fact]
    public void Validate_WithShortPiiToken_IsIgnoredToAvoidFalsePositives()
    {
        var validator = NewValidator();

        // "A" sinf harfi kabi juda qisqa token — matnda tasodifan uchraydi, lekin PII emas.
        var result = validator.Validate(ValidJson, piiTokens: ["A"]);

        result.Outcome.Should().Be(ValidationOutcome.Ok);
    }
}
