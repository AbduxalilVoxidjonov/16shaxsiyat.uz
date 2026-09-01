using Microsoft.Extensions.Caching.Memory;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Common;

/// <summary>
/// `ICacheService`ning `IMemoryCache` asosidagi amalga oshirilishi (`prompts/11`). Bitta
/// jarayon ichida (`AddSingleton`) baham ko'riladi — bir nechta Kestrel so'rovi orasida
/// keshni almashtiradi.
/// </summary>
public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool TryGet<T>(string key, out T value)
        where T : class
    {
        if (_cache.TryGetValue(key, out var raw) && raw is T typed)
        {
            value = typed;
            return true;
        }

        value = null!;
        return false;
    }

    public void Set<T>(string key, T value, TimeSpan duration)
        where T : class
    {
        _cache.Set(key, value, duration);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
    }
}
