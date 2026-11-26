namespace MacroX.Domain.Pipelines;

/// <summary>
/// Comprueba que un pipeline pueda ejecutarse. Uno inválido no entrega orden de ejecución.
/// </summary>
public sealed class PipelineValidator
{
    public PipelineValidationResult Validate(Pipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        var errors = new List<string>();
        if (pipeline.Steps.Count == 0)
        {
            errors.Add("El pipeline no tiene pasos.");
        }

        var orders = new HashSet<int>();
        foreach (var step in pipeline.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.OperationId))
            {
                errors.Add("Hay un paso sin identificador de macro.");
            }

            if (step.Order < 1)
            {
                errors.Add("El orden de cada paso empieza en 1.");
            }
            else if (!orders.Add(step.Order))
            {
                errors.Add($"El orden {step.Order} está repetido.");
            }
        }

        return errors.Count == 0
            ? PipelineValidationResult.Valid()
            : PipelineValidationResult.Invalid(errors);
    }

    /// <summary>
    /// Devuelve los pasos en orden de ejecución solo cuando el pipeline es válido.
    /// </summary>
    public bool TryGetExecutionOrder(
        Pipeline pipeline,
        out IReadOnlyList<PipelineStep> orderedSteps,
        out PipelineValidationResult validation)
    {
        validation = Validate(pipeline);
        if (!validation.IsValid)
        {
            orderedSteps = [];
            return false;
        }

        orderedSteps = pipeline.GetOrderedSteps();
        return true;
    }
}
