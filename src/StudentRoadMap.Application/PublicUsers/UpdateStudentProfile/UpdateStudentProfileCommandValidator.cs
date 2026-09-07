using FluentValidation;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.PublicUsers.Common;

namespace StudentRoadMap.Application.PublicUsers.UpdateStudentProfile;

/// <summary>
/// FAQAT format (`PublicProfileFormatRules`) — `StartPublicSessionCommandValidator` bilan bir
/// manbadan. Majburiylik `Student` holatiga bog'liq va handlerda tekshiriladi
/// (`PublicStudentProfile.RequireFields`). `public` — `AssemblyScanner` uchun.
/// </summary>
public sealed class UpdateStudentProfileCommandValidator : AbstractValidator<UpdateStudentProfileCommand>
{
    public UpdateStudentProfileCommandValidator(IDateTime dateTime)
    {
        this.AddPublicProfileFormatRules(dateTime);
    }
}
