using System.Security.Cryptography;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Infrastructure.Security;

/// <summary>
/// Maktab kodi generatori — `RandomNumberGenerator.GetInt32` (kriptografik, alifbo bo'yicha
/// TEKIS taqsimot, modul og'ishi yo'q). Format/alifbo — `Domain.Schools.SchoolEntryCode`
/// (domen faqat tekshiradi, tasodifiylik shu yerda — `Identity.TokenGenerator` bilan bir xil naqsh).
/// </summary>
internal sealed class EntryCodeGenerator : IEntryCodeGenerator
{
    public string Generate()
    {
        var chars = new char[SchoolEntryCode.Length];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = SchoolEntryCode.Alphabet[RandomNumberGenerator.GetInt32(SchoolEntryCode.Alphabet.Length)];
        }

        return new string(chars);
    }
}
