using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.RequisitionCategories.DTOs;
using RMS.Application.Interfaces;

namespace RMS.Application.Features.RequisitionCategories.Queries.GetCategories;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategorySummaryDto>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICurrentUserService _currentUser;

    public GetCategoriesQueryHandler(ICategoryRepository categoryRepository, ICurrentUserService currentUser)
    {
        _categoryRepository = categoryRepository;
        _currentUser = currentUser;
    }

    public async Task<List<CategorySummaryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();
        var categories = await _categoryRepository.GetAllAsync(companyId, request.IsActive, cancellationToken);
        return categories.Select(CategorySummaryDto.FromEntity).ToList();
    }
}
