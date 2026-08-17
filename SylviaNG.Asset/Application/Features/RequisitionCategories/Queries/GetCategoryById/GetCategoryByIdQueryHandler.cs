using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.RequisitionCategories.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.RequisitionCategories.Queries.GetCategoryById;

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, CategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICurrentUserService _currentUser;

    public GetCategoryByIdQueryHandler(ICategoryRepository categoryRepository, ICurrentUserService currentUser)
    {
        _categoryRepository = categoryRepository;
        _currentUser = currentUser;
    }

    public async Task<CategoryDto> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(RequisitionCategory), request.CategoryId);

        if (category.CompanyId != companyId)
        {
            throw new ForbiddenException();
        }

        return CategoryDto.FromEntity(category);
    }
}
