using System.Collections.ObjectModel;

namespace MacroX.Domain.Pipelines;

/// <summary>
/// Secuencia ordenada de macros. El orden es parte del resultado: dos pipelines
/// con los mismos pasos en distinto orden no son la misma ejecución.
/// </summary>
public sealed class Pipeline
{
    private readonly ReadOnlyCollection<PipelineStep> _steps;

    public Pipeline(IReadOnlyList<PipelineStep> steps, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(steps);
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        _steps = new ReadOnlyCollection<PipelineStep>(steps.ToList());
    }

    public string? Name { get; }

    public IReadOnlyList<PipelineStep> Steps => _steps;

    /// <summary>Pasos listos para ejecutarse, del orden menor al mayor.</summary>
    public IReadOnlyList<PipelineStep> GetOrderedSteps() =>
        _steps
            .Select((step, index) => (step, index))
            .OrderBy(item => item.step.Order)
            .ThenBy(item => item.index)
            .Select(item => item.step)
            .ToArray();

    /// <summary>Firma estable del orden de ejecución, por ejemplo <c>1:trim|2:uppercase</c>.</summary>
    public string ExecutionSignature =>
        string.Join("|", GetOrderedSteps().Select(step => $"{step.Order}:{step.OperationId}"));
}
