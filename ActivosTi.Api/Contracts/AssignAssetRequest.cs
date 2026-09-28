using System.ComponentModel.DataAnnotations;

namespace ActivosTi.Api.Contracts;

public sealed class AssignAssetRequest
{
    [Range( 1, long.MaxValue, ErrorMessage = "El colaborador no es válido.")]
    public long EmployeeId { get; init; }

    [StringLength(500, ErrorMessage = "Las observaciones no pueden exceder 500 caracteres.")]
    public string? Observations { get; init; }
}