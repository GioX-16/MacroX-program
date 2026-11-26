namespace MacroX.Domain.Macros;

/// <summary>
/// Resultado inmutable de ejecutar una macro sobre un libro.
/// </summary>
public sealed class MacroResult
{
    private MacroResult(
        bool isSuccess,
        string operationId,
        int affectedRows,
        int affectedCells,
        string message,
        IReadOnlyList<string> warnings)
    {
        IsSuccess = isSuccess;
        OperationId = operationId;
        AffectedRows = affectedRows;
        AffectedCells = affectedCells;
        Message = message;
        Warnings = warnings;
    }

    public bool IsSuccess { get; }

    public string OperationId { get; }

    /// <summary>Filas de datos en las que cambió al menos una celda. Un fallo deja este valor en cero.</summary>
    public int AffectedRows { get; }

    /// <summary>Celdas cuyo texto cambió. Un fallo deja este valor en cero.</summary>
    public int AffectedCells { get; }

    public string Message { get; }

    public IReadOnlyList<string> Warnings { get; }

    public static MacroResult Success(
        string operationId,
        int affectedRows,
        string message,
        IReadOnlyList<string>? warnings = null,
        int? affectedCells = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (affectedRows < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(affectedRows));
        }

        var cells = affectedCells ?? affectedRows;
        if (cells < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(affectedCells));
        }

        return new MacroResult(true, operationId, affectedRows, cells, message, Copy(warnings));
    }

    public static MacroResult Failure(
        string operationId,
        string message,
        IReadOnlyList<string>? warnings = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new MacroResult(false, operationId, 0, 0, message, Copy(warnings));
    }

    private static IReadOnlyList<string> Copy(IReadOnlyList<string>? warnings) =>
        warnings is null || warnings.Count == 0 ? [] : warnings.ToArray();
}
