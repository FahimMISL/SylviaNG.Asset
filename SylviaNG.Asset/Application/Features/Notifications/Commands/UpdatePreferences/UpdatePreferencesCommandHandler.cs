using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Notifications.Commands.UpdatePreferences;

public class UpdatePreferencesCommandHandler : IRequestHandler<UpdatePreferencesCommand>
{
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePreferencesCommandHandler(
        INotificationPreferenceRepository preferenceRepository, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _preferenceRepository = preferenceRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdatePreferencesCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();

        foreach (var input in request.Preferences)
        {
            // Critical types (see NotificationEventTypeCatalog) can never be disabled - silently
            // ignored here rather than erroring, so a client that doesn't yet know the current
            // critical set can't accidentally fail an otherwise-valid bulk update. Skipped
            // unconditionally (not just on a disable attempt) so no row is ever written for a
            // critical type, matching NotificationPreference's own "never written here" remarks -
            // the frontend always submits critical types as enabled=true, and without this a stray
            // (harmless but pointless) row would get created for them every time preferences are saved.
            if (NotificationEventTypeCatalog.IsCritical(input.EventType))
            {
                continue;
            }

            await _preferenceRepository.UpsertAsync(userId, input.EventType, input.IsEnabled, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
