using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.RequisitionCategories.DTOs;
using RMS.Application.Features.RequisitionCategories.Mappings;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.RequisitionCategories.Commands.CreateCategory;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICostCenterRepository _costCenterRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        ICostCenterRepository costCenterRepository,
        ICurrentUserService currentUser,
        IAuditLogger auditLogger,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _costCenterRepository = costCenterRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        // US-001 AC10: category name must be unique within the same company.
        if (await _categoryRepository.NameExistsAsync(companyId, request.Name, null, cancellationToken))
        {
            throw new ConflictException($"A category named '{request.Name}' already exists in your company.");
        }

        var category = new RequisitionCategory
        {
            CompanyId = companyId,
            Name = request.Name,
            Description = request.Description,
            IsActive = false, // US-001: admin previews before publishing; starts inactive until activated.
            IsCostCenterMandatory = request.IsCostCenterMandatory,
            ProjectCodeRequirement = request.ProjectCodeRequirement,
            CreatedByUserId = _currentUser.UserId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        category.FieldDefinitions = CategoryFieldMapper.ToEntities(category.Id, request.FieldDefinitions);
        category.Items = request.Items.Select((i, index) => new CategoryItem
        {
            CategoryId = category.Id,
            Name = i.Name,
            IsActive = i.IsActive,
            DisplayOrder = index,
            Price = i.Price,
        }).ToList();

        if (request.CostCenterIds.Count > 0)
        {
            var distinctIds = request.CostCenterIds.Distinct().ToList();
            var costCenters = await _costCenterRepository.GetByIdsAsync(distinctIds, cancellationToken);
            if (costCenters.Count != distinctIds.Count)
            {
                throw new NotFoundException(nameof(CostCenter), "one or more of the supplied ids");
            }

            foreach (var costCenter in costCenters)
            {
                category.CostCenterLinks.Add(new CategoryCostCenterLink
                {
                    CategoryId = category.Id,
                    CostCenterId = costCenter.Id,
                });
            }
        }

        _categoryRepository.Add(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("CategoryCreated", nameof(RequisitionCategory), category.Id,
            $"Name={category.Name}", cancellationToken);

        return CategoryDto.FromEntity(category);
    }
}
