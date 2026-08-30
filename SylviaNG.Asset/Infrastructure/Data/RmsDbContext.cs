using Microsoft.EntityFrameworkCore;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Infrastructure.Data;

public class RmsDbContext : DbContext, IUnitOfWork
{
    public RmsDbContext(DbContextOptions<RmsDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RequisitionCategory> RequisitionCategories => Set<RequisitionCategory>();
    public DbSet<CategoryFieldDefinition> CategoryFieldDefinitions => Set<CategoryFieldDefinition>();
    public DbSet<CategoryFieldOption> CategoryFieldOptions => Set<CategoryFieldOption>();
    public DbSet<CategoryFieldValidationRule> CategoryFieldValidationRules => Set<CategoryFieldValidationRule>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<CategoryCostCenterLink> CategoryCostCenterLinks => Set<CategoryCostCenterLink>();
    public DbSet<CategoryTemplateVersion> CategoryTemplateVersions => Set<CategoryTemplateVersion>();
    public DbSet<Requisition> Requisitions => Set<Requisition>();
    public DbSet<RequisitionItem> RequisitionItems => Set<RequisitionItem>();
    public DbSet<RequisitionFieldValue> RequisitionFieldValues => Set<RequisitionFieldValue>();
    public DbSet<CategoryItem> CategoryItems => Set<CategoryItem>();
    public DbSet<RequisitionStatusHistory> RequisitionStatusHistories => Set<RequisitionStatusHistory>();
    public DbSet<RequisitionAttachment> RequisitionAttachments => Set<RequisitionAttachment>();

    // Feature 3 - Approval Workflow Management
    public DbSet<ApprovalWorkflow> ApprovalWorkflows => Set<ApprovalWorkflow>();
    public DbSet<ApprovalWorkflowVersion> ApprovalWorkflowVersions => Set<ApprovalWorkflowVersion>();
    public DbSet<ApprovalWorkflowCategoryLink> ApprovalWorkflowCategoryLinks => Set<ApprovalWorkflowCategoryLink>();
    public DbSet<ApprovalWorkflowStage> ApprovalWorkflowStages => Set<ApprovalWorkflowStage>();
    public DbSet<WorkflowApprover> WorkflowApprovers => Set<WorkflowApprover>();
    public DbSet<ApprovalWorkflowStageCondition> ApprovalWorkflowStageConditions => Set<ApprovalWorkflowStageCondition>();
    public DbSet<ApprovalWorkflowSlaConfiguration> ApprovalWorkflowSlaConfigurations => Set<ApprovalWorkflowSlaConfiguration>();
    public DbSet<RequisitionApprovalProcess> RequisitionApprovalProcesses => Set<RequisitionApprovalProcess>();
    public DbSet<RequisitionApproval> RequisitionApprovals => Set<RequisitionApproval>();
    public DbSet<RequisitionApprovalAssignment> RequisitionApprovalAssignments => Set<RequisitionApprovalAssignment>();
    public DbSet<RequisitionApprovalAction> RequisitionApprovalActions => Set<RequisitionApprovalAction>();
    public DbSet<PartialApprovalDecision> PartialApprovalDecisions => Set<PartialApprovalDecision>();
    public DbSet<ApprovalDelegation> ApprovalDelegations => Set<ApprovalDelegation>();

    // Feature 4 - Eligibility & Policy Management
    public DbSet<EligibilityPolicy> EligibilityPolicies => Set<EligibilityPolicy>();
    public DbSet<EligibilityPolicyCriterion> EligibilityPolicyCriteria => Set<EligibilityPolicyCriterion>();
    public DbSet<EligibilityPolicyReplacementRule> EligibilityPolicyReplacementRules => Set<EligibilityPolicyReplacementRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Scoped to this namespace only: RmsDbContext shares its assembly with
        // ApplicationDBContext (Asset/Employee), which also calls
        // ApplyConfigurationsFromAssembly. An unscoped scan here would pull in
        // Asset/Employee's IEntityTypeConfiguration too and, transitively via
        // Audit.DomainEvents, a keyless DomainEvent type this context never
        // declares a DbSet for.
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(RmsDbContext).Assembly,
            type => type.Namespace == "RMS.Infrastructure.Data.Configurations");
        base.OnModelCreating(modelBuilder);
    }
}
