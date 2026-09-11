namespace StudentRoadMap.Domain.Settings;

/// <summary>
/// `RegistrationCustomField` (`SingleChoice`/`MultiChoice`) ning bitta tanlov varianti —
/// `AnswerOption` bilan bir xil naqsh, lekin savol kataloglidan mustaqil (ro'yxatdan o'tish
/// formasi savollar bilan bog'liq emas).
/// </summary>
public sealed record RegistrationCustomFieldOption(string TextUz, string Value, int Order);
