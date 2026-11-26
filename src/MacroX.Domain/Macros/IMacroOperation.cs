using MacroX.Domain.Workbook;

namespace MacroX.Domain.Macros;

/// <summary>
/// Operación atómica del catálogo. Cada macro nueva es otra implementación de este contrato.
/// </summary>
public interface IMacroOperation
{
    string Id { get; }

    string DisplayName { get; }

    string Category { get; }

    IReadOnlyList<MacroParameterDefinition> Parameters { get; }

    Task<MacroResult> ExecuteAsync(
        IWorkbookContext workbook,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken);
}
