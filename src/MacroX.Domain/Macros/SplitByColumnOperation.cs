using System.Text;
using MacroX.Domain.Workbook;

namespace MacroX.Domain.Macros;

/// <summary>
/// Crea una hoja por cada valor distinto de una columna.
/// Copia encabezado y filas. Si existe la columna Total, añade la suma al final.
/// </summary>
public sealed class SplitByColumnOperation : IMacroOperation
{
    public const string OperationId = "split-by-column";
    public const string ColumnParameter = "column";
    public const string SheetParameter = "sheet";

    private static readonly IReadOnlyList<MacroParameterDefinition> ParameterList =
    [
        new(
            ColumnParameter,
            "Columna",
            MacroParameterKind.Text,
            isRequired: true,
            "Nombre de la columna cuyos valores distintos generan una hoja cada uno. Si hay una columna Total, cada hoja lleva la suma al final."),
        new(
            SheetParameter,
            "Hoja",
            MacroParameterKind.Text,
            isRequired: false,
            "Nombre de la hoja de origen. Si se omite, se usa la primera."),
    ];

    public string Id => OperationId;

    public string DisplayName => "Separar por columna";

    public string Category => "Transformación";

    public IReadOnlyList<MacroParameterDefinition> Parameters => ParameterList;

    public Task<MacroResult> ExecuteAsync(
        IWorkbookContext workbook,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        ArgumentNullException.ThrowIfNull(parameters);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Execute(workbook, parameters, cancellationToken));
    }

    private MacroResult Execute(
        IWorkbookContext workbook,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        if (!TryReadColumn(parameters, out var columnName, out var columnError))
        {
            return MacroResult.Failure(OperationId, columnError);
        }

        if (!TryResolveSheet(workbook, parameters, out var source, out var sheetError))
        {
            return MacroResult.Failure(OperationId, sheetError);
        }

        if (source.LastRow < 2 || source.LastColumn < 1)
        {
            return MacroResult.Failure(OperationId, "No hay filas para separar.");
        }

        var column = FindColumn(source, columnName, cancellationToken);
        if (column is null)
        {
            return MacroResult.Failure(OperationId, $"La columna «{columnName}» no existe en la hoja «{source.Name}».");
        }

        var totalColumn = FindColumn(source, "Total", cancellationToken);
        var groups = GroupRows(source, column.Value, cancellationToken);
        if (groups.Count == 0)
        {
            return MacroResult.Failure(OperationId, "No hay filas para separar.");
        }

        var created = new List<string>(groups.Count);
        var copiedRows = 0;
        foreach (var group in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sheetName = UniqueSheetName(workbook, SheetName(group.Key));
            var target = workbook.AddSheet(sheetName);
            CopyRow(source, 1, target, 1, source.LastColumn);
            var targetRow = 2;
            foreach (var sourceRow in group.Value)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CopyRow(source, sourceRow, target, targetRow, source.LastColumn);
                targetRow++;
                copiedRows++;
            }

            if (totalColumn is > 0 && targetRow > 2)
            {
                if (totalColumn.Value > 1)
                {
                    target.SetCellValue(targetRow, totalColumn.Value - 1, "TOTAL");
                }

                var letter = ColumnLetter(totalColumn.Value);
                target.SetFormula(targetRow, totalColumn.Value, $"SUM({letter}2:{letter}{targetRow - 1})");
            }

            created.Add(sheetName);
        }

        var listed = created.Count <= 8
            ? string.Join(", ", created)
            : created.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " hojas";
        var message = created.Count == 1
            ? $"Se creó 1 hoja según «{columnName}»: {listed}."
            : $"Se crearon {created.Count} hojas según «{columnName}»: {listed}.";
        return MacroResult.Success(OperationId, copiedRows, message, affectedCells: copiedRows);
    }

    private static List<KeyValuePair<string, List<int>>> GroupRows(
        IWorksheetContext source,
        int column,
        CancellationToken cancellationToken)
    {
        var groups = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        for (var row = 2; row <= source.LastRow; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = source.GetCellValue(row, column) switch
            {
                null => "Sin_dato",
                string text when string.IsNullOrWhiteSpace(text) => "Sin_dato",
                string text => text.Trim(),
                var other => Convert.ToString(other, System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? "Sin_dato",
            };
            if (key.Length == 0)
            {
                key = "Sin_dato";
            }

            if (!groups.TryGetValue(key, out var rows))
            {
                rows = [];
                groups.Add(key, rows);
            }

            rows.Add(row);
        }

        return groups.ToList();
    }

    private static void CopyRow(
        IWorksheetContext source,
        int sourceRow,
        IWorksheetContext target,
        int targetRow,
        int lastColumn)
    {
        for (var column = 1; column <= lastColumn; column++)
        {
            var value = source.GetCellValue(sourceRow, column);
            if (value is not null)
            {
                target.SetCellValue(targetRow, column, value);
            }
        }
    }

    private static string SheetName(string key)
    {
        var builder = new StringBuilder(key.Length);
        foreach (var character in key.Trim())
        {
            builder.Append(character is '/' or '\\' or '?' or '*' or '[' or ']' or ':' ? '-' : character);
        }

        var name = builder.ToString().Trim().Trim('\'');
        if (name.Length == 0)
        {
            name = "Sin_dato";
        }

        return name.Length <= 31 ? name : name[..31];
    }

    private static string UniqueSheetName(IWorkbookContext workbook, string desired)
    {
        var name = desired;
        for (var suffix = 2; workbook.FindSheet(name) is not null; suffix++)
        {
            var ending = " (" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
            var stem = desired.Length + ending.Length <= 31
                ? desired
                : desired[..(31 - ending.Length)];
            name = stem + ending;
        }

        return name;
    }

    private static string ColumnLetter(int column)
    {
        var name = string.Empty;
        var current = column;
        while (current > 0)
        {
            var modulo = (current - 1) % 26;
            name = (char)('A' + modulo) + name;
            current = (current - 1) / 26;
        }

        return name;
    }

    private static bool TryReadColumn(
        IReadOnlyDictionary<string, object?> parameters,
        out string columnName,
        out string error)
    {
        columnName = string.Empty;
        if (!parameters.TryGetValue(ColumnParameter, out var raw) || raw is not string text || string.IsNullOrWhiteSpace(text))
        {
            error = "Elige la columna para separar.";
            return false;
        }

        columnName = text.Trim();
        error = string.Empty;
        return true;
    }

    private static bool TryResolveSheet(
        IWorkbookContext workbook,
        IReadOnlyDictionary<string, object?> parameters,
        out IWorksheetContext sheet,
        out string error)
    {
        if (parameters.TryGetValue(SheetParameter, out var raw) && raw is string { Length: > 0 } name)
        {
            var found = workbook.FindSheet(name.Trim());
            if (found is null)
            {
                sheet = null!;
                error = $"La hoja «{name.Trim()}» no existe.";
                return false;
            }

            sheet = found;
            error = string.Empty;
            return true;
        }

        if (workbook.SheetNames.Count == 0)
        {
            sheet = null!;
            error = "El libro no tiene hojas.";
            return false;
        }

        var first = workbook.FindSheet(workbook.SheetNames[0]);
        if (first is null)
        {
            sheet = null!;
            error = "No se pudo abrir la primera hoja del libro.";
            return false;
        }

        sheet = first;
        error = string.Empty;
        return true;
    }

    private static int? FindColumn(IWorksheetContext sheet, string header, CancellationToken cancellationToken)
    {
        for (var column = 1; column <= sheet.LastColumn; column++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (sheet.GetCellValue(1, column) is string text
                && string.Equals(text.Trim(), header, StringComparison.OrdinalIgnoreCase))
            {
                return column;
            }
        }

        return null;
    }
}
