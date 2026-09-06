using FluentValidation;

namespace RMS.Application.Features.Approvals.Commands.SendBackApproval;

public class SendBackApprovalCommandValidator : AbstractValidator<SendBackApprovalCommand>
{
    public SendBackApprovalCommandValidator()
    {
        RuleFor(c => c.ApprovalId).NotEmpty();
    }
}
