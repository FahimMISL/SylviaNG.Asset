using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;

namespace RMS.Application.Features.Notifications.Queries.GetUnreadCount;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, int>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUser;

    public GetUnreadCountQueryHandler(INotificationRepository notificationRepository, ICurrentUserService currentUser)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
    }

    public Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        return _notificationRepository.GetUnreadCountAsync(userId, cancellationToken);
    }
}
