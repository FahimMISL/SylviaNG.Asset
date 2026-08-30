namespace RMS.Application.Common;

/// <summary>
/// Feature 11: the one shared shape for a server-paginated result set, mirroring the
/// (Items, TotalCount) tuple NotificationRepository.GetForUserAsync already used - the only real
/// pagination precedent in this codebase before this feature. Used by both requisition search and
/// the newly-paginated audit log query.
/// </summary>
public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);
