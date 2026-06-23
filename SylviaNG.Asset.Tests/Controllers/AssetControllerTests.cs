using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SylviaNG.Assets.Application.Features.Assets.Commands.AssetCreate;
using SylviaNG.Assets.Application.Features.Assets.Commands.AssetDelete;
using SylviaNG.Assets.Application.Features.Assets.Models;
using SylviaNG.Assets.Application.Features.Assets.Queries.AssetGetAll;
using SylviaNG.Assets.Application.Features.Assets.Queries.AssetGetById;
using SylviaNG.Assets.Controllers;
using SylviaNG.Assets.Domain.Enums;

namespace SylviaNG.Assets.Tests.Controllers;

public class AssetControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly AssetController _controller;

    public AssetControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new AssetController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithAssets()
    {
        var expected = new List<AssetResponse>
        {
            new() { AssetId = 1, Name = "Laptop", Status = AssetStatusEnum.Assigned },
            new() { AssetId = 2, Name = "Chair", Status = AssetStatusEnum.Available }
        };
        _mediatorMock.Setup(m => m.Send(It.IsAny<AssetGetAllQuery>(), default)).ReturnsAsync(expected);

        var result = await _controller.GetAll();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetById_ShouldReturnOkWithAsset()
    {
        var expected = new AssetResponse { AssetId = 1, Name = "Laptop" };
        _mediatorMock.Setup(m => m.Send(It.IsAny<AssetGetByIdQuery>(), default)).ReturnsAsync(expected);

        var result = await _controller.GetById(1);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Create_WithValidRequest_ShouldReturnCreatedId()
    {
        var request = new AssetCreateRequest { AssetCode = "IT-LAP-0099", Name = "New Laptop", SiteId = 1 };
        _mediatorMock.Setup(m => m.Send(It.IsAny<AssetCreateCommand>(), default)).ReturnsAsync(1L);

        var result = await _controller.Create(request);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(1L);
    }

    [Fact]
    public async Task Delete_WithValidId_ShouldReturnOk()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<AssetDeleteCommand>(), default)).ReturnsAsync(Unit.Value);
        var result = await _controller.Delete(1);
        result.Should().BeOfType<OkResult>();
    }
}