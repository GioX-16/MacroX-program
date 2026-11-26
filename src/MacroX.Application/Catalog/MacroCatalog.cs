using MacroX.Domain.Macros;

namespace MacroX.Application.Catalog;

/// <summary>
/// Catálogo en memoria. Una macro nueva se registra al construir la aplicación.
/// </summary>
public sealed class MacroCatalog : IMacroCatalog
{
    private readonly Dictionary<string, IMacroOperation> _operations;

    public MacroCatalog(IEnumerable<IMacroOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        _operations = new Dictionary<string, IMacroOperation>(StringComparer.OrdinalIgnoreCase);
        foreach (var operation in operations)
        {
            ArgumentNullException.ThrowIfNull(operation);
            if (string.IsNullOrWhiteSpace(operation.Id))
            {
                throw new ArgumentException("La macro necesita un identificador.", nameof(operations));
            }

            if (!_operations.TryAdd(operation.Id, operation))
            {
                throw new ArgumentException($"La macro «{operation.Id}» está repetida.", nameof(operations));
            }
        }

        Operations = _operations.Values.ToArray();
    }

    public IReadOnlyList<IMacroOperation> Operations { get; }

    public IMacroOperation? Find(string operationId)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        return _operations.TryGetValue(operationId, out var operation) ? operation : null;
    }
}
