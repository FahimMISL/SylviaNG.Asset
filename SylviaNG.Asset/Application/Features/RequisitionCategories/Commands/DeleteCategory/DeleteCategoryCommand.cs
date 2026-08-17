using MediatR;

namespace RMS.Application.Features.RequisitionCategories.Commands.DeleteCategory;

public record DeleteCategoryCommand(Guid CategoryId) : IRequest;
