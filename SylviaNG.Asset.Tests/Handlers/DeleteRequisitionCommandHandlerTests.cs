using FluentAssertions;
using Moq;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Requisitions.Commands.DeleteRequisition;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Handlers;

/// <summary>
/// Draft or UnderReview requisitions are eligible for real deletion, per explicit product decision - a
/// requestor can pull back a request that's actively being reviewed, not just one that was never
/// submitted. Anything further along (Submitted/SentBack/Approved/etc.) must go through Cancel instead.
/// Scoped strictly to the owning requestor's own requisitions.
/// </summary>
public class DeleteRequisitionCommandHandlerTests
{
    private readonly Mock<IRequisitionRepository> _requisitionRepository = new();
    private readonly Mock<IFileStorageService> _fileStorage = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _requisitionId = Guid.NewGuid();

    private readonly DeleteRequisitionCommandHandler _handler;

    public DeleteRequisitionCommandHandlerTests()
    {
        _currentUser.Setup(c => c.UserId).Returns(_userId);

        _handler = new DeleteRequisitionCommandHandler(
            _requisitionRepository.Object, _fileStorage.Object, _currentUser.Object, _auditLogger.Object, _unitOfWork.Object);
    }

    private Requisition BuildRequisition(RequisitionStatus status, Guid ownerId) => new()
    {
        Id = _requisitionId,
        CompanyId = _companyId,
        RequestedByUserId = ownerId,
        Status = status,
    };

    [Fact]
    public async Task Handle_DraftOwnedByCaller_DeletesItAndAnyStoredAttachments()
    {
        var requisition = BuildRequisition(RequisitionStatus.Draft, _userId);
        requisition.Attachments.Add(new RequisitionAttachment { StoragePath = "requisitions/att1.pdf" });
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(requisition);

        var act = () => _handler.Handle(new DeleteRequisitionCommand(_requisitionId), CancellationToken.None);

        await act.Should().NotThrowAsync();
        _fileStorage.Verify(f => f.Delete("requisitions/att1.pdf"), Times.Once);
        _requisitionRepository.Verify(r => r.Remove(requisition), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnderReviewOwnedByCaller_DeletesIt()
    {
        var requisition = BuildRequisition(RequisitionStatus.UnderReview, _userId);
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(requisition);

        var act = () => _handler.Handle(new DeleteRequisitionCommand(_requisitionId), CancellationToken.None);

        await act.Should().NotThrowAsync();
        _requisitionRepository.Verify(r => r.Remove(requisition), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonDraftStatus_ThrowsConflictException_AndDoesNotDelete()
    {
        var requisition = BuildRequisition(RequisitionStatus.Submitted, _userId);
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(requisition);

        var act = () => _handler.Handle(new DeleteRequisitionCommand(_requisitionId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _requisitionRepository.Verify(r => r.Remove(It.IsAny<Requisition>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DraftOwnedBySomeoneElse_ThrowsForbiddenException_AndDoesNotDelete()
    {
        var requisition = BuildRequisition(RequisitionStatus.Draft, Guid.NewGuid());
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(requisition);

        var act = () => _handler.Handle(new DeleteRequisitionCommand(_requisitionId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        _requisitionRepository.Verify(r => r.Remove(It.IsAny<Requisition>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
