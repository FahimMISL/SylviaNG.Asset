using MediatR;
using RMS.Application.Features.RequisitionCategories.DTOs;

namespace RMS.Application.Features.RequisitionCategories.Commands.SetCategoryActiveState;

/// <summary>US-001 AC9: admin can set a category as active or inactive.</summary>
public record SetCategoryActiveStateCommand(Guid CategoryId, bool IsActive) : IRequest<CategoryDto>;
