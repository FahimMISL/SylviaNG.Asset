using System.Text.Json;
using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.RequisitionCategories.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.RequisitionCategories.Commands.PublishCategory;

public class PublishCategoryCommandHandler : IRequestHandler<PublishCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public PublishCategoryCommandHandler(
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

    public async Task<CategoryDto> Handle(PublishCategoryCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(RequisitionCategory), request.CategoryId);

        if (category.CompanyId != companyId)
        {
            throw new ForbiddenException();
        }

        category.EnsurePublishable();
        var versionNumber = category.IncrementVersion();

        var snapshot = CategoryDto.FromEntity(category);

        // Added directly via the repository (not category.Versions.Add) so EF Core
        // tracks it as a new insert - adding a pre-keyed entity through an
        // already-tracked parent's navigation collection can otherwise get
        // mis-detected as an update to a nonexistent row.
        _categoryRepository.AddVersion(new CategoryTemplateVersion
        {
            CategoryId = category.Id,
            VersionNumber = versionNumber,
            SnapshotJson = JsonSerializer.Serialize(snapshot),
            PublishedAtUtc = DateTime.UtcNow,
            PublishedByUserId = _currentUser.UserId ?? Guid.Empty,
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("CategoryPublished", nameof(RequisitionCategory), category.Id,
            $"VersionNumber={versionNumber}", cancellationToken);

        return CategoryDto.FromEntity(category);
    }
}
