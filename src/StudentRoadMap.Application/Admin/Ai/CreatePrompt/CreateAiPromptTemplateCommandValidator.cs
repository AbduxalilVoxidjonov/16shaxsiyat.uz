using System.Text.Json;
using FluentValidation;

namespace StudentRoadMap.Application.Admin.Ai.CreatePrompt;

public sealed class CreateAiPromptTemplateCommandValidator : AbstractValidator<CreateAiPromptTemplateCommand>
{
    public CreateAiPromptTemplateCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Version).NotEmpty().MaximumLength(20);
        RuleFor(x => x.SystemText).NotEmpty();
        RuleFor(x => x.UserText).NotEmpty()
            .Must(t => t.Contains("{ANALYSIS_INPUT_JSON}", StringComparison.Ordinal))
            .WithMessage("Foydalanuvchi matnida '{ANALYSIS_INPUT_JSON}' belgisi bo'lishi shart.");
        RuleFor(x => x.JsonSchema).NotEmpty().Must(BeValidJson).WithMessage("JSON sxema to'g'ri JSON bo'lishi kerak.");
    }

    private static bool BeValidJson(string json)
    {
        try
        {
            using var _ = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
