using MediatR;
using RMS.Application.Features.Notifications.DTOs;

namespace RMS.Application.Features.Notifications.Queries.GetMyPreferences;

public record GetMyPreferencesQuery : IRequest<List<NotificationPreferenceDto>>;
