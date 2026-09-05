using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Users.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.Users.Commands.SetUserActiveState;

public class SetUserActiveStateCommandHandler : IRequestHandler<SetUserActiveStateCommand, UserSummaryDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public SetUserActiveStateCommandHandler(
        IUserRepository userRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserSummaryDto> Handle(SetUserActiveStateCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);
        if (user.CompanyId != companyId)
        {
            throw new ForbiddenException();
        }

        if (user.IsActive == request.IsActive)
        {
            return UserSummaryDto.FromEntity(user);
        }

        user.IsActive = request.IsActive;
        user.UpdatedByUserId = _currentUser.UserId;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            request.IsActive ? "UserActivated" : "UserDeactivated", nameof(User), user.Id, null, cancellationToken, target: user.FullName);

        return UserSummaryDto.FromEntity(user);
    }
}
