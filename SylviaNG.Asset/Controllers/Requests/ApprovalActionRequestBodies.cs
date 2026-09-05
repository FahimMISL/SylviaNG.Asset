using RMS.Application.Features.Approvals.DTOs;

namespace RMS.Api.Controllers.Requests;

public record ApprovalCommentRequestBody(string Comment);

public record ApproveApprovalRequestBody(string Comment);

public record DelegateApprovalActionRequestBody(Guid DelegateToUserId, string Comment);

public record PartialApproveApprovalRequestBody(string Comment, List<PartialApprovalDecisionInput> Decisions);
