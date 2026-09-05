namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// Telegram Login Widget qaytargan ma'lumotning HAQIQIYLIGINI tekshiradi (Telegram rasmiy
/// algoritmi, `docs/08-auth-va-xavfsizlik.md` 2a-bo'lim):
///
/// <code>
/// data_check_string = "\n" bilan birlashtirilgan, kalit bo'yicha ALIFBO tartibida
///                     saralangan "key=value" qatorlari (`hash` maydonisiz)
/// secret_key        = SHA256(bot_token)
/// tekshiruv         = HMAC_SHA256(data_check_string, secret_key) hex == hash
/// </code>
///
/// Solishtirish DOIMIY vaqtda (`CryptographicOperations.FixedTimeEquals`) bajariladi —
/// baytma-bayt erta chiqadigan solishtiruv xesh qiymatini bosqichma-bosqich tiklashga
/// (timing oracle) imkon berardi.
///
/// Bot tokeni SIR (`Telegram:BotToken`, env `Telegram__BotToken`) — shu sabab implementatsiya
/// `Infrastructure` qatlamida (`CLAUDE.md` 4-qoida: sirlar kodda/`appsettings.json`da yo'q).
/// `auth_date` YANGILIGI bu yerda TEKSHIRILMAYDI — u vaqtga bog'liq (`IDateTime`) va biznes
/// qarori, shu sabab handler'da (`TelegramLoginCommandHandler.MaxAuthDateAge`).
/// </summary>
public interface ITelegramLoginVerifier
{
    /// <summary>
    /// `Telegram:BotToken` berilganmi. `false` bo'lsa handler `503 TELEGRAM_AUTH_NOT_CONFIGURED`
    /// qaytaradi — sozlamasiz muhitda har qanday `hash` "noto'g'ri" ko'rinib, mijozga
    /// chalg'ituvchi `401` berilishining oldini oladi.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// <paramref name="fields"/> — Telegram yuborgan BARCHA maydon (`hash`dan tashqari),
    /// xom (o'zgartirilmagan) qiymatlari bilan. Kalitlarni saralash va birlashtirish
    /// implementatsiya zimmasida.
    /// </summary>
    bool Verify(IReadOnlyDictionary<string, string> fields, string hash);
}
