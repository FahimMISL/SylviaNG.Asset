using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.Notifications.Commands.MarkAsRead;

public class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public MarkAsReadCommandHandler(INotificationRepository notificationRepository, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();

        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.NotificationId);

        if (notification.RecipientUserId != userId)
        {
            throw new ForbiddenException();
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
