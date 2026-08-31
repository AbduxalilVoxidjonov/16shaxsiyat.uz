using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Security;

/// <summary>
/// AES-GCM bilan simmetrik shifrlash — AI provayder API kaliti va TOTP sirini saqlash uchun
/// (`docs/06-arxitektura.md` 7-bo'limi: `Security:EncryptionKey`, base64, 32 bayt).
/// Kalit sir sifatida env/user-secrets orqali keladi — kodda yo'q (`CLAUDE.md` 4-qoida).
/// </summary>
internal sealed class AesEncryptionService : IEncryptionService
{
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly byte[] _key;

    public AesEncryptionService(IConfiguration configuration)
    {
        var keyBase64 = configuration["Security:EncryptionKey"];
        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            throw new InvalidOperationException(
                "'Security:EncryptionKey' sozlamasi topilmadi. Env o'zgaruvchi yoki user-secrets orqali 32 baytli base64 kalit bering.");
        }

        _key = Convert.FromBase64String(keyBase64);
        if (_key.Length != 32)
        {
            throw new InvalidOperationException("'Security:EncryptionKey' aynan 32 bayt (base64) bo'lishi kerak (AES-256).");
        }
    }

    public string Encrypt(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSizeBytes];

        using (var aesGcm = new AesGcm(_key, TagSizeBytes))
        {
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        // Format: nonce || tag || cipherText — base64 sifatida saqlanadi.
        var result = new byte[NonceSizeBytes + TagSizeBytes + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSizeBytes);
        Buffer.BlockCopy(tag, 0, result, NonceSizeBytes, TagSizeBytes);
        Buffer.BlockCopy(cipherBytes, 0, result, NonceSizeBytes + TagSizeBytes, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        ArgumentNullException.ThrowIfNull(cipherText);

        var payload = Convert.FromBase64String(cipherText);
        if (payload.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new ArgumentException("Shifrlangan matn formati noto'g'ri.", nameof(cipherText));
        }

        var nonce = payload[..NonceSizeBytes];
        var tag = payload[NonceSizeBytes..(NonceSizeBytes + TagSizeBytes)];
        var cipherBytes = payload[(NonceSizeBytes + TagSizeBytes)..];
        var plainBytes = new byte[cipherBytes.Length];

        using (var aesGcm = new AesGcm(_key, TagSizeBytes))
        {
            aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }

        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }
}
