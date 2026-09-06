using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RMS.Api.Controllers;
using RMS.Api.Controllers.Requests;
using RMS.Application.Features.Approvals.Commands.ApproveApproval;
using RMS.Application.Features.Approvals.Commands.RejectApproval;
using RMS.Application.Features.Approvals.DTOs;
using RMS.Application.Features.Approvals.Queries.GetPendingApprovals;

namespace SylviaNG.Assets.Tests.Controllers;

public class ApprovalsControllerTests
{
    private readonly Mock<ISender> _senderMock;
    private readonly ApprovalsController _controller;

    public ApprovalsControllerTests()
    {
        _senderMock = new Mock<ISender>();
        _controller = new ApprovalsController(_senderMock.Object);
    }

    [Fact]
    public async Task GetPending_ShouldReturnOkWithInbox()
    {
        var expected = new List<PendingApprovalDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "REQ-2026-00001", "IT Equipment", "High",
                1500m, DateTime.UtcNow.AddDays(3), DateTime.UtcNow, 1, "Line Manager Review", false, true, null, "Green", false),
        };
        _senderMock.Setup(s => s.Send(It.IsAny<GetPendingApprovalsQuery>(), default)).ReturnsAsync(expected);

        var result = await _controller.GetPending(CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Approve_ShouldSendCommand_AndReturnNoContent()
    {
        var approvalId = Guid.NewGuid();
        var body = new ApproveApprovalRequestBody("Looks good, approving this request.", null);

        var result = await _controller.Approve(approvalId, body, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _senderMock.Verify(s => s.Send(
            It.Is<ApproveApprovalCommand>(c => c.ApprovalId == approvalId && c.Comment == body.Comment),
            default), Times.Once);
    }

    [Fact]
    public async Task Reject_ShouldSendCommand_AndReturnNoContent()
    {
        var approvalId = Guid.NewGuid();
        var body = new ApprovalCommentRequestBody("Budget unavailable for this request right now.");

        var result = await _controller.Reject(approvalId, body, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _senderMock.Verify(s => s.Send(
            It.Is<RejectApprovalCommand>(c => c.ApprovalId == approvalId && c.Comment == body.Comment),
            default), Times.Once);
    }
}
