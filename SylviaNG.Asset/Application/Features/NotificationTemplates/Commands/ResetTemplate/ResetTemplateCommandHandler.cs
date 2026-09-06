using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.NotificationTemplates.Commands.ResetTemplate;

public class ResetTemplateCommandHandler : IRequestHandler<ResetTemplateCommand>
{
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ResetTemplateCommandHandler(
        INotificationTemplateRepository templateRepository, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _templateRepository = templateRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ResetTemplateCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole(UserRole.SystemAdmin))
        {
            throw new ForbiddenException();
        }

        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var existing = await _templateRepository.GetByEventTypeAsync(companyId, request.EventType, cancellationToken);
        if (existing is null)
        {
            // Already default - nothing to do.
            return;
        }

        _templateRepository.Remove(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
