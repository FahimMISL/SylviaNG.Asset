using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Users.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserSummaryDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserCommandHandler(
        IUserRepository userRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserSummaryDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);
        if (user.CompanyId != companyId)
        {
            throw new ForbiddenException();
        }

        if (await _userRepository.ExistsByEmailAsync(companyId, request.Email, user.Id, cancellationToken))
        {
            throw new ConflictException($"A user with email '{request.Email}' already exists.");
        }

        var oldRole = user.Role;

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.Role = request.Role;
        user.Grade = request.Grade;
        user.Designation = request.Designation;
        user.EmploymentType = request.EmploymentType;
        user.Department = request.Department;
        user.Location = request.Location;
        user.UpdatedByUserId = _currentUser.UserId;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var details = oldRole == request.Role
            ? $"FullName={user.FullName}"
            : $"FullName={user.FullName}; OldRole={oldRole}; NewRole={request.Role}";
        await _auditLogger.LogAsync("UserUpdated", nameof(User), user.Id, details, cancellationToken, target: user.FullName);

        return UserSummaryDto.FromEntity(user);
    }
}
