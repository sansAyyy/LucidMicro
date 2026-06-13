using LucidMicro.BuildingBlocks.AspNetCore.Results;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;
using LucidMicro.Services.__ServiceName__.Application.Features.__FeatureName__.Abstractions;
using LucidMicro.Services.__ServiceName__.Application.Features.__FeatureName__.Dtos.Requests;
using LucidMicro.Services.__ServiceName__.Application.Features.__FeatureName__.Dtos.Responses;
using Microsoft.AspNetCore.Mvc;

namespace LucidMicro.Services.__ServiceName__.Api.Controllers;

[ApiController]
[Route("__Route__")]
public sealed class __FeatureName__Controller : ControllerBase
{
    private readonly I__FeatureName__ApplicationService ___featureNameCamel__;

    public __FeatureName__Controller(I__FeatureName__ApplicationService __featureNameCamel__)
    {
        ___featureNameCamel__ = __featureNameCamel__;
    }

    [HttpGet]
    public async Task<ActionResult<PageResult<__EntityName__Response>>> GetList(
        [FromQuery] Get__FeatureName__Request request,
        CancellationToken cancellationToken)
    {
        var result = await ___featureNameCamel__.GetListAsync(request, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<__EntityName__Response>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await ___featureNameCamel__.GetByIdAsync(id, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<__EntityName__Response>> Create(
        Create__EntityName__Request request,
        CancellationToken cancellationToken)
    {
        var result = await ___featureNameCamel__.CreateAsync(request, cancellationToken);

        return this.ToActionResult(
            result,
            item => CreatedAtAction(nameof(GetById), new { id = item.Id }, item));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(
        Guid id,
        Update__EntityName__Request request,
        CancellationToken cancellationToken)
    {
        var result = await ___featureNameCamel__.UpdateAsync(id, request, cancellationToken);

        return this.ToActionResult(result, NoContent);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await ___featureNameCamel__.DeleteAsync(id, cancellationToken);

        return this.ToActionResult(result, NoContent);
    }
}
