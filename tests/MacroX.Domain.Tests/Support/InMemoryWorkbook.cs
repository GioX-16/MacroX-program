using MacroX.Domain.Workbook;

namespace MacroX.Domain.Tests.Support;

/// <summary>
/// Libro en memoria para probar macros sin abrir un archivo.
/// </summary>
internal sealed class InMemoryWorkbook : IWorkbookContext
{
    private readonly List<InMemoryWorksheet> _sheets = [];

    public IReadOnlyList<string> SheetNames => _sheets.Select(sheet => sheet.Name).ToArray();

    public InMemoryWorksheet AddSheet(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("La hoja necesita un nombre.", nameof(name));
        }

        var sheet = new InMemoryWorksheet(name.Trim());
        _sheets.Add(sheet);
        return sheet;
    }

    IWorksheetContext IWorkbookContext.AddSheet(string name) => AddSheet(name);

    public IWorksheetContext? FindSheet(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _sheets.FirstOrDefault(sheet =>
            string.Equals(sheet.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}

internal sealed class InMemoryWorksheet : IWorksheetContext
{
    private readonly Dictionary<(int Row, int Column), object?> _cells = [];
    private readonly Dictionary<(int Row, int Column), string> _formulas = [];

    public InMemoryWorksheet(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public int LastRow => Math.Max(MaxIndex(_cells.Keys, static cell => cell.Row), MaxIndex(_formulas.Keys, static cell => cell.Row));

    public int LastColumn => Math.Max(MaxIndex(_cells.Keys, static cell => cell.Column), MaxIndex(_formulas.Keys, static cell => cell.Column));

    public object? GetCellValue(int row, int column)
    {
        EnsureCoordinate(row, column);
        return _cells.TryGetValue((row, column), out var value) ? value : null;
    }

    public string? GetFormula(int row, int column)
    {
        EnsureCoordinate(row, column);
        return _formulas.TryGetValue((row, column), out var formula) ? formula : null;
    }

    public void MarkFormula(int row, int column, string formula = "IF(TRUE,\"Sí\",\"No\")") =>
        SetFormula(row, column, formula);

    public void SetFormula(int row, int column, string formula)
    {
        EnsureCoordinate(row, column);
        if (string.IsNullOrWhiteSpace(formula))
        {
            throw new ArgumentException("Falta la fórmula.", nameof(formula));
        }

        _formulas[(row, column)] = formula.Trim().TrimStart('=');
    }

    private static int MaxIndex(
        IEnumerable<(int Row, int Column)> keys,
        Func<(int Row, int Column), int> select)
    {
        var max = 0;
        foreach (var key in keys)
        {
            var value = select(key);
            if (value > max)
            {
                max = value;
            }
        }

        return max;
    }

    public void SetCellValue(int row, int column, object? value)
    {
        EnsureCoordinate(row, column);
        if (value is null)
        {
            _cells.Remove((row, column));
            return;
        }

        _cells[(row, column)] = value;
    }

    private static void EnsureCoordinate(int row, int column)
    {
        if (row < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }

        if (column < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }
    }
}
