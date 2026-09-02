using FluentValidation;

namespace StudentRoadMap.Application.Admin.Ai.UpdateProvider;

/// <summary>`public` — `AssemblyScanner` faqat public validatorlarni ko'radi (`docs/06` qarorlar jurnali, P10).</summary>
public sealed class UpdateAiProviderCommandValidator : AbstractValidator<UpdateAiProviderCommand>
{
    public UpdateAiProviderCommandValidator()
    {
        RuleFor(x => x.Model).NotEmpty().MaximumLength(80);
        RuleFor(x => x.MaxOutputTokens).GreaterThan(0).LessThanOrEqualTo(1_000_000);
        // `docs/09` 10-bo'lim: standart 0.4 — 0..2 oralig'i providerlarning odatiy diapazoni.
        RuleFor(x => x.Temperature).InclusiveBetween(0m, 2m);
        RuleFor(x => x.FallbackOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BaseUrl).MaximumLength(200);
    }
}
