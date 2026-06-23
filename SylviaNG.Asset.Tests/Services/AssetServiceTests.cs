using FluentAssertions;
using Moq;
using SylviaNG.Assets.Application.Common.Exceptions;
using SylviaNG.Assets.Application.Features.Assets.Models;
using SylviaNG.Assets.Application.Interfaces.Repositories;
using SylviaNG.Assets.Application.Services;
using SylviaNG.Assets.Domain.Entities;
using SylviaNG.Assets.Domain.Enums;
using SylviaNG.Assets.SharedKernel.Generic;

namespace SylviaNG.Assets.Tests.Services;

public class AssetServiceTests
{
    private readonly Mock<IAssetRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly AssetService _service;

    public AssetServiceTests()
    {
        _repositoryMock = new Mock<IAssetRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _service = new AssetService(_repositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldReturnAssetId()
    {
        var request = new AssetCreateRequest
        {
            AssetCode = "IT-LAP-0099", Name = "Dell Latitude 5540", SiteId = 1, Category = AssetCategoryEnum.IT
        };
        _repositoryMock.Setup(r => r.ExistsByAssetCodeAndSiteIdAsync(request.AssetCode, request.SiteId, null)).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Asset>())).Callback<Asset>(a => a.AssetId = 1);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _service.CreateAsync(request);

        result.Should().Be(1);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Asset>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateAssetCode_ShouldThrowDuplicateException()
    {
        var request = new AssetCreateRequest { AssetCode = "IT-LAP-0001", Name = "Dup", SiteId = 1 };
        _repositoryMock.Setup(r => r.ExistsByAssetCodeAndSiteIdAsync(request.AssetCode, request.SiteId, null)).ReturnsAsync(true);

        var act = () => _service.CreateAsync(request);

        await act.Should().ThrowAsync<DuplicateException>().WithMessage("*IT-LAP-0001*");
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_ShouldDeleteAndSave()
    {
        var entity = new Asset { AssetId = 1, Name = "Test Asset" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        await _service.DeleteAsync(1);

        _repositoryMock.Verify(r => r.Delete(entity), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ShouldThrowNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Asset?)null);
        var act = () => _service.DeleteAsync(999);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnMappedResponse()
    {
        var entity = new Asset { AssetId = 1, AssetCode = "IT-LAP-0001", Name = "Dell Latitude 5540", SiteId = 1, IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        var result = await _service.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.AssetId.Should().Be(1);
        result.AssetCode.Should().Be("IT-LAP-0001");
    }
}