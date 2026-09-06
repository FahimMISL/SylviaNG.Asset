using MediatR;
using RMS.Domain.Enums;

namespace RMS.Application.Features.NotificationTemplates.Commands.UpdateTemplate;

public record UpdateTemplateCommand(
    NotificationEventType EventType, string EmailSubject, string EmailBody, string InAppMessage) : IRequest;
