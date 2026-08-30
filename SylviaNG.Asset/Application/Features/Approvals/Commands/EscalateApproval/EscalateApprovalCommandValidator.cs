using FluentValidation;

namespace RMS.Application.Features.Approvals.Commands.EscalateApproval;

public class EscalateApprovalCommandValidator : AbstractValidator<EscalateApprovalCommand>
{
    public EscalateApprovalCommandValidator()
    {
        RuleFor(c => c.ApprovalId).NotEmpty();
    }
}
