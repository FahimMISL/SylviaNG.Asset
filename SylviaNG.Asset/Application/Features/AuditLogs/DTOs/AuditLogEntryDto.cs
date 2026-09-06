namespace RMS.Application.Features.AuditLogs.DTOs;

public record AuditLogEntryDto(
    Guid Id,
    DateTime TimestampUtc,
    Guid? ActorUserId,
    string ActorName,
    string? ActorRole,
    bool IsSystemGenerated,
    string ActionType,
    string EntityName,
    Guid EntityId,
    string? Details,
    string? IpAddress)
{
    public static AuditLogEntryDto FromEntity(RMS.Domain.Entities.AuditLog entity) => new(
        entity.Id,
        entity.TimestampUtc,
        entity.ActorUserId,
        entity.ActorName,
        entity.ActorRole,
        entity.ActorUserId == null,
        entity.ActionType,
        entity.EntityName,
        entity.EntityId,
        entity.Details,
        entity.IpAddress);
}
