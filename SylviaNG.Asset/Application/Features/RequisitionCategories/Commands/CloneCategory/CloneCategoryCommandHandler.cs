using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.RequisitionCategories.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.RequisitionCategories.Commands.CloneCategory;

public class CloneCategoryCommandHandler : IRequestHandler<CloneCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public CloneCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        ICurrentUserService currentUser,
        IAuditLogger auditLogger,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<CategoryDto> Handle(CloneCategoryCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var source = await _categoryRepository.GetByIdAsync(request.SourceCategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(RequisitionCategory), request.SourceCategoryId);

        if (source.CompanyId != companyId)
        {
            throw new ForbiddenException();
        }

        if (await _categoryRepository.NameExistsAsync(companyId, request.NewName, null, cancellationToken))
        {
            throw new ConflictException($"A category named '{request.NewName}' already exists in your company.");
        }

        var clone = source.CloneAsNew(request.NewName);
        clone.CreatedByUserId = _currentUser.UserId;
        clone.CreatedAtUtc = DateTime.UtcNow;

        _categoryRepository.Add(clone);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("CategoryCloned", nameof(RequisitionCategory), clone.Id,
            $"ClonedFrom={source.Id}, Name={clone.Name}", cancellationToken);

        return CategoryDto.FromEntity(clone);
    }
}
