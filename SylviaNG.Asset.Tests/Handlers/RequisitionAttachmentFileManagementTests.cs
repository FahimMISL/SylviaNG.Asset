using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Requisitions.Commands.DeleteRequisitionAttachment;
using RMS.Application.Features.Requisitions.Commands.UploadRequisitionAttachment;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Application.Features.Requisitions.Queries.GetRequisitionAttachmentDownload;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Handlers;

/// <summary>
/// Feature 13 (File Management): version computation, broadened upload authorization (Procurement
/// Officer on a pipeline-stage requisition, HR Manager on a Manpower-category requisition - Delete
/// stays owner-only, untouched), soft-delete, latest-version computation, and download auditing.
/// </summary>
public class RequisitionAttachmentFileManagementTests
{
    private readonly Mock<IRequisitionRepository> _requisitionRepository = new();
    private readonly Mock<IFileStorageService> _fileStorage = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    // IConfiguration's GetValue<T> extension method NREs against a bare Moq mock (it walks
    // GetSection internally) - a real, empty in-memory configuration falls through to each call's own
    // default value instead, which is exactly the "no Rms:* override configured" scenario this app
    // actually runs under today (confirmed - no Rms section exists in any appsettings file).
    private readonly IConfiguration _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
    private readonly Mock<IApprovalDelegationRepository> _delegationRepository = new();

