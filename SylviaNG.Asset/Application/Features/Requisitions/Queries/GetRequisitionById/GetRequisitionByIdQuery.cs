using MediatR;
using RMS.Application.Features.Requisitions.DTOs;

namespace RMS.Application.Features.Requisitions.Queries.GetRequisitionById;

public record GetRequisitionByIdQuery(Guid Id) : IRequest<RequisitionDto>;
