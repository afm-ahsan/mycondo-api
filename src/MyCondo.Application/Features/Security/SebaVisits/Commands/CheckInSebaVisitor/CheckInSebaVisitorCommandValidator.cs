using FluentValidation;
using MyCondo.Application.Common.Validation;

namespace MyCondo.Application.Features.Security.SebaVisits.Commands.CheckInSebaVisitor;

public sealed class CheckInSebaVisitorCommandValidator : AbstractValidator<CheckInSebaVisitorCommand>
{
    public CheckInSebaVisitorCommandValidator()
    {
        RuleFor(x => x.VisitorFullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.VisitorPhone).MustBeValidBangladeshMobileNumber();
        RuleFor(x => x.Organization).MaximumLength(200);
        RuleFor(x => x.DepartmentOrEmployeeToMeet).MaximumLength(200);
        RuleFor(x => x.TokenNumber).MaximumLength(40);
        RuleFor(x => x.RelatedReferenceType).MaximumLength(40);
        RuleFor(x => x.EntryGateId).NotEmpty();
    }
}
