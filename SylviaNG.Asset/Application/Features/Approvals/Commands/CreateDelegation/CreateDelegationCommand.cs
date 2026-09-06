using MediatR;
using RMS.Application.Features.Approvals.DTOs;

namespace RMS.Application.Features.Approvals.Commands.CreateDelegation;

/// <summary>US-048 out-of-office delegation. OnBehalfOfUserId lets a SystemAdmin configure this for
/// someone else - null means "for myself".</summary>
public record CreateDelegationCommand(
    Guid DelegateUserId, DateOnly StartDate, DateOnly EndDate, string Reason, Guid? OnBehalfOfUserId) : IRequest<ApprovalDelegationDto>;
