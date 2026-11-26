using MacroX.Domain.Macros;

namespace MacroX.Application.Catalog;

/// <summary>
/// Reconoce un módulo de macro conocido. No ejecuta el texto del archivo.
/// </summary>
public static class MacroModuleInspector
{
    public const int MaxCharacters = 256 * 1024;

    public static MacroImportResult Inspect(string? sourceText)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return MacroImportResult.Rejected("El archivo está vacío.");
        }

        if (sourceText.Length > MaxCharacters)
        {
            return MacroImportResult.Rejected("El archivo es demasiado grande para revisarlo.");
        }

        if (sourceText.Contains("LimpiarEspacios", StringComparison.Ordinal)
            && sourceText.Contains("WorksheetFunction.Trim", StringComparison.Ordinal))
        {
            return MacroImportResult.Linked(
                TrimOperation.OperationId,
                "LimpiarEspacios quedó vinculada a Trim. MacroX aplica esa operación y no ejecuta el VBA.");
        }

        if (sourceText.Contains("SepararPorColumna", StringComparison.Ordinal))
        {
            return MacroImportResult.Linked(
                SplitByColumnOperation.OperationId,
                "SepararPorColumna quedó vinculada a Separar por columna. MacroX aplica esa operación y no ejecuta el VBA.");
        }

        return MacroImportResult.Rejected(
            "Ese módulo no está en el catálogo. MacroX no ejecuta código VBA.");
    }
}

/// <summary>
/// Resultado de revisar un módulo importado.
/// </summary>
public sealed class MacroImportResult
{
    private MacroImportResult(bool isLinked, string? operationId, string message)
    {
        IsLinked = isLinked;
        OperationId = operationId;
        Message = message;
    }

    public bool IsLinked { get; }

    public string? OperationId { get; }

    public string Message { get; }

    public static MacroImportResult Linked(string operationId, string message) =>
        new(true, operationId, message);

    public static MacroImportResult Rejected(string message) =>
        new(false, null, message);
}
