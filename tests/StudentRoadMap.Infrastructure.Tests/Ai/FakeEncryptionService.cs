using System.Text;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Tests.Ai;

/// <summary>
/// `AesEncryptionService` o'rniga — sinovda haqiqiy `Security:EncryptionKey` konfiguratsiyasi
/// kerak bo'lmasligi uchun (`AiProviderResolverTests` faqat shifrlash/deshifrlash **chaqirilishini**
/// tekshiradi, algoritmni emas — algoritm P16'da, `AesEncryptionServiceTests`da qamrab olingan).
/// Base64 — qaytariladigan, lekin xom matn emas (adashtirib bo'lmaydi).
/// </summary>
internal sealed class FakeEncryptionService : IEncryptionService
{
    public string Encrypt(string plainText) => Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));

    public string Decrypt(string cipherText) => Encoding.UTF8.GetString(Convert.FromBase64String(cipherText));
}
