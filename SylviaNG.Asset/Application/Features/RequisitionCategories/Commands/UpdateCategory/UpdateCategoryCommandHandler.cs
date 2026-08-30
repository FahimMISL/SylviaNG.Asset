using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.RequisitionCategories.DTOs;
using RMS.Application.Features.RequisitionCategories.Mappings;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.RequisitionCategories.Commands.UpdateCategory;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICostCenterRepository _costCenterRepository;
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        ICostCenterRepository costCenterRepository,
        IRequisitionRepository requisitionRepository,
        ICurrentUserService currentUser,
        IAuditLogger auditLogger,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _costCenterRepository = costCenterRepository;
        _requisitionRepository = requisitionRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(RequisitionCategory), request.CategoryId);

        if (category.CompanyId != companyId)
        {
            throw new ForbiddenException();
        }

        if (await _categoryRepository.NameExistsAsync(companyId, request.Name, category.Id, cancellationToken))
        {
            throw new ConflictException($"A category named '{request.Name}' already exists in your company.");
        }

        if (request.CostCenterIds.Count > 0)
        {
            var distinctIds = request.CostCenterIds.Distinct().ToList();
            var costCenters = await _costCenterRepository.GetByIdsAsync(distinctIds, cancellationToken);
            if (costCenters.Count != distinctIds.Count)
            {
                throw new NotFoundException(nameof(CostCenter), "one or more of the supplied ids");
            }
        }

        category.Name = request.Name;
        category.Description = request.Description;
        category.IsCostCenterMandatory = request.IsCostCenterMandatory;
        category.ProjectCodeRequirement = request.ProjectCodeRequirement;
        category.UpdatedByUserId = _currentUser.UserId;
        category.UpdatedAtUtc = DateTime.UtcNow;

        // Fields are fully replaced (new rows, new ids) on every save, so once any field on this
        // category has a real answer recorded against it, further field edits must be blocked -
        // otherwise the DB rejects the delete because that historical answer still points to it.
        if (await _requisitionRepository.AnyFieldValuesExistForCategoryAsync(category.Id, cancellationToken))
        {
            throw new ConflictException(
                "This category's fields can't be edited anymore because employees have already submitted requisitions using them. " +
                "Clone this category to make a new version instead.");
        }

        var newFieldDefinitions = CategoryFieldMapper.ToEntities(category.Id, request.FieldDefinitions);
        _categoryRepository.ReplaceFieldDefinitions(category, newFieldDefinitions);
        _categoryRepository.ReplaceCostCenterLinks(category, request.CostCenterIds.Distinct().ToList());

        var newItems = request.Items.Select((i, index) => new CategoryItem
        {
            CategoryId = category.Id,
            Name = i.Name,
            IsActive = i.IsActive,
            DisplayOrder = index,
            Price = i.Price,
        }).ToList();
        _categoryRepository.ReplaceItems(category, newItems);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("CategoryUpdated", nameof(RequisitionCategory), category.Id,
            $"Name={category.Name}", cancellationToken);

        return CategoryDto.FromEntity(category);
    }
}
