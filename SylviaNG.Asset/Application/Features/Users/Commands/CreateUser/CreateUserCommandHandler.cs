using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Users.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserSummaryDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(
        IUserRepository userRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserSummaryDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        if (await _userRepository.ExistsByEmailAsync(companyId, request.Email, null, cancellationToken))
        {
            throw new ConflictException($"A user with email '{request.Email}' already exists.");
        }

        var user = new User
        {
            CompanyId = companyId,
            FullName = request.FullName,
            Email = request.Email,
            // Real login (Keycloak) isn't wired yet - see UsersController's [AllowAnonymous] remark.
            // PasswordHash has no real use until then; left empty like every seeded dev user.
            PasswordHash = string.Empty,
            Role = request.Role,
            IsActive = true,
            Grade = request.Grade,
            Designation = request.Designation,
            EmploymentType = request.EmploymentType,
            Department = request.Department,
            Location = request.Location,
            CreatedByUserId = _currentUser.UserId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _userRepository.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            "UserCreated", nameof(User), user.Id, $"FullName={user.FullName}; Role={user.Role}", cancellationToken, target: user.FullName);

        return UserSummaryDto.FromEntity(user);
    }
}
