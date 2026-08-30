using MediatR;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Notifications.Commands.UpdatePreferences;

public record NotificationPreferenceInput(NotificationEventType EventType, bool IsEnabled);

public record UpdatePreferencesCommand(List<NotificationPreferenceInput> Preferences) : IRequest;
