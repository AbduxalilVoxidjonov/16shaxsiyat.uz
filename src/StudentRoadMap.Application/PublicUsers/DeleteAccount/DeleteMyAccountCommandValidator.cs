using FluentValidation;
using StudentRoadMap.Domain.PublicUsers;

namespace StudentRoadMap.Application.PublicUsers.DeleteAccount;

/// <summary>
/// `DELETE /api/me` tanasi (egasining 2026-09-08 talabi): sabab MAJBURIY va ro'yxatdagi
/// qiymatlardan biri bo'lishi shart (noma'lum satr `PublicUserDeletionReason?`ga
/// deserializatsiya bosqichida allaqachon `400`ga uchraydi — bu yerda faqat "berilmagan"
/// holat tekshiriladi). `Other` tanlanganda izoh bo'sh bo'lmasligi kerak — "boshqa sabab"
/// erkin matnsiz ma'nosiz. `public` — `AssemblyScanner` uchun.
/// </summary>
public sealed class DeleteMyAccountCommandValidator : AbstractValidator<DeleteMyAccountCommand>
{
    public const int MaxCommentLength = PublicUser.MaxDeletionCommentLength;

    public DeleteMyAccountCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotNull()
            .WithMessage("O'chirish sababi tanlanishi shart.");

        RuleFor(x => x.Comment)
            .MaximumLength(MaxCommentLength)
            .WithMessage($"Izoh {MaxCommentLength} belgidan oshmasligi kerak.");

        RuleFor(x => x.Comment)
            .Must(comment => !string.IsNullOrWhiteSpace(comment))
            .When(x => x.Reason == PublicUserDeletionReason.Other)
            .WithMessage("'Boshqa sabab' tanlanganda izoh yozilishi shart.");
    }
}
