using System.Collections.ObjectModel;

namespace MacroX.Domain.Pipelines;

/// <summary>
/// Un paso del pipeline: qué macro se ejecuta, en qué orden y con qué parámetros.
/// </summary>
public sealed class PipelineStep
{
    public PipelineStep(string operationId, int order, IReadOnlyDictionary<string, object?>? parameters = null)
    {
        OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
        Order = order;
        Parameters = new ReadOnlyDictionary<string, object?>(
            parameters is null
                ? new Dictionary<string, object?>()
                : new Dictionary<string, object?>(parameters));
    }

    public string OperationId { get; }

    /// <summary>Posición explícita. El valor 1 es el primer paso que se ejecuta.</summary>
    public int Order { get; }

    public IReadOnlyDictionary<string, object?> Parameters { get; }
}
