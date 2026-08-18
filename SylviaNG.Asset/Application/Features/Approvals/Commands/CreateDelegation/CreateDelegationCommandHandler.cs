using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Approvals.DTOs;
using RMS.Application.Features.Approvals.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Approvals.Commands.CreateDelegation;

public class CreateDelegationCommandHandler : IRequestHandler<CreateDelegationCommand, ApprovalDelegationDto>
{
    private readonly IApprovalDelegationRepository _delegationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDelegationCommandHandler(
        IApprovalDelegationRepository delegationRepository, IUserRepository userRepository,
        ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _delegationRepository = delegationRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApprovalDelegationDto> Handle(CreateDelegationCommand request, CancellationToken cancellationToken)
    {
        CommentValidation.EnsureValid(request.Reason);

        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var delegatorUserId = request.OnBehalfOfUserId ?? userId;
        if (request.OnBehalfOfUserId.HasValue && request.OnBehalfOfUserId.Value != userId && !_currentUser.IsInRole(UserRole.SystemAdmin))
        {
            throw new ForbiddenException("Only a System Administrator can configure a delegation on someone else's behalf.");
        }

        if (delegatorUserId == request.DelegateUserId)
        {
            throw new ConflictException("You cannot delegate to yourself.");
        }

        var delegateUser = await _userRepository.GetByIdAsync(request.DelegateUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.DelegateUserId);
        if (!delegateUser.IsActive || delegateUser.CompanyId != companyId)
        {
            throw new ConflictException("The selected delegate must be an active user in your company.");
        }

        var delegation = new ApprovalDelegation
        {
            CompanyId = companyId,
            DelegatorUserId = delegatorUserId,
            DelegateUserId = request.DelegateUserId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason,
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _delegationRepository.Add(delegation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("ApprovalDelegationCreated", nameof(ApprovalDelegation), delegation.Id,
            $"Delegator={delegatorUserId}, Delegate={request.DelegateUserId}, {request.StartDate:yyyy-MM-dd}..{request.EndDate:yyyy-MM-dd}", cancellationToken);

        var refreshed = await _delegationRepository.GetByIdAsync(delegation.Id, cancellationToken) ?? delegation;
        return ApprovalDelegationDto.FromEntity(refreshed);
    }
}
