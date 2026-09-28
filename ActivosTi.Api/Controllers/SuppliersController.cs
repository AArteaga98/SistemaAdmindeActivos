using ActivosTi.Api.Contracts;
using ActivosTi.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivosTi.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize(Roles = "Administrador,Operador")]
public sealed class SuppliersController : ControllerBase
{
    private readonly SupplierRepository _supplierRepository;

    public SuppliersController(
        SupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<SupplierDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<SupplierDto>>> List(
        CancellationToken cancellationToken)
    {
        var suppliers =
            await _supplierRepository.ListAsync(
                cancellationToken);

        return Ok(suppliers);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(CreateSupplierResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreateSupplierResponse>> Create(
        CreateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();

        var normalizedEmail =
            string.IsNullOrWhiteSpace(request.ContactEmail)
                ? null
                : request.ContactEmail.Trim();

        var id = await _supplierRepository.CreateAsync(
            request,
            cancellationToken);

        var response = new CreateSupplierResponse(
            Id: id,
            Name: normalizedName,
            ContactEmail: normalizedEmail,
            Purchase: request.Purchase,
            Maintenance: request.Maintenance,
            Rental: request.Rental
        );

        return Created("/api/suppliers", response);
    }
}