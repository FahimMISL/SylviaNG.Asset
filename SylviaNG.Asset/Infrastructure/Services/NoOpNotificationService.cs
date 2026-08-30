using RMS.Application.Interfaces;

namespace RMS.Infrastructure.Services;

/// <summary>No-op implementation - see INotificationService's remarks. Logging only, no actual delivery.</summary>
public class NoOpNotificationService : INotificationService
{
    private readonly ILogger<NoOpNotificationService> _logger;

    public NoOpNotificationService(ILogger<NoOpNotificationService> logger)
    {
        _logger = logger;
    }

    public Task NotifyAsync(Guid userId, string subject, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[NoOpNotificationService] To={UserId} Subject={Subject} Message={Message}", userId, subject, message);
        return Task.CompletedTask;
    }
}
