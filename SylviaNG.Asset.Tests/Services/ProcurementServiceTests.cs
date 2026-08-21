using FluentAssertions;
using FluentValidation;
using Moq;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Procurement.Services;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Services;

/// <summary>
/// Feature 5's core engine - mirrors ApprovalWorkflowEngineTests's style. Uses the requisition's
/// already-loaded Items/ApprovalProcess navigation directly (as IRequisitionRepository.GetByIdAsync
/// really loads them), not a mocked repository call, since GetApprovedCeilings reads them in-memory.
/// </summary>
public class ProcurementServiceTests
{
    private readonly Mock<IRequisitionRepository> _requisitionRepository = new();
    private readonly ProcurementService _service;

    private readonly Guid _actorId = Guid.NewGuid();
    private const string ActorName = "Pat Procurement";
    private const string ActorRole = "ProcurementOfficer";

    public ProcurementServiceTests()
    {
        _service = new ProcurementService(_requisitionRepository.Object);
        _requisitionRepository.Setup(r => r.AddStatusHistory(It.IsAny<RequisitionStatusHistory>()));
        _requisitionRepository.Setup(r => r.AddProcurementRecord(It.IsAny<RequisitionProcurementRecord>()));
    }

    private static RequisitionItem NewItem(int quantity, decimal? price) => new()
    {
        Id = Guid.NewGuid(),
        ItemName = "Laptop",
        Quantity = quantity,
        CategoryItem = new CategoryItem { Name = "Laptop", Price = price },
    };

    private static Requisition NewRequisition(RequisitionStatus status, params RequisitionItem[] items)
    {
        var requisition = new Requisition { Status = status };
        requisition.Items.AddRange(items);
        return requisition;
    }

    private static void ApplyPartialApproval(Requisition requisition, RequisitionItem item, int approvedQuantity)
    {
        requisition.ApprovalProcess = new RequisitionApprovalProcess
        {
            StageInstances =
            {
                new RequisitionApproval
                {
                    Actions =
                    {
                        new RequisitionApprovalAction
                        {
                            ActionType = ApprovalActionType.PartialApprove,
                            PartialDecisions =
                            {
                                new PartialApprovalDecision { RequisitionItemId = item.Id, ApprovedQuantity = approvedQuantity },
                            },
                        },
                    },
                },
            },
        };
    }

    [Fact]
    public void StartProcessing_NotApprovedStatus_ThrowsConflict()
    {
        var requisition = NewRequisition(RequisitionStatus.UnderReview, NewItem(5, 100m));

        var act = () => _service.StartProcessing(requisition, _actorId, ActorName, ActorRole, null);

        act.Should().Throw<ConflictException>();
    }

    [Fact]
    public void StartProcessing_MissingItemPrice_ThrowsValidationNamingItem()
    {
        var requisition = NewRequisition(RequisitionStatus.Approved, NewItem(5, null));

        var act = () => _service.StartProcessing(requisition, _actorId, ActorName, ActorRole, null);

        act.Should().Throw<ValidationException>().Which.Errors.Should().Contain(e => e.ErrorMessage.Contains("Laptop"));
    }

    [Fact]
    public void StartProcessing_FullyApproved_ComputesTotalFromFullQuantity()
    {
        var item = NewItem(quantity: 3, price: 100m);
        var requisition = NewRequisition(RequisitionStatus.Approved, item);

        _service.StartProcessing(requisition, _actorId, ActorName, ActorRole, "starting");

        requisition.Status.Should().Be(RequisitionStatus.InProcurement);
        _requisitionRepository.Verify(r => r.AddProcurementRecord(It.Is<RequisitionProcurementRecord>(
            rec => rec.ActionType == ProcurementActionType.StartProcessing && rec.TotalProcurementAmount == 300m)), Times.Once);
    }

    [Fact]
    public void StartProcessing_PartiallyApproved_UsesPartialApprovalDecisionCeilingNotFullQuantity()
    {
        var item = NewItem(quantity: 10, price: 50m);
        var requisition = NewRequisition(RequisitionStatus.PartiallyApproved, item);
        ApplyPartialApproval(requisition, item, approvedQuantity: 4);

        _service.StartProcessing(requisition, _actorId, ActorName, ActorRole, null);

        requisition.Status.Should().Be(RequisitionStatus.InProcurement);
        _requisitionRepository.Verify(r => r.AddProcurementRecord(It.Is<RequisitionProcurementRecord>(
            rec => rec.TotalProcurementAmount == 200m)), Times.Once); // 4 (approved, not 10 requested) x 50
    }

    [Fact]
    public void RecordFulfillment_WrongStatus_ThrowsConflict()
    {
        var requisition = NewRequisition(RequisitionStatus.Approved, NewItem(5, 100m));

        var act = () => _service.RecordFulfillment(requisition, _actorId, ActorName, ActorRole, null, []);

        act.Should().Throw<ConflictException>();
    }

    [Fact]
    public void RecordFulfillment_OverCeiling_ThrowsValidationNamingItem()
    {
        var item = NewItem(quantity: 5, price: 100m);
        var requisition = NewRequisition(RequisitionStatus.InProcurement, item);

        var act = () => _service.RecordFulfillment(requisition, _actorId, ActorName, ActorRole, null, [(item.Id, 6)]);

        act.Should().Throw<ValidationException>().Which.Errors.Should().Contain(e => e.ErrorMessage.Contains("Laptop"));
    }

    [Fact]
    public void RecordFulfillment_PartialThenRemainder_TransitionsPartiallyFulfilledThenFulfilled()
    {
        var item = NewItem(quantity: 5, price: 100m);
        var requisition = NewRequisition(RequisitionStatus.InProcurement, item);

        _service.RecordFulfillment(requisition, _actorId, ActorName, ActorRole, null, [(item.Id, 2)]);
        requisition.Status.Should().Be(RequisitionStatus.PartiallyFulfilled);
        item.FulfilledQuantity.Should().Be(2);

        _service.RecordFulfillment(requisition, _actorId, ActorName, ActorRole, null, [(item.Id, 3)]);
        requisition.Status.Should().Be(RequisitionStatus.Fulfilled);
        item.FulfilledQuantity.Should().Be(5);
    }

    [Fact]
    public void RecordFulfillment_FullQuantityInOneAction_TransitionsDirectlyToFulfilled()
    {
        var item = NewItem(quantity: 4, price: 100m);
        var requisition = NewRequisition(RequisitionStatus.InProcurement, item);

        _service.RecordFulfillment(requisition, _actorId, ActorName, ActorRole, null, [(item.Id, 4)]);

        requisition.Status.Should().Be(RequisitionStatus.Fulfilled);
    }

    [Fact]
    public void Close_FromInProcurement_ThrowsConflict()
    {
        var requisition = NewRequisition(RequisitionStatus.InProcurement, NewItem(5, 100m));

        var act = () => _service.Close(requisition, _actorId, ActorName, ActorRole, null);

        act.Should().Throw<ConflictException>();
    }

    [Theory]
    [InlineData(RequisitionStatus.Fulfilled)]
    [InlineData(RequisitionStatus.PartiallyFulfilled)]
    public void Close_FromFulfilledOrPartiallyFulfilled_Succeeds(RequisitionStatus status)
    {
        var requisition = NewRequisition(status, NewItem(5, 100m));

        _service.Close(requisition, _actorId, ActorName, ActorRole, "closing");

        requisition.Status.Should().Be(RequisitionStatus.Closed);
    }
}
