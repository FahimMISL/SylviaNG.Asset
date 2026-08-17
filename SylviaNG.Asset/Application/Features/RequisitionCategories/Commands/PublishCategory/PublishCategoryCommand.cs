using MediatR;
using RMS.Application.Features.RequisitionCategories.DTOs;

namespace RMS.Application.Features.RequisitionCategories.Commands.PublishCategory;

/// <summary>
/// US-001 AC6: locks in the current field schema as a new immutable version.
/// In-progress requisitions submitted under an earlier version keep that
/// version's snapshot even after this category is edited again.
/// </summary>
public record PublishCategoryCommand(Guid CategoryId) : IRequest<CategoryDto>;
