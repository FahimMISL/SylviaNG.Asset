using MediatR;
using RMS.Application.Features.RequisitionCategories.DTOs;

namespace RMS.Application.Features.RequisitionCategories.Queries.GetCategories;

public record GetCategoriesQuery(bool? IsActive) : IRequest<List<CategorySummaryDto>>;
