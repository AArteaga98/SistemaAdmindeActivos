using ActivosTi.Api.Contracts;
using ActivosTi.Api.Data;
using ActivosTi.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivosTi.Api.Controllers;

[ApiController]
[Route("api/assets")]
[Authorize(Roles = "Administrador,Operador")]
public sealed class AssetsController : ControllerBase
{
    private readonly AssetRepository _assetRepository;
    private readonly AssetOperationsRepository _assetOperationsRepository;
    public AssetsController(AssetRepository assetRepository, AssetOperationsRepository assetOperationsRepository)
    {
        _assetRepository = assetRepository;
        _assetOperationsRepository = assetOperationsRepository;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AssetDto>>> List( [FromQuery] AssetQuery query,CancellationToken cancellationToken)
    {
        var result = await _assetRepository.ListAsync(
            query,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CreateAssetResponse>> Create( CreateAssetRequest request,CancellationToken cancellationToken)
    {
        var actorUserId = User.GetRequiredUserId();

        var id = await _assetRepository.CreateAsync(
            request,
            actorUserId,
            cancellationToken);

        var response = new CreateAssetResponse(
            Id: id,
            AssetCode: request.AssetCode.Trim(),
            Status: "Disponible"
        );

        return Created("/api/assets", response);
    }

    [HttpPut("{assetId:long}")]
    public async Task<IActionResult> Update(long assetId,UpdateAssetRequest request,CancellationToken cancellationToken)
    {
        if (assetId <= 0)
        {
            return BadRequest(new
            {
                message = "El identificador del activo no es válido."
            });
        }

        var actorUserId = User.GetRequiredUserId();

        await _assetRepository.UpdateAsync(
            assetId,
            request,
            actorUserId,
            cancellationToken);

        return NoContent();
    }


    [HttpPost("{assetId:long}/assignments")]
    [ProducesResponseType(typeof(AssignAssetResponse),StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)] 
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssignAssetResponse>> Assign(long assetId,AssignAssetRequest request,CancellationToken cancellationToken)
    {
        if (assetId <= 0)
        {
            return BadRequest(new
            {
                message = "El identificador del activo no es válido."
            });
        }

        var actorUserId = User.GetRequiredUserId();

        var assignmentId =
            await _assetOperationsRepository.AssignAsync(
                assetId,
                request,
                actorUserId,
                cancellationToken);

        var response = new AssignAssetResponse(
            AssignmentId: assignmentId,
            AssetId: assetId,
            EmployeeId: request.EmployeeId,
            Status: "Asignado"
        );

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }

    [HttpPost("{assetId:long}/devolucion")]
    [ProducesResponseType(typeof(ReturnAssetResponse),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReturnAssetResponse>> Devolucion( long assetId,ReturnAssetRequest request, CancellationToken cancellationToken)
    {
        if (assetId <= 0)
        {
            return BadRequest(new
            {
                message = "El identificador del activo no es válido."
            });
        }

        var actorUserId =User.GetRequiredUserId();

        var assignmentId =
            await _assetOperationsRepository.ReturnAsync(
                assetId,
                request,
                actorUserId,
                cancellationToken);

        var response = new ReturnAssetResponse(
            AssignmentId: assignmentId,
            AssetId: assetId,
            Status: "Disponible",
            ReturnCondition:  request.ReturnCondition.Trim()
        );

        return Ok(response);
    }

    [HttpGet("{assetId:long}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<AssetMovementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType( StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AssetMovementDto>>> History( long assetId,CancellationToken cancellationToken)
    {
        if (assetId <= 0)
        {
            return BadRequest(new
            {
                message = "El identificador del activo no es válido."
            });
        }

        var movements =
            await _assetOperationsRepository.HistoryAsync(
                assetId,
                cancellationToken);

        return Ok(movements);
    }



}