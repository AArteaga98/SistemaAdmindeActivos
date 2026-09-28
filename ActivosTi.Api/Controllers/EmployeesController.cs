using ActivosTi.Api.Contracts;
using ActivosTi.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivosTi.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize(Roles = "Administrador,Operador")]
public sealed class EmployeesController : ControllerBase
{
    private readonly EmployeeRepository _employeeRepository;

    public EmployeesController(
        EmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EmployeeDto>),StatusCodes.Status200OK)]
    [ProducesResponseType( StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<EmployeeDto>>> List( CancellationToken cancellationToken)
    {
        var employees =
            await _employeeRepository.ListAsync(
                cancellationToken);

        return Ok(employees);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(CreateEmployeeResponse),StatusCodes.Status201Created)]
    [ProducesResponseType( StatusCodes.Status400BadRequest)]
    [ProducesResponseType( StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateEmployeeResponse>> Create( CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmployeeNumber =
            request.EmployeeNumber.Trim();

        var normalizedFullName =
            request.FullName.Trim();

        var normalizedEmail =
            request.Email.Trim();

        var id = await _employeeRepository.CreateAsync(
            request,
            cancellationToken);

        var response = new CreateEmployeeResponse(
            Id: id,
            EmployeeNumber: normalizedEmployeeNumber,
            FullName: normalizedFullName,
            Email: normalizedEmail,
            IsActive: true
        );

        return Created("/api/employees", response);
    }
}