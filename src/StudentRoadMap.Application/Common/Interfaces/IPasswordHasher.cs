namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// Admin foydalanuvchi parolini xeshlash abstraksiyasi (`docs/08-auth-va-xavfsizlik.md`).
/// `Infrastructure` da PBKDF2 bilan amalga oshiriladi. Parol hech qachon ochiq holda
/// saqlanmaydi — faqat xesh `AdminUser.PasswordHash` maydonida (`CLAUDE.md` 4-qoida).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Ochiq parolni xeshlaydi — natija tuz va parametrlarni o'z ichiga oladi.</summary>
    string Hash(string password);

    /// <summary>Ochiq parolni saqlangan xesh bilan solishtiradi.</summary>
    bool Verify(string password, string hash);
}
