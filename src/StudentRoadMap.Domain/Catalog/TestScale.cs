using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// Superadmin `Custom` anketasining shkalasi — `SUM` strategiyasi uchun (`docs/03` §6.1,
/// `docs/04` §2.7). Tizim metodikalarida `TestScale` yozuvi bo'lmaydi — ularning shkalalari
/// (`EI`, `O`, `R` va h.k.) kod ichida qattiq yozilgan strategiyalarga (`Mbti16Strategy` va
/// h.k.) tegishli, admin CRUD orqali boshqarilmaydi (`docs/07` §3.4: "Shkalalar — faqat
/// `Custom`").
/// </summary>
public sealed class TestScale : Entity
{
    public Guid TestDefinitionId { get; private set; }

    public string Code { get; private set; } = null!;

    public string NameUz { get; private set; } = null!;

    public string? DescriptionUz { get; private set; }

    public int DisplayOrder { get; private set; }

    /// <summary>Talqin oraliqlari (`docs/03` §6.1: `[{ "from":0, "to":33, "label":"Past" }, …]`) — nashr validatsiyasi (`docs/03` §6.3) 0–100 ni bo'shliqsiz/ustma-ustsiz qoplashini tekshiradi.</summary>
    public IReadOnlyList<InterpretationBand> InterpretationBands { get; private set; } = [];

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private TestScale()
    {
    }

    private TestScale(
        Guid id,
        Guid testDefinitionId,
        string code,
        string nameUz,
        string? descriptionUz,
        int displayOrder,
        IReadOnlyList<InterpretationBand> interpretationBands)
        : base(id)
    {
        TestDefinitionId = testDefinitionId;
        Code = code;
        NameUz = nameUz;
        DescriptionUz = descriptionUz;
        DisplayOrder = displayOrder;
        InterpretationBands = interpretationBands;
    }

    public static TestScale Create(
        Guid id,
        Guid testDefinitionId,
        string code,
        string nameUz,
        int displayOrder,
        IReadOnlyList<InterpretationBand>? interpretationBands = null,
        string? descriptionUz = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Shkala kodi bo'sh bo'lishi mumkin emas.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(nameUz))
        {
            throw new ArgumentException("Shkala nomi bo'sh bo'lishi mumkin emas.", nameof(nameUz));
        }

        return new TestScale(id, testDefinitionId, code.Trim(), nameUz, descriptionUz, displayOrder, interpretationBands ?? []);
    }

    public void UpdateMetadata(string nameUz, string? descriptionUz, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(nameUz))
        {
            throw new ArgumentException("Shkala nomi bo'sh bo'lishi mumkin emas.", nameof(nameUz));
        }

        NameUz = nameUz;
        DescriptionUz = descriptionUz;
        DisplayOrder = displayOrder;
    }

    public void UpdateInterpretationBands(IReadOnlyList<InterpretationBand> interpretationBands)
    {
        ArgumentNullException.ThrowIfNull(interpretationBands);
        InterpretationBands = interpretationBands;
    }
}
