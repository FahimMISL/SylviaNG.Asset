using MediatR;
using RMS.Application.Features.Users.DTOs;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Users.Queries.GetUsers;

/// <summary>Company-scoped user directory (via ICurrentUserService), optionally filtered by role -
/// the picker source the frontend needs for approvers/fallback approvers/escalation contacts/
/// delegates instead of raw GUID text inputs.</summary>
public record GetUsersQuery(UserRole? Role) : IRequest<List<UserSummaryDto>>;
