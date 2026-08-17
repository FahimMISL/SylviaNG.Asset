using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.RequisitionCategories.Commands.DeleteCategory;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IRequisitionExistenceChecker _requisitionExistenceChecker;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        ICurrentUserService currentUser,
        IRequisitionExistenceChecker requisitionExistenceChecker,
        IAuditLogger auditLogger,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _currentUser = currentUser;
        _requisitionExistenceChecker = requisitionExistenceChecker;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(RequisitionCategory), request.CategoryId);

        if (category.CompanyId != companyId)
        {
            throw new ForbiddenException();
        }

        // US-001 edge case: deleting a category with historical requisitions is blocked; deactivation must be used instead.
        if (await _requisitionExistenceChecker.HasRequisitionsForCategoryAsync(category.Id, cancellationToken))
        {
            throw new ConflictException(
                $"'{category.Name}' has historical requisitions and cannot be deleted. Deactivate it instead.");
        }

        _categoryRepository.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("CategoryDeleted", nameof(RequisitionCategory), category.Id,
            $"Name={category.Name}", cancellationToken);
    }
}
