using MediatR;

namespace RMS.Application.Features.Notifications.Queries.GetUnreadCount;

public record GetUnreadCountQuery : IRequest<int>;
