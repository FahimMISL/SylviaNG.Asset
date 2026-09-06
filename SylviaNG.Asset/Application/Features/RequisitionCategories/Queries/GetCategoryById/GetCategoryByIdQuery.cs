using MediatR;
using RMS.Application.Features.RequisitionCategories.DTOs;

namespace RMS.Application.Features.RequisitionCategories.Queries.GetCategoryById;

/// <summary>Also used for US-001 AC5 "Employee View" preview - same schema, read-only.</summary>
public record GetCategoryByIdQuery(Guid CategoryId) : IRequest<CategoryDto>;
