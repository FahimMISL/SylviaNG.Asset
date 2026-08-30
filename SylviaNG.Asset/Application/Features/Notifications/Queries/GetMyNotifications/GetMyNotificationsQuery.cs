using MediatR;
using RMS.Application.Features.Notifications.DTOs;

namespace RMS.Application.Features.Notifications.Queries.GetMyNotifications;

public record GetMyNotificationsQuery(bool UnreadOnly, int Skip, int Take) : IRequest<NotificationPageDto>;
