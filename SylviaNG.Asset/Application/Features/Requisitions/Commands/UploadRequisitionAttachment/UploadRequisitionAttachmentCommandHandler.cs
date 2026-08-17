using MediatR;
using Microsoft.Extensions.Configuration;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Requisitions.DTOs;
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

        if (requisition.RequestedByUserId != userId)
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

        var maxTotalSizeBytes = _configuration.GetValue("Rms:MaxTotalAttachmentsSizeMb", RequisitionAttachmentRules.DefaultMaxTotalSizeMb) * 1024 * 1024;
        var currentTotal = requisition.Attachments.Sum(a => a.SizeBytes);
        if (currentTotal + request.SizeBytes > maxTotalSizeBytes)
        {
            throw new ConflictException($"This requisition's attachments would exceed the total limit of {maxTotalSizeBytes / (1024 * 1024)} MB.");
        }

        var storagePath = await _fileStorage.SaveAsync(request.RequisitionId.ToString(), request.FileName, request.Content, cancellationToken);

        var attachment = new RequisitionAttachment
        {
            RequisitionId = requisition.Id,
            FileName = request.FileName,
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            StoragePath = storagePath,
            UploadedByUserId = userId,
            UploadedByName = actorName,
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        // requisition was loaded (already tracked), so the new attachment needs to be registered
        // explicitly - see Requisition.Submit's remarks on this same EF Core tracking issue.
        _requisitionRepository.AddAttachment(requisition, attachment);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync(
            "RequisitionAttachmentAdded", nameof(Requisition), requisition.Id,
            pastApproverAction ? $"File={request.FileName}; Reason={request.Reason}" : $"File={request.FileName}", cancellationToken);

        return RequisitionAttachmentDto.FromEntity(attachment);
    }
}
