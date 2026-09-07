namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// Maktab kodi (`School.EntryCode`, `Domain.Schools.SchoolEntryCode` formati) generatori —
/// `ITokenGenerator` bilan bir xil naqsh: tasodifiylik Infrastructure'da
/// (`Security.EntryCodeGenerator`, `RandomNumberGenerator`), domen faqat formatni tekshiradi.
/// </summary>
public interface IEntryCodeGenerator
{
    /// <summary>`SchoolEntryCode.Length` belgili, `SchoolEntryCode.Alphabet` dan tuzilgan yangi kod (defissiz saqlash shakli).</summary>
    string Generate();
}
