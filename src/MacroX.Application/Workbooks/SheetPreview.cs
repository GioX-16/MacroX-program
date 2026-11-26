namespace MacroX.Application.Workbooks;

/// <summary>
/// Vista de una hoja: nombres de hoja del libro y filas de la hoja activa, incluida la de encabezado.
/// </summary>
public sealed class SheetPreview
{
    public SheetPreview(
        IReadOnlyList<string> sheetNames,
        string sheetName,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        ArgumentNullException.ThrowIfNull(sheetNames);
        if (string.IsNullOrWhiteSpace(sheetName))
        {
            throw new ArgumentException("La hoja necesita un nombre.", nameof(sheetName));
        }

        SheetNames = sheetNames;
        SheetName = sheetName;
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
    }

    public IReadOnlyList<string> SheetNames { get; }

    public string SheetName { get; }

    public IReadOnlyList<IReadOnlyList<string>> Rows { get; }
}
