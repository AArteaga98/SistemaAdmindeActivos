using System.ComponentModel.DataAnnotations;

namespace ActivosTi.Api.Contracts;

public sealed class ReturnAssetRequest : IValidatableObject
{
    [Required(ErrorMessage = "La condición de devolución es obligatoria.")]
    [StringLength( 300,ErrorMessage = "La condición no puede exceder 300 caracteres.")]
    public string ReturnCondition { get; init; } = string.Empty;

    [StringLength( 500,ErrorMessage = "Las observaciones no pueden exceder 500 caracteres.")]
    public string? Observations { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ReturnCondition))
        {
            yield return new ValidationResult(
                "La condición de devolución no puede contener solamente espacios.",
                new[] { nameof(ReturnCondition) });
        }
    }
}