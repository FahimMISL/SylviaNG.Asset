using FluentValidation;

namespace RMS.Application.Features.Approvals.Commands.RequestClarification;

public class RequestClarificationCommandValidator : AbstractValidator<RequestClarificationCommand>
{
    public RequestClarificationCommandValidator()
    {
        RuleFor(c => c.ApprovalId).NotEmpty();
    }
}
