using MediatR;
using RMS.Application.Features.Users.DTOs;

namespace RMS.Application.Features.Users.Queries.GetUserById;

/// <summary>Loads a single user for the admin edit form - the list endpoint already returns every
/// field an edit form needs, but a direct-by-id fetch avoids relying on the caller having the full
/// list already loaded (e.g. a deep link to /users/{id}/edit).</summary>
public record GetUserByIdQuery(Guid UserId) : IRequest<UserSummaryDto>;
