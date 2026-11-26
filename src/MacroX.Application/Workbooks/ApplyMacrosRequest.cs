namespace MacroX.Application.Workbooks;

/// <summary>
/// Petición para aplicar una o varias macros, en orden, sobre una hoja y guardar otro archivo.
/// </summary>
public sealed class ApplyMacrosRequest
{
    public ApplyMacrosRequest(
        string sourcePath,
        string outputPath,
        string sheetName,
        IReadOnlyList<string> operationIds,
        string? columnName = null)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Falta el archivo de origen.", nameof(sourcePath));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Falta el archivo de salida.", nameof(outputPath));
        }

        if (string.IsNullOrWhiteSpace(sheetName))
        {
            throw new ArgumentException("Falta la hoja.", nameof(sheetName));
        }

        SourcePath = sourcePath;
        OutputPath = outputPath;
        SheetName = sheetName.Trim();
        OperationIds = operationIds ?? throw new ArgumentNullException(nameof(operationIds));
        ColumnName = string.IsNullOrWhiteSpace(columnName) ? null : columnName.Trim();
    }

    public string SourcePath { get; }

    public string OutputPath { get; }

    public string SheetName { get; }

    public IReadOnlyList<string> OperationIds { get; }

    /// <summary>Columna usada por Separar por columna. Null si esa macro no está elegida.</summary>
    public string? ColumnName { get; }
}
