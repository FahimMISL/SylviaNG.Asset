using FluentAssertions;
using SylviaNG.Assets.Application.Features.Assets.Commands.AssetCreate;
using SylviaNG.Assets.Application.Features.Assets.Models;

namespace SylviaNG.Assets.Tests.Validators;

public class AssetCreateValidatorTests
{
    private readonly AssetCreateValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_ShouldHaveNoErrors()
    {
        var command = new AssetCreateCommand(new AssetCreateRequest
        {
            AssetCode = "IT-LAP-0099", Name = "Dell Latitude 5540", SiteId = 1, PurchaseValue = 1450m
        });

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyAssetCode_ShouldHaveError()
    {
        var command = new AssetCreateCommand(new AssetCreateRequest { AssetCode = "", Name = "Laptop", SiteId = 1 });

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Request.AssetCode");
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldHaveError()
    {
        var command = new AssetCreateCommand(new AssetCreateRequest { AssetCode = "IT-LAP-0099", Name = "", SiteId = 1 });

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Request.Name");
    }

    [Fact]
    public void Validate_WithZeroSiteId_ShouldHaveError()
    {
        var command = new AssetCreateCommand(new AssetCreateRequest { AssetCode = "IT-LAP-0099", Name = "Laptop", SiteId = 0 });

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Request.SiteId");
    }
}