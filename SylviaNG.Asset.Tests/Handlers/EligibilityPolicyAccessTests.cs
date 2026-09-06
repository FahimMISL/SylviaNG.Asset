using FluentAssertions;
using Moq;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.EligibilityPolicies.Queries.GetEligibilityPolicyById;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Handlers;

/// <summary>Feature 10: GetEligibilityPolicyByIdQueryHandler used to call a company-agnostic
/// GetByIdAsync(id) - any authenticated user of any company could view any policy by GUID. Confirms
/// the fix: the repository is now called with the CALLER's own CompanyId, so a policy belonging to a
/// different company (simulated here by the mock simply returning null, exactly what the real
/// CompanyId-scoped query does for a mismatched company) 404s instead of leaking.</summary>
public class EligibilityPolicyAccessTests
{
    private readonly Mock<IEligibilityPolicyRepository> _policyRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _policyId = Guid.NewGuid();

    private GetEligibilityPolicyByIdQueryHandler BuildHandler() => new(_policyRepository.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_PolicyBelongsToCallersCompany_ReturnsIt()
    {
        _currentUser.Setup(c => c.CompanyId).Returns(_companyId);
        var policy = new EligibilityPolicy { Id = _policyId, CompanyId = _companyId, Name = "Laptop Policy" };
        _policyRepository.Setup(r => r.GetByIdAsync(_companyId, _policyId, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        var handler = BuildHandler();
        var result = await handler.Handle(new GetEligibilityPolicyByIdQuery(_policyId), CancellationToken.None);

        result.Name.Should().Be("Laptop Policy");
        _policyRepository.Verify(r => r.GetByIdAsync(_companyId, _policyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PolicyBelongsToAnotherCompany_404sInsteadOfLeaking()
    {
        _currentUser.Setup(c => c.CompanyId).Returns(_companyId);
        // The real CompanyId-scoped repository returns null for a policy owned by a different company -
        // this is the exact behavior that closes the cross-tenant leak.
        _policyRepository.Setup(r => r.GetByIdAsync(_companyId, _policyId, It.IsAny<CancellationToken>())).ReturnsAsync((EligibilityPolicy?)null);

        var handler = BuildHandler();
        var act = () => handler.Handle(new GetEligibilityPolicyByIdQuery(_policyId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
