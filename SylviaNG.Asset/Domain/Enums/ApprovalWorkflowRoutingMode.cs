namespace RMS.Domain.Enums;

/// <summary>
/// UI label/default only - per the Feature 3 plan's "one unified engine" design, this field does not
/// branch the runtime engine. A stage with >1 required approver already behaves as parallel; a stage
/// with conditions already behaves as conditional. Kept purely for the admin builder's labeling.
/// </summary>
public enum ApprovalWorkflowRoutingMode
{
    Sequential = 0,
    Parallel = 1,
    Conditional = 2,
}
