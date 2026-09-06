using MediatR;
using RMS.Application.Features.RequisitionCategories.DTOs;

namespace RMS.Application.Features.RequisitionCategories.Commands.CloneCategory;

/// <summary>US-001 AC7: clone an existing category template to create a similar one.</summary>
public record CloneCategoryCommand(Guid SourceCategoryId, string NewName) : IRequest<CategoryDto>;
