using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Notifications.DTOs;
using RMS.Application.Interfaces;

namespace RMS.Application.Features.Notifications.Queries.GetMyNotifications;

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, NotificationPageDto>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyNotificationsQueryHandler(INotificationRepository notificationRepository, ICurrentUserService currentUser)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
    }

    public async Task<NotificationPageDto> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();

        var (items, totalCount) = await _notificationRepository.GetForUserAsync(
            userId, request.UnreadOnly, request.Skip, request.Take, cancellationToken);

        return new NotificationPageDto(items.Select(NotificationEntryDto.FromEntity).ToList(), totalCount);
    }
}
