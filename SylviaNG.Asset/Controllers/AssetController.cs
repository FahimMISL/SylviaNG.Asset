using MediatR;
using Microsoft.AspNetCore.Mvc;
using SylviaNG.Assets.Application.Features.Assets.Commands.AssetCreate;
using SylviaNG.Assets.Application.Features.Assets.Commands.AssetDelete;
using SylviaNG.Assets.Application.Features.Assets.Commands.AssetUpdate;
using SylviaNG.Assets.Application.Features.Assets.Models;
using SylviaNG.Assets.Application.Features.Assets.Queries.AssetGetAll;
using SylviaNG.Assets.Application.Features.Assets.Queries.AssetGetAllPaged;
using SylviaNG.Assets.Application.Features.Assets.Queries.AssetGetById;
using SylviaNG.Assets.SharedKernel.Pagination;

namespace SylviaNG.Assets.Controllers
{
    [ApiController]
    [Route("asset/asset")]
    public class AssetController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AssetController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<List<AssetResponse>>> GetAll()
        {
            var result = await _mediator.Send(new AssetGetAllQuery());
            return Ok(result);
        }

        [HttpGet("{assetId}")]
        public async Task<ActionResult<AssetResponse>> GetById(long assetId)
        {
            var result = await _mediator.Send(new AssetGetByIdQuery(assetId));
            return Ok(result);
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<AssetResponse>>> GetPaged([FromQuery] PagedRequest request)
        {
            var result = await _mediator.Send(new AssetGetAllPagedQuery(request));
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<long>> Create([FromBody] AssetCreateRequest request)
        {
            var id = await _mediator.Send(new AssetCreateCommand(request));
            return Ok(id);
        }

        [HttpPut("{assetId}")]
        public async Task<ActionResult> Update(long assetId, [FromBody] AssetUpdateRequest request)
        {
            await _mediator.Send(new AssetUpdateCommand(assetId, request));
            return Ok();
        }

        [HttpDelete("{assetId}")]
        public async Task<ActionResult> Delete(long assetId)
        {
            await _mediator.Send(new AssetDeleteCommand(assetId));
            return Ok();
        }
    }
}