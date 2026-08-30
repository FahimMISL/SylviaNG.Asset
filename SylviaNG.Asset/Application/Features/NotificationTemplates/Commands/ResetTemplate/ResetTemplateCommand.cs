using MediatR;
using RMS.Domain.Enums;

namespace RMS.Application.Features.NotificationTemplates.Commands.ResetTemplate;

public record ResetTemplateCommand(NotificationEventType EventType) : IRequest;
