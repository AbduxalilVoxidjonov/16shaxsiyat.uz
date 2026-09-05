namespace StudentRoadMap.Api.IntegrationTests.Testing;

/// <summary>
/// `PublicApiTestFactory` + sozlangan Telegram bot tokeni. Token — SINOV qiymati, hech qanday
/// haqiqiy bot bilan bog'liq emas va hech qayerga yuborilmaydi: `TelegramLoginVerifier` undan
/// faqat `SHA256(bot_token)` maxfiy kalitini hisoblaydi (`CLAUDE.md` 4-qoida buzilmaydi —
/// bu sir emas, sinov konstantasi).
/// </summary>
public class TelegramApiTestFactory : PublicApiTestFactory
{
    /// <summary>Testlar imzo hisoblashda AYNAN shu qiymatdan foydalanadi (`TelegramLoginTestSupport`).</summary>
    public const string BotToken = "1234567890:TEST-ONLY-BOT-TOKEN-NOT-A-SECRET";

    protected override IReadOnlyDictionary<string, string?> AdditionalConfiguration { get; } =
        new Dictionary<string, string?>
        {
            ["Telegram:BotToken"] = BotToken,
        };
}

/// <summary>
/// Telegram bot tokeni BERILMAGAN host — `503 TELEGRAM_AUTH_NOT_CONFIGURED` yo'lini sinash
/// uchun. `PublicApiTestFactory`ning o'zi ham tokensiz, lekin alohida sinf ATAYLAB: niyat
/// ("bu test aynan sozlanmagan holatni tekshiradi") kod nomida ko'rinib tursin.
/// </summary>
public sealed class TelegramNotConfiguredApiTestFactory : PublicApiTestFactory
{
}

/// <summary>
/// Telegram sozlangan + GLOBAL natija rubilnigi O'CHIRILGAN (`App:ShowResultToStudent=false`).
/// Kill-switch makon bayrog'idan (`School.ShowResultToStudent = true`) USTUN ekanini sinaydi.
/// </summary>
public sealed class ResultKillSwitchOffApiTestFactory : PublicApiTestFactory
{
    protected override IReadOnlyDictionary<string, string?> AdditionalConfiguration { get; } =
        new Dictionary<string, string?>
        {
            ["App:ShowResultToStudent"] = "false",
        };
}

/// <summary>
/// Telegram sozlangan, LEKIN bazada hech qanday dastur yo'q — "makonga dastur biriktirilmagan"
/// (`409 NO_PROGRAM_AVAILABLE`) yo'lini sinash uchun. Alohida sinf/fixture: `TestDataFactory`
/// standart dasturni YARATIB QO'YADI, shu sabab bu bazada `CreatePublishedTestAsync`
/// chaqirilmasligi kerak.
/// </summary>
public sealed class TelegramNoProgramApiTestFactory : TelegramApiTestFactory
{
}

/// <summary>`MeDeleteEndpointTests` uchun alohida host — kirish (rate limit) kvotasi ajratilsin.</summary>
public sealed class PublicUserDeleteApiTestFactory : TelegramApiTestFactory
{
}
