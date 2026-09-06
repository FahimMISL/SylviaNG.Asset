using FluentAssertions;
using Moq;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.ApprovalWorkflows.Commands.PublishApprovalWorkflowVersion;
using RMS.Application.Features.ApprovalWorkflows.Commands.UpdateApprovalWorkflowVersion;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Handlers;

/// <summary>Workflow version snapshot immutability: once published, a version can never be edited or
/// re-published again - only a new version cloned from it can change anything.</summary>
public class ApprovalWorkflowVersionImmutabilityTests
{
    private readonly Mock<IApprovalWorkflowRepository> _workflowRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly Guid _companyId = Guid.NewGuid();

    public ApprovalWorkflowVersionImmutabilityTests()
    {
        _currentUser.Setup(c => c.CompanyId).Returns(_companyId);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
    }

    private ApprovalWorkflow WorkflowWithPublishedVersion(out ApprovalWorkflowVersion version)
    {
        var workflow = new ApprovalWorkflow { CompanyId = _companyId, Name = "Standard", CurrentVersionNumber = 1 };
        version = new ApprovalWorkflowVersion { ApprovalWorkflowId = workflow.Id, VersionNumber = 1, IsPublished = true, PublishedAtUtc = DateTime.UtcNow };
        version.Stages.Add(new ApprovalWorkflowStage { Name = "Stage 1", StageOrder = 1 });
        workflow.Versions.Add(version);
        return workflow;
    }

    [Fact]
    public async Task Publish_AlreadyPublishedVersion_ThrowsConflict()
    {
        var workflow = WorkflowWithPublishedVersion(out var version);
        _workflowRepository.Setup(r => r.GetByIdAsync(workflow.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workflow);

        var handler = new PublishApprovalWorkflowVersionCommandHandler(
            _workflowRepository.Object, _currentUser.Object, _auditLogger.Object, _unitOfWork.Object);

        var act = () => handler.Handle(new PublishApprovalWorkflowVersionCommand(workflow.Id, version.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Update_AlreadyPublishedVersion_ThrowsConflict()
    {
        var workflow = WorkflowWithPublishedVersion(out var version);
        _workflowRepository.Setup(r => r.GetByIdAsync(workflow.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workflow);

        var handler = new UpdateApprovalWorkflowVersionCommandHandler(
            _workflowRepository.Object, _currentUser.Object, _auditLogger.Object, _unitOfWork.Object);

        var stages = new List<RMS.Application.Features.ApprovalWorkflows.DTOs.ApprovalWorkflowStageInput>
        {
            new(1, "Stage 1", false, [new(ApproverType.SpecificUser, null, Guid.NewGuid(), null, true)], [], null),
        };
        var command = new UpdateApprovalWorkflowVersionCommand(workflow.Id, version.Id, ApprovalWorkflowRoutingMode.Sequential, true, [], stages, null);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Publish_UnpublishedDraft_Succeeds_AndBumpsCurrentVersionNumber()
    {
        var workflow = new ApprovalWorkflow { CompanyId = _companyId, Name = "Standard", CurrentVersionNumber = 0 };
        var draft = new ApprovalWorkflowVersion { ApprovalWorkflowId = workflow.Id, VersionNumber = 1, IsPublished = false };
        draft.Stages.Add(new ApprovalWorkflowStage { Name = "Stage 1", StageOrder = 1 });
        workflow.Versions.Add(draft);
        _workflowRepository.Setup(r => r.GetByIdAsync(workflow.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workflow);

        var handler = new PublishApprovalWorkflowVersionCommandHandler(
            _workflowRepository.Object, _currentUser.Object, _auditLogger.Object, _unitOfWork.Object);

        await handler.Handle(new PublishApprovalWorkflowVersionCommand(workflow.Id, draft.Id), CancellationToken.None);

        draft.IsPublished.Should().BeTrue();
        workflow.CurrentVersionNumber.Should().Be(1);
    }
}
