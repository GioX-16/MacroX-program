using System.Text;
using MacroX.Domain.Workbook;

namespace MacroX.Domain.Macros;

/// <summary>
/// Quita espacios al inicio, al final y los espacios internos repetidos.
/// Equivale a TRIM de Excel. La fila 1 es encabezado y las fórmulas no se modifican.
/// </summary>
public sealed class TrimOperation : IMacroOperation
{
    public const string OperationId = "trim";
    public const string ColumnsParameter = "columns";
    public const string SheetParameter = "sheet";

    private static readonly IReadOnlyList<MacroParameterDefinition> ParameterList =
    [
        new(
            ColumnsParameter,
            "Columnas objetivo",
            MacroParameterKind.ColumnList,
            isRequired: true,
            "Nombres de columna, tomados de la fila de encabezado, que se van a recortar. No altera celdas con fórmula."),
        new(
            SheetParameter,
            "Hoja",
            MacroParameterKind.Text,
            isRequired: false,
            "Nombre de la hoja. Si se omite, se usa la primera."),
    ];

    public string Id => OperationId;

    public string DisplayName => "Trim";

    public string Category => "Normalización de texto";

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
        if (!TryReadColumns(parameters, out var columnNames, out var columnsError))
        {
            return MacroResult.Failure(OperationId, columnsError);
        }

        if (!TryResolveSheet(workbook, parameters, out var sheet, out var sheetError))
        {
            return MacroResult.Failure(OperationId, sheetError);
        }

        if (!TryMapColumns(sheet, columnNames, cancellationToken, out var columnIndexes, out var mapError))
        {
            return MacroResult.Failure(OperationId, mapError);
        }

        var affectedRows = 0;
        var affectedCells = 0;
        for (var row = 2; row <= sheet.LastRow; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cells = TrimRow(sheet, row, columnIndexes);
            if (cells > 0)
            {
                affectedRows++;
                affectedCells += cells;
            }
        }

        var message = affectedCells switch
        {
            0 => "Ninguna celda necesitaba recorte.",
            1 => "Se recortó 1 celda.",
            _ => $"Se recortaron espacios en {affectedCells} celdas.",
        };

        return MacroResult.Success(OperationId, affectedRows, message, affectedCells: affectedCells);
    }

    private static int TrimRow(IWorksheetContext sheet, int row, IReadOnlyList<int> columnIndexes)
    {
        var changed = 0;
        foreach (var column in columnIndexes)
        {
            if (sheet.GetFormula(row, column) is not null)
            {
                continue;
            }

            if (sheet.GetCellValue(row, column) is not string text)
            {
                continue;
            }

            var trimmed = CollapseSpaces(text);
            if (string.Equals(trimmed, text, StringComparison.Ordinal))
            {
                continue;
            }

            sheet.SetCellValue(row, column, trimmed);
            changed++;
        }

        return changed;
    }

    /// <summary>
    /// TRIM de Excel: quita espacios de los extremos y deja un solo espacio entre palabras.
    /// </summary>
    private static string CollapseSpaces(string text)
    {
        var builder = new StringBuilder(text.Length);
        var pendingSpace = false;
        foreach (var character in text)
        {
            if (character == ' ')
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static bool TryReadColumns(
        IReadOnlyDictionary<string, object?> parameters,
        out IReadOnlyList<string> columnNames,
        out string error)
    {
        columnNames = [];
        if (!parameters.TryGetValue(ColumnsParameter, out var raw) || raw is null)
        {
            error = "Falta el parámetro obligatorio «Columnas objetivo».";
            return false;
        }

        var names = new List<string>();
        switch (raw)
        {
            case string single when !string.IsNullOrWhiteSpace(single):
                names.Add(single.Trim());
                break;
            case string:
                error = "Falta el parámetro obligatorio «Columnas objetivo».";
                return false;
            case System.Collections.IEnumerable enumerable:
                foreach (var item in enumerable)
                {
                    if (item is not string text || string.IsNullOrWhiteSpace(text))
                    {
                        error = "«Columnas objetivo» debe ser una lista de nombres de columna.";
                        return false;
                    }

                    names.Add(text.Trim());
                }

                break;
            default:
                error = "«Columnas objetivo» debe ser una lista de nombres de columna.";
                return false;
        }

        if (names.Count == 0)
        {
            error = "Falta el parámetro obligatorio «Columnas objetivo».";
            return false;
        }

        columnNames = names
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        error = string.Empty;
        return true;
    }

    private static bool TryResolveSheet(
        IWorkbookContext workbook,
        IReadOnlyDictionary<string, object?> parameters,
        out IWorksheetContext sheet,
        out string error)
    {
        if (parameters.TryGetValue(SheetParameter, out var raw) && raw is not null && raw is not string { Length: 0 })
        {
            if (raw is not string name)
            {
                sheet = null!;
                error = "El parámetro «Hoja» debe ser texto.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(name))
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

    private static bool TryMapColumns(
        IWorksheetContext sheet,
        IReadOnlyList<string> columnNames,
        CancellationToken cancellationToken,
        out IReadOnlyList<int> columnIndexes,
        out string error)
    {
        var indexes = new List<int>(columnNames.Count);
        var missing = new List<string>();

        foreach (var name in columnNames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var index = FindColumn(sheet, name, cancellationToken);
            if (index is null)
            {
                missing.Add(name);
            }
            else
            {
                indexes.Add(index.Value);
            }
        }

        if (missing.Count > 0)
        {
            columnIndexes = [];
            error = missing.Count == 1
                ? $"La columna «{missing[0]}» no existe en la hoja «{sheet.Name}»."
                : $"Las columnas «{string.Join("», «", missing)}» no existen en la hoja «{sheet.Name}».";
            return false;
        }

        columnIndexes = indexes;
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
