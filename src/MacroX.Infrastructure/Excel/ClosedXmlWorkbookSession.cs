using System.Globalization;
using ClosedXML.Excel;
using MacroX.Application.Workbooks;
using MacroX.Domain.Workbook;

namespace MacroX.Infrastructure.Excel;

internal sealed class ClosedXmlWorkbookSession : IWorkbookSession
{
    private readonly string _tempPath;
    private readonly XLWorkbook _workbook;
    private readonly ClosedXmlWorkbook _context;

    public ClosedXmlWorkbookSession(string tempPath)
    {
        _tempPath = tempPath;
        _workbook = new XLWorkbook(tempPath);
        _context = new ClosedXmlWorkbook(_workbook);
    }

    public IWorkbookContext Workbook => _context;

    public async Task SaveAsAsync(string outputPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Falta la ruta de salida.", nameof(outputPath));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var fullOutput = Path.GetFullPath(outputPath);
        if (string.Equals(fullOutput, Path.GetFullPath(_tempPath), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("La salida no puede ser la copia temporal.");
        }

        var directory = Path.GetDirectoryName(fullOutput);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await Task.Run(() => _workbook.SaveAs(fullOutput), cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        _workbook.Dispose();
        try
        {
            if (File.Exists(_tempPath))
            {
                File.Delete(_tempPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return ValueTask.CompletedTask;
    }
}

internal sealed class ClosedXmlWorkbook : IWorkbookContext
{
    private readonly XLWorkbook _workbook;

    public ClosedXmlWorkbook(XLWorkbook workbook)
    {
        _workbook = workbook;
    }

    public IReadOnlyList<string> SheetNames =>
        _workbook.Worksheets.Select(sheet => sheet.Name).ToArray();

    public IWorksheetContext? FindSheet(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var worksheet = _workbook.Worksheets.FirstOrDefault(sheet =>
            string.Equals(sheet.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
        return worksheet is null ? null : new ClosedXmlWorksheet(worksheet);
    }

    public IWorksheetContext AddSheet(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("La hoja necesita un nombre.", nameof(name));
        }

        return new ClosedXmlWorksheet(_workbook.Worksheets.Add(name.Trim()));
    }
}

internal sealed class ClosedXmlWorksheet : IWorksheetContext
{
    private readonly IXLWorksheet _worksheet;

    public ClosedXmlWorksheet(IXLWorksheet worksheet)
    {
        _worksheet = worksheet;
    }

    public string Name => _worksheet.Name;

    public int LastRow => _worksheet.LastRowUsed()?.RowNumber() ?? 0;

    public int LastColumn => _worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

    public object? GetCellValue(int row, int column)
    {
        var cell = Cell(row, column);
        if (cell.IsEmpty())
        {
            return null;
        }

        return cell.Value.Type switch
        {
            XLDataType.Blank => null,
            XLDataType.Text => cell.Value.GetText(),
            XLDataType.Number => cell.Value.GetNumber(),
            XLDataType.Boolean => cell.Value.GetBoolean(),
            XLDataType.DateTime => cell.Value.GetDateTime(),
            XLDataType.TimeSpan => cell.Value.GetTimeSpan(),
            _ => string.IsNullOrEmpty(cell.GetFormattedString()) ? null : cell.GetFormattedString(),
        };
    }

    public string? GetFormula(int row, int column)
    {
        var cell = Cell(row, column);
        return cell.HasFormula ? cell.FormulaA1 : null;
    }

    public void SetCellValue(int row, int column, object? value)
    {
        var cell = Cell(row, column);
        if (value is null)
        {
            cell.Clear(XLClearOptions.Contents);
            return;
        }

        switch (value)
        {
            case string text:
                cell.Value = text;
                return;
            case bool flag:
                cell.Value = flag;
                return;
            case DateTime date:
                cell.Value = date;
                return;
            case double number:
                cell.Value = number;
                return;
            case float number:
                cell.Value = number;
                return;
            case decimal number:
                cell.Value = (double)number;
                return;
            case int number:
                cell.Value = number;
                return;
            case long number:
                cell.Value = number;
                return;
            default:
                cell.Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                return;
        }
    }

    public void SetFormula(int row, int column, string formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
        {
            throw new ArgumentException("Falta la fórmula.", nameof(formula));
        }

        Cell(row, column).FormulaA1 = formula.Trim().TrimStart('=');
    }

    private IXLCell Cell(int row, int column)
    {
        if (row < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }

        if (column < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }

        return _worksheet.Cell(row, column);
    }
}
