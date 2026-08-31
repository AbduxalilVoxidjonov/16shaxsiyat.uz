namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// Nozik ma'lumotlarni (AI provayder API kaliti, TOTP siri) simmetrik shifrlash abstraksiyasi.
/// `Infrastructure` da AES bilan amalga oshiriladi (`Security/AesEncryptionService`).
/// Kalit sirlar kabi konfiguratsiyadan olinadi — kodda yo'q (`CLAUDE.md` 4-qoida).
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plainText);

    string Decrypt(string cipherText);
}