    private readonly Guid _requisitionId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public RequisitionAttachmentFileManagementTests()
    {
        _requisitionRepository
            .Setup(r => r.AddAttachment(It.IsAny<Requisition>(), It.IsAny<RequisitionAttachment>()))
            .Callback<Requisition, RequisitionAttachment>((req, att) => req.Attachments.Add(att));
        _fileStorage
            .Setup(f => f.SaveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("some/path.pdf");
    }

    private Requisition BuildRequisition(RequisitionStatus status, string categoryName = "IT") => new()
    {
        Id = _requisitionId,
        CompanyId = Guid.NewGuid(),
        CategoryId = Guid.NewGuid(),
        Category = new RequisitionCategory { Name = categoryName },
        RequestedByUserId = _ownerId,
        Status = status,
        NeedByDate = DateTime.UtcNow.AddDays(5),
    };

    private UploadRequisitionAttachmentCommandHandler BuildUploadHandler() => new(
        _requisitionRepository.Object, _fileStorage.Object, _currentUser.Object, _auditLogger.Object, _unitOfWork.Object, _configuration);

    private static Stream EmptyStream() => new MemoryStream();

    // ---- Version computation ----

    [Fact]
    public async Task Upload_FirstFileForADocumentType_IsVersion1_AndAddedEvent()
    {
        var requisition = BuildRequisition(RequisitionStatus.Draft);
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(requisition);
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);

        var result = await BuildUploadHandler().Handle(
            new UploadRequisitionAttachmentCommand(_requisitionId, "a.pdf", "application/pdf", 100, EmptyStream(), null, DocumentType.SupportingDocument),
            CancellationToken.None);

        result.Version.Should().Be(1);
        result.IsLatestVersion.Should().BeTrue();
        _auditLogger.Verify(a => a.LogAsync(
            "RequisitionAttachmentAdded", nameof(Requisition), _requisitionId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upload_SecondFileSameDocumentType_IsVersion2_AndVersionAddedEvent_PreviousNoLongerLatest()
    {
        var requisition = BuildRequisition(RequisitionStatus.Draft);
        requisition.Attachments.Add(new RequisitionAttachment
        {
            Id = Guid.NewGuid(), RequisitionId = _requisitionId, DocumentType = DocumentType.ApprovalMemo, Version = 1, FileName = "memo-v1.pdf",
        });
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(requisition);
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);

        var result = await BuildUploadHandler().Handle(
            new UploadRequisitionAttachmentCommand(_requisitionId, "memo-v2.pdf", "application/pdf", 100, EmptyStream(), null, DocumentType.ApprovalMemo),
            CancellationToken.None);

        result.Version.Should().Be(2);
        result.IsLatestVersion.Should().BeTrue();
        _auditLogger.Verify(a => a.LogAsync(
            "RequisitionAttachmentVersionAdded", nameof(Requisition), _requisitionId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upload_DifferentDocumentTypeOnSameRequisition_GetsItsOwnVersion1()
    {
        var requisition = BuildRequisition(RequisitionStatus.Draft);
        requisition.Attachments.Add(new RequisitionAttachment
        {
            Id = Guid.NewGuid(), RequisitionId = _requisitionId, DocumentType = DocumentType.ApprovalMemo, Version = 3, FileName = "memo-v3.pdf",
        });
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(requisition);
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);

        var result = await BuildUploadHandler().Handle(
            new UploadRequisitionAttachmentCommand(_requisitionId, "support.pdf", "application/pdf", 100, EmptyStream(), null, DocumentType.SupportingDocument),
            CancellationToken.None);

        result.Version.Should().Be(1);
    }

    [Fact]
    public async Task Upload_SoftDeletedVersionStillCountsTowardNextVersionNumber()
    {
        var requisition = BuildRequisition(RequisitionStatus.Draft);
        requisition.Attachments.Add(new RequisitionAttachment
        {
            Id = Guid.NewGuid(), RequisitionId = _requisitionId, DocumentType = DocumentType.SupportingDocument, Version = 1, FileName = "old.pdf", IsDeleted = true,
        });
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(requisition);
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);

        var result = await BuildUploadHandler().Handle(
            new UploadRequisitionAttachmentCommand(_requisitionId, "new.pdf", "application/pdf", 100, EmptyStream(), null, DocumentType.SupportingDocument),
            CancellationToken.None);

        // Never reuses version 1 even though the row holding it is soft-deleted.
        result.Version.Should().Be(2);
    }

    // ---- Upload authorization: broadened for Procurement/HR, Delete stays owner-only (separate tests below) ----

    [Fact]
    public async Task Upload_ProcurementOfficer_OnPipelineStatusRequisition_Allowed()
    {
        var requisition = BuildRequisition(RequisitionStatus.Approved);
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(requisition);
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _currentUser.Setup(c => c.IsInRole(UserRole.ProcurementOfficer)).Returns(true);

        // Approved is past Draft/Submitted (an approver has already acted), so the pre-existing
        // "reason required past that point" rule applies here same as it would for the owner.
        var act = () => BuildUploadHandler().Handle(
            new UploadRequisitionAttachmentCommand(
                _requisitionId, "po.pdf", "application/pdf", 100, EmptyStream(), "Attaching purchase order", DocumentType.ProcurementDocument),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Upload_ProcurementOfficer_OnDraftRequisition_StillForbidden()
    {
        var requisition = BuildRequisition(RequisitionStatus.Draft);
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(requisition);
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _currentUser.Setup(c => c.IsInRole(UserRole.ProcurementOfficer)).Returns(true);

        var act = () => BuildUploadHandler().Handle(
            new UploadRequisitionAttachmentCommand(_requisitionId, "po.pdf", "application/pdf", 100, EmptyStream(), null, DocumentType.ProcurementDocument),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Upload_HrManager_OnManpowerCategoryRequisition_Allowed()
    {
        var requisition = BuildRequisition(RequisitionStatus.Draft, categoryName: "Manpower");
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(requisition);
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _currentUser.Setup(c => c.IsInRole(UserRole.HrManager)).Returns(true);

        var act = () => BuildUploadHandler().Handle(
            new UploadRequisitionAttachmentCommand(_requisitionId, "jd.pdf", "application/pdf", 100, EmptyStream(), null, DocumentType.JobDescription),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Upload_HrManager_OnNonManpowerRequisition_StillForbidden()
    {
        var requisition = BuildRequisition(RequisitionStatus.Draft, categoryName: "IT");
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(requisition);
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _currentUser.Setup(c => c.IsInRole(UserRole.HrManager)).Returns(true);

        var act = () => BuildUploadHandler().Handle(
            new UploadRequisitionAttachmentCommand(_requisitionId, "jd.pdf", "application/pdf", 100, EmptyStream(), null, DocumentType.JobDescription),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Upload_NonOwnerEmployee_StillForbidden_Regression()
    {
        var requisition = BuildRequisition(RequisitionStatus.Draft);
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(requisition);
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _currentUser.Setup(c => c.IsInRole(It.IsAny<UserRole>())).Returns(false);

        var act = () => BuildUploadHandler().Handle(
            new UploadRequisitionAttachmentCommand(_requisitionId, "x.pdf", "application/pdf", 100, EmptyStream(), null, DocumentType.SupportingDocument),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ---- Delete: soft-delete, still owner-only ----

    [Fact]
    public async Task Delete_SoftDeletes_RowAndFilePreserved_NotPhysicallyRemoved()
    {
        var attachment = new RequisitionAttachment { Id = Guid.NewGuid(), RequisitionId = _requisitionId, UploadedByUserId = _ownerId, FileName = "a.pdf" };
        var requisition = BuildRequisition(RequisitionStatus.Draft);
        requisition.Attachments.Add(attachment);
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(requisition);
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);

        var handler = new DeleteRequisitionAttachmentCommandHandler(_requisitionRepository.Object, _currentUser.Object, _auditLogger.Object, _unitOfWork.Object);
        await handler.Handle(new DeleteRequisitionAttachmentCommand(_requisitionId, attachment.Id), CancellationToken.None);

        attachment.IsDeleted.Should().BeTrue();
        attachment.DeletedByUserId.Should().Be(_ownerId);
        attachment.DeletedAtUtc.Should().NotBeNull();
        // Never hard-removed from the tracked collection - RemoveAttachment must never be called.
        _requisitionRepository.Verify(r => r.RemoveAttachment(It.IsAny<Requisition>(), It.IsAny<RequisitionAttachment>()), Times.Never);
    }

    // ---- IsLatestVersion computation (pure, no mocks needed) ----

    [Fact]
    public void FromEntity_MultipleVersions_OnlyHighestNonDeletedVersionIsLatest()
    {
        var v1 = new RequisitionAttachment { Id = Guid.NewGuid(), DocumentType = DocumentType.ApprovalMemo, Version = 1 };
        var v2 = new RequisitionAttachment { Id = Guid.NewGuid(), DocumentType = DocumentType.ApprovalMemo, Version = 2 };
        var v3Deleted = new RequisitionAttachment { Id = Guid.NewGuid(), DocumentType = DocumentType.ApprovalMemo, Version = 3, IsDeleted = true };
        var siblings = new[] { v1, v2, v3Deleted };

        RequisitionAttachmentDto.FromEntity(v1, siblings).IsLatestVersion.Should().BeFalse();
        RequisitionAttachmentDto.FromEntity(v2, siblings).IsLatestVersion.Should().BeTrue();
        RequisitionAttachmentDto.FromEntity(v3Deleted, siblings).IsLatestVersion.Should().BeFalse();
    }

    // ---- Download: audited ----

    [Fact]
    public async Task Download_Succeeds_LogsAuditEvent()
    {
        var attachment = new RequisitionAttachment
        {
            Id = Guid.NewGuid(), RequisitionId = _requisitionId, FileName = "a.pdf", ContentType = "application/pdf", StoragePath = "some/path",
        };
        var requisition = BuildRequisition(RequisitionStatus.Draft);
        requisition.Attachments.Add(attachment);
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(requisition);
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);
        _fileStorage.Setup(f => f.OpenRead("some/path")).Returns(new MemoryStream());

        var handler = new GetRequisitionAttachmentDownloadQueryHandler(
            _requisitionRepository.Object, _delegationRepository.Object, _fileStorage.Object, _currentUser.Object, _auditLogger.Object);

        await handler.Handle(new GetRequisitionAttachmentDownloadQuery(_requisitionId, attachment.Id), CancellationToken.None);

        _auditLogger.Verify(a => a.LogAsync(
            "RequisitionAttachmentDownloaded", nameof(Requisition), _requisitionId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Download_SoftDeletedAttachment_NotFound()
    {
        var attachment = new RequisitionAttachment
        {
            Id = Guid.NewGuid(), RequisitionId = _requisitionId, FileName = "a.pdf", IsDeleted = true,
        };
        var requisition = BuildRequisition(RequisitionStatus.Draft);
        requisition.Attachments.Add(attachment);
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(requisition);
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);

        var handler = new GetRequisitionAttachmentDownloadQueryHandler(
            _requisitionRepository.Object, _delegationRepository.Object, _fileStorage.Object, _currentUser.Object, _auditLogger.Object);

        var act = () => handler.Handle(new GetRequisitionAttachmentDownloadQuery(_requisitionId, attachment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
