using MediatR;

namespace RMS.Application.Features.Notifications.Commands.MarkAsRead;

public record MarkAsReadCommand(Guid NotificationId) : IRequest;
