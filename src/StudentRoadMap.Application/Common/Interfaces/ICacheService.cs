namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// Sessiyaga bog'liq bo'lmagan (umumiy katalog) ma'lumotlarni keshlash abstraksiyasi —
/// `Infrastructure`da `IMemoryCache` bilan amalga oshiriladi (`Application` EF Core'siz
/// bo'lgani kabi keshlash kutubxonasiga ham bevosita bog'lanmaydi, `docs/06-arxitektura.md`
/// 3-bo'lim ruhida). `prompts/11`: `TestDefinition`/`Question` ro'yxati 10 daqiqa keshlanadi,
/// lekin o'quvchiga xos qiymatlar (masalan, `currentValue`) BU ORQALI HECH QACHON keshlanmaydi.
/// </summary>
public interface ICacheService
{
    /// <summary>Kalit bo'yicha oldindan saqlangan qiymatni qaytaradi (topilmasa — `false`).</summary>
    bool TryGet<T>(string key, out T value)
        where T : class;

    /// <summary>Qiymatni berilgan muddatga (`duration`) keshga yozadi.</summary>
    void Set<T>(string key, T value, TimeSpan duration)
        where T : class;

    /// <summary>Kalitni keshdan olib tashlaydi (invalidatsiya uchun).</summary>
    void Remove(string key);
}
