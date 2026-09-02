namespace StudentRoadMap.Domain.Catalog;

/// <summary>`AssessmentProgram.Kind` — `docs/06-arxitektura.md` 8-bo'lim (2026-09-02 qaror), `prompts/34` A-band.</summary>
public enum ProgramKind
{
    /// <summary>Seed'dan kelgan tizim dasturi (`PERSONALITY_PROFILE`) — qulflangan (`IsSystem = true`).</summary>
    System = 1,

    /// <summary>Superadmin yaratgan dastur.</summary>
    Custom = 2,
}
