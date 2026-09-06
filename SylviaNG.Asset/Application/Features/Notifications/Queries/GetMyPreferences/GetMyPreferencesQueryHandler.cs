using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Notifications.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Notifications.Queries.GetMyPreferences;

public class GetMyPreferencesQueryHandler : IRequestHandler<GetMyPreferencesQuery, List<NotificationPreferenceDto>>
{
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyPreferencesQueryHandler(INotificationPreferenceRepository preferenceRepository, ICurrentUserService currentUser)
    {
        _preferenceRepository = preferenceRepository;
        _currentUser = currentUser;
    }

    public async Task<List<NotificationPreferenceDto>> Handle(GetMyPreferencesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();

        var existing = (await _preferenceRepository.GetAllForUserAsync(userId, cancellationToken))
            .ToDictionary(p => p.EventType, p => p.IsEnabled);

        // Every event type is listed, even ones with no row - "no row" means enabled by default (see
        // NotificationPreference's remarks), and critical types always show enabled/locked regardless
        // of anything that might exist in the table.
        return NotificationEventTypeCatalog.AllTypes.Select(eventType =>
        {
            var isCritical = NotificationEventTypeCatalog.IsCritical(eventType);
            var isEnabled = isCritical || !existing.TryGetValue(eventType, out var enabled) || enabled;
            return new NotificationPreferenceDto(eventType, NotificationEventTypeCatalog.LabelOf(eventType), isCritical, isEnabled);
        }).ToList();
    }
}
