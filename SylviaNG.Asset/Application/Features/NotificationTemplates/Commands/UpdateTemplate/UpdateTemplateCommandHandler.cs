using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.NotificationTemplates.Commands.UpdateTemplate;

public class UpdateTemplateCommandHandler : IRequestHandler<UpdateTemplateCommand>
{
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTemplateCommandHandler(
        INotificationTemplateRepository templateRepository, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _templateRepository = templateRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateTemplateCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole(UserRole.SystemAdmin))
        {
            throw new ForbiddenException();
        }

        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();
        var userId = _currentUser.UserId ?? throw new ForbiddenException();

        var existing = await _templateRepository.GetByEventTypeAsync(companyId, request.EventType, cancellationToken);
        if (existing is not null)
        {
            existing.EmailSubject = request.EmailSubject;
            existing.EmailBody = request.EmailBody;
            existing.InAppMessage = request.InAppMessage;
            existing.UpdatedAtUtc = DateTime.UtcNow;
            existing.UpdatedByUserId = userId;
        }
        else
        {
            _templateRepository.Add(new NotificationTemplate
            {
                CompanyId = companyId,
                EventType = request.EventType,
                EmailSubject = request.EmailSubject,
                EmailBody = request.EmailBody,
                InAppMessage = request.InAppMessage,
                UpdatedByUserId = userId,
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
