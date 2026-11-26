using System.Globalization;
using MacroX.Domain.Workbook;

namespace MacroX.Application.Workbooks;

/// <summary>
/// Lee una hoja para mostrarla. No modifica el archivo.
/// </summary>
public sealed class WorkbookPreviewService
{
    public const int MaxRows = 300;

    private readonly IWorkbookSessionFactory _files;

    public WorkbookPreviewService(IWorkbookSessionFactory files)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public async Task<SheetPreview> ReadAsync(
        string sourcePath,
        string? sheetName,
        CancellationToken cancellationToken)
    {
        await using var session = await _files.OpenCopyAsync(sourcePath, cancellationToken).ConfigureAwait(false);
        var workbook = session.Workbook;
        if (workbook.SheetNames.Count == 0)
        {
            throw new InvalidOperationException("El libro no tiene hojas.");
        }

        var resolvedName = string.IsNullOrWhiteSpace(sheetName) ? workbook.SheetNames[0] : sheetName.Trim();
        var sheet = workbook.FindSheet(resolvedName)
            ?? throw new InvalidOperationException($"La hoja «{resolvedName}» no existe.");

        var lastRow = Math.Min(sheet.LastRow, MaxRows);
        var lastColumn = sheet.LastColumn;
        var rows = new List<IReadOnlyList<string>>();
        for (var row = 1; row <= lastRow; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = new string[lastColumn];
            for (var column = 1; column <= lastColumn; column++)
            {
                values[column - 1] = Display(sheet, row, column);
            }

            rows.Add(values);
        }

        return new SheetPreview(workbook.SheetNames.ToArray(), sheet.Name, rows);
    }

    private static string Display(IWorksheetContext sheet, int row, int column)
    {
        var formula = sheet.GetFormula(row, column);
        if (formula is not null)
        {
            return "=" + formula;
        }

        return sheet.GetCellValue(row, column) switch
        {
            null => string.Empty,
            string text => text,
            double number when number == Math.Truncate(number) => number.ToString("0", CultureInfo.InvariantCulture),
            double number => number.ToString("G", CultureInfo.InvariantCulture),
            bool flag => flag ? "VERDADERO" : "FALSO",
            DateTime date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            var other => Convert.ToString(other, CultureInfo.InvariantCulture) ?? string.Empty,
        };
    }
}
