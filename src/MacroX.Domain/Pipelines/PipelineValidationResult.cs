namespace MacroX.Domain.Pipelines;

/// <summary>
/// Resultado inmutable de validar un pipeline antes de ejecutarlo.
/// </summary>
public sealed class PipelineValidationResult
{
    private PipelineValidationResult(bool isValid, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public bool IsValid { get; }

    public IReadOnlyList<string> Errors { get; }

    public static PipelineValidationResult Valid() => new(true, []);

    public static PipelineValidationResult Invalid(IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
        {
            throw new ArgumentException("Un resultado inválido necesita al menos un error.", nameof(errors));
        }

        return new PipelineValidationResult(false, errors.ToArray());
    }
}
