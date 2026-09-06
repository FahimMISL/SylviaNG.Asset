using MediatR;
using Microsoft.Extensions.Configuration;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Application.Features.Requisitions.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Requisitions.Commands.UploadRequisitionAttachment;

public class UploadRequisitionAttachmentCommandHandler : IRequestHandler<UploadRequisitionAttachmentCommand, RequisitionAttachmentDto>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public UploadRequisitionAttachmentCommandHandler(
        IRequisitionRepository requisitionRepository,
        IFileStorageService fileStorage,
        ICurrentUserService currentUser,
        IAuditLogger auditLogger,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _requisitionRepository = requisitionRepository;
        _fileStorage = fileStorage;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<RequisitionAttachmentDto> Handle(UploadRequisitionAttachmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var actorName = _currentUser.FullName ?? "Unknown";

        var requisition = await _requisitionRepository.GetByIdAsync(request.RequisitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Requisition), request.RequisitionId);

        // Feature 13: broadened beyond owner-only so Procurement Officers can attach procurement/
        // fulfillment documents to requisitions they're processing, and HR Managers can attach
        // manpower documents to requisitions they didn't personally submit - the same two scopes
        // Download (Feature 10) and the Dashboard's manpower summary (Feature 12) already established.
        // Delete stays owner-only, untouched - a destructive action shouldn't be handed to non-owners.
        var isOwner = requisition.RequestedByUserId == userId;
        var isProcurementOnPipeline = !isOwner && _currentUser.IsInRole(UserRole.ProcurementOfficer)
            && RequisitionAccessHelper.ProcurementPipelineStatuses.Contains(requisition.Status);
        var isHrOnManpower = !isOwner && _currentUser.IsInRole(UserRole.HrManager)
            && string.Equals(requisition.Category?.Name?.Trim(), "Manpower", StringComparison.OrdinalIgnoreCase);
        if (!isOwner && !isProcurementOnPipeline && !isHrOnManpower)
        {
            throw new ForbiddenException();
        }

        if (requisition.Status is RequisitionStatus.Cancelled or RequisitionStatus.Closed or RequisitionStatus.Rejected)
        {
            throw new ConflictException("Attachments cannot be added to a requisition in this status.");
        }

        // US-007 AC6: once past Draft/Submitted (an approver has acted), addition still allowed but
        // only with a logged reason.
        var pastApproverAction = requisition.Status is not (RequisitionStatus.Draft or RequisitionStatus.Submitted);
        if (pastApproverAction && string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ConflictException("A reason is required to add an attachment after an approver has acted on this requisition.");
        }

        if (!RequisitionAttachmentRules.IsAllowedExtension(request.FileName))
        {
            throw new ConflictException("Unsupported file type. Allowed: PDF, DOCX, XLSX, JPEG, PNG, PPTX.");
        }

        var maxFileSizeBytes = _configuration.GetValue("Rms:MaxAttachmentSizeMb", RequisitionAttachmentRules.DefaultMaxFileSizeMb) * 1024 * 1024;
        if (request.SizeBytes > maxFileSizeBytes)
        {
            throw new ConflictException($"File exceeds the maximum size of {maxFileSizeBytes / (1024 * 1024)} MB.");
        }

        // Feature 13: soft-deleted attachments no longer count toward the active storage cap - a
        // "removed" file shouldn't keep blocking new uploads.
        var maxTotalSizeBytes = _configuration.GetValue("Rms:MaxTotalAttachmentsSizeMb", RequisitionAttachmentRules.DefaultMaxTotalSizeMb) * 1024 * 1024;
        var currentTotal = requisition.Attachments.Where(a => !a.IsDeleted).Sum(a => a.SizeBytes);
        if (currentTotal + request.SizeBytes > maxTotalSizeBytes)
        {
            throw new ConflictException($"This requisition's attachments would exceed the total limit of {maxTotalSizeBytes / (1024 * 1024)} MB.");
        }

        var storagePath = await _fileStorage.SaveAsync(request.RequisitionId.ToString(), request.FileName, request.Content, cancellationToken);

        // Feature 13: next version in this (RequisitionId, DocumentType) lineage - includes
        // soft-deleted rows so a removed version's number is never reused, keeping the sequence
        // strictly monotonic even across a delete.
        var version = requisition.Attachments
            .Where(a => a.DocumentType == request.DocumentType)
            .Select(a => (int?)a.Version)
            .Max() is { } maxVersion ? maxVersion + 1 : 1;

        var attachment = new RequisitionAttachment
        {
            RequisitionId = requisition.Id,
            FileName = request.FileName,
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            StoragePath = storagePath,
            DocumentType = request.DocumentType,
            Version = version,
            UploadedByUserId = userId,
            UploadedByName = actorName,
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        // requisition was loaded (already tracked), so the new attachment needs to be registered
        // explicitly - see Requisition.Submit's remarks on this same EF Core tracking issue.
        _requisitionRepository.AddAttachment(requisition, attachment);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        // Feature 13: distinguishes a genuinely new document (Version 1) from a new version of an
        // existing one - the task names these as two separate events to audit.
        var actionType = version == 1 ? "RequisitionAttachmentAdded" : "RequisitionAttachmentVersionAdded";
        var details = $"File={request.FileName}; Type={request.DocumentType}; Version={version}"
            + (pastApproverAction ? $"; Reason={request.Reason}" : string.Empty);
        await _auditLogger.LogAsync(actionType, nameof(Requisition), requisition.Id, details, cancellationToken);

        return RequisitionAttachmentDto.FromEntity(attachment, requisition.Attachments);
    }
}
