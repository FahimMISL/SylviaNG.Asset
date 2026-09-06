using FluentAssertions;
using Moq;
using RMS.Application.Features.ApprovalWorkflows.Services;
using RMS.Application.Features.Approvals.Commands.ApproveApproval;
using RMS.Application.Features.Approvals.Commands.RejectApproval;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Handlers;

/// <summary>
/// Feature 8: before this fix, every approval-decision audit entry was anchored to
/// EntityName=RequisitionApproval/EntityId=approval.Id, so filtering the audit trail by
/// requisitionId (the whole point of "view audit history for one requisition") silently missed
/// every approve/reject/etc. decision. These handlers now anchor to the requisition itself.
/// </summary>
public class AuditLogEntityAnchoringTests
{
    [Fact]
    public async Task ApproveApproval_LogsAuditEntry_AnchoredToRequisition_NotToTheApprovalStageInstance()
    {
        var requisitionApprovalRepository = new Mock<IRequisitionApprovalRepository>();
        var delegationRepository = new Mock<IApprovalDelegationRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var auditLogger = new Mock<IAuditLogger>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var workflowRepository = new Mock<IApprovalWorkflowRepository>();
        var requisitionRepository = new Mock<IRequisitionRepository>();
        var userRepository = new Mock<IUserRepository>();
        var notificationService = new Mock<INotificationService>();

        var requisition = new Requisition
        {
            CompanyId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            RequestedByUserId = Guid.NewGuid(),
            NeedByDate = DateTime.UtcNow.AddDays(5),
            Status = RequisitionStatus.UnderReview,
        };

        var approverId = Guid.NewGuid();
        var stage = new ApprovalWorkflowStage { StageOrder = 1, Name = "Manager Review" };
        var version = new ApprovalWorkflowVersion { Id = Guid.NewGuid(), IsPublished = true, AppliesToAllCategories = true };
        version.Stages.Add(stage);
        var process = new RequisitionApprovalProcess { RequisitionId = requisition.Id, ApprovalWorkflowVersionId = version.Id, Requisition = requisition };
        var approval = new RequisitionApproval
        {
            ApprovalWorkflowStage = stage,
            StageOrder = 1,
            Status = RequisitionApprovalStatus.Pending,
            RequisitionApprovalProcess = process,
        };
        approval.Assignments.Add(new RequisitionApprovalAssignment { AssignedUserId = approverId, IsRequired = true });
        process.StageInstances.Add(approval);

        requisitionApprovalRepository.Setup(r => r.GetApprovalByIdAsync(approval.Id, It.IsAny<CancellationToken>())).ReturnsAsync(approval);
        delegationRepository
            .Setup(r => r.GetActiveOnAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApprovalDelegation?)null);
        workflowRepository.Setup(r => r.GetVersionByIdAsync(version.Id, It.IsAny<CancellationToken>())).ReturnsAsync(version);

        currentUser.Setup(c => c.UserId).Returns(approverId);
        currentUser.Setup(c => c.FullName).Returns("Manny Manager");
        currentUser.Setup(c => c.Role).Returns(UserRole.LineManager);

        var engine = new ApprovalWorkflowEngine(workflowRepository.Object, requisitionApprovalRepository.Object, requisitionRepository.Object, userRepository.Object);
        var handler = new ApproveApprovalCommandHandler(
            requisitionApprovalRepository.Object, delegationRepository.Object, currentUser.Object, auditLogger.Object, unitOfWork.Object, engine,
            notificationService.Object);

        await handler.Handle(new ApproveApprovalCommand(approval.Id, "Looks good.", null), CancellationToken.None);

        auditLogger.Verify(a => a.LogAsync(
            "ApprovalApproved", nameof(Requisition), requisition.Id, It.Is<string>(d => d.Contains("Looks good.")), It.IsAny<CancellationToken>()),
            Times.Once);
        // Must NOT be logged against the stage-instance id - that's exactly the gap this fixes.
        auditLogger.Verify(a => a.LogAsync(
            "ApprovalApproved", nameof(RequisitionApproval), approval.Id, It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RejectApproval_LogsAuditEntry_AnchoredToRequisition_NotToTheApprovalStageInstance()
    {
        var requisitionApprovalRepository = new Mock<IRequisitionApprovalRepository>();
        var requisitionRepository = new Mock<IRequisitionRepository>();
        var delegationRepository = new Mock<IApprovalDelegationRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var auditLogger = new Mock<IAuditLogger>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var notificationService = new Mock<INotificationService>();

        var requisition = new Requisition
        {
            CompanyId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            RequestedByUserId = Guid.NewGuid(),
            NeedByDate = DateTime.UtcNow.AddDays(5),
            Status = RequisitionStatus.UnderReview,
        };

        var approverId = Guid.NewGuid();
        var stage = new ApprovalWorkflowStage { StageOrder = 1, Name = "Manager Review" };
        var process = new RequisitionApprovalProcess { RequisitionId = requisition.Id, Requisition = requisition };
        var approval = new RequisitionApproval
        {
            ApprovalWorkflowStage = stage,
            StageOrder = 1,
            Status = RequisitionApprovalStatus.Pending,
            RequisitionApprovalProcess = process,
        };
        approval.Assignments.Add(new RequisitionApprovalAssignment { AssignedUserId = approverId, IsRequired = true });
        process.StageInstances.Add(approval);

        requisitionApprovalRepository.Setup(r => r.GetApprovalByIdAsync(approval.Id, It.IsAny<CancellationToken>())).ReturnsAsync(approval);
        delegationRepository
            .Setup(r => r.GetActiveOnAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApprovalDelegation?)null);

        currentUser.Setup(c => c.UserId).Returns(approverId);
        currentUser.Setup(c => c.FullName).Returns("Manny Manager");
        currentUser.Setup(c => c.Role).Returns(UserRole.LineManager);

        var handler = new RejectApprovalCommandHandler(
            requisitionApprovalRepository.Object, requisitionRepository.Object, delegationRepository.Object,
            currentUser.Object, auditLogger.Object, unitOfWork.Object, notificationService.Object);

        await handler.Handle(new RejectApprovalCommand(approval.Id, "Missing budget approval."), CancellationToken.None);

        auditLogger.Verify(a => a.LogAsync(
            "ApprovalRejected", nameof(Requisition), requisition.Id, It.Is<string>(d => d.Contains("Missing budget approval.")), It.IsAny<CancellationToken>()),
            Times.Once);
        auditLogger.Verify(a => a.LogAsync(
            "ApprovalRejected", nameof(RequisitionApproval), approval.Id, It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
