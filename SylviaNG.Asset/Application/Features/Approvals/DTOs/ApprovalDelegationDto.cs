using RMS.Domain.Entities;

namespace RMS.Application.Features.Approvals.DTOs;

public record ApprovalDelegationDto(
    Guid Id, Guid DelegatorUserId, string DelegatorName, Guid DelegateUserId, string DelegateName,
    DateOnly StartDate, DateOnly EndDate, string Reason, bool IsRevoked, bool IsCurrentlyActive)
{
    public static ApprovalDelegationDto FromEntity(ApprovalDelegation d) => new(
        d.Id, d.DelegatorUserId, d.DelegatorUser?.FullName ?? string.Empty,
        d.DelegateUserId, d.DelegateUser?.FullName ?? string.Empty,
        d.StartDate, d.EndDate, d.Reason, d.IsRevoked, d.IsActiveOn(DateOnly.FromDateTime(DateTime.UtcNow)));
}
