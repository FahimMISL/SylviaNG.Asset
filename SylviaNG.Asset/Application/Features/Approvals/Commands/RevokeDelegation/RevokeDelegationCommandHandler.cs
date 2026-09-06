using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Approvals.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Approvals.Commands.RevokeDelegation;

public class RevokeDelegationCommandHandler : IRequestHandler<RevokeDelegationCommand>
{
    private readonly IApprovalDelegationRepository _delegationRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeDelegationCommandHandler(
        IApprovalDelegationRepository delegationRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _delegationRepository = delegationRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RevokeDelegationCommand request, CancellationToken cancellationToken)
    {
        CommentValidation.EnsureValid(request.Reason);

        var userId = _currentUser.UserId ?? throw new ForbiddenException();

        var delegation = await _delegationRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ApprovalDelegation), request.Id);

        if (delegation.DelegatorUserId != userId && delegation.CreatedByUserId != userId && !_currentUser.IsInRole(UserRole.SystemAdmin))
        {
            throw new ForbiddenException();
        }

        if (delegation.IsRevoked)
        {
            throw new ConflictException("This delegation has already been revoked.");
        }

        delegation.IsRevoked = true;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("ApprovalDelegationRevoked", nameof(ApprovalDelegation), delegation.Id, request.Reason, cancellationToken);
    }
}
