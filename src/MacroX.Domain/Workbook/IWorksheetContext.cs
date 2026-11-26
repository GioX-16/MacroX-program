namespace MacroX.Domain.Workbook;

/// <summary>
/// Hoja vista por una macro. Filas y columnas son 1-based, como en Excel.
/// </summary>
public interface IWorksheetContext
{
    string Name { get; }

    /// <summary>Última fila con celda almacenada. Cero si la hoja está vacía.</summary>
    int LastRow { get; }

    /// <summary>Última columna con celda almacenada. Cero si la hoja está vacía.</summary>
    int LastColumn { get; }

    object? GetCellValue(int row, int column);

    /// <summary>Fórmula A1, sin el signo =. Null si la celda no tiene fórmula.</summary>
    string? GetFormula(int row, int column);

    void SetCellValue(int row, int column, object? value);

    /// <summary>Escribe una fórmula A1, sin el signo =.</summary>
    void SetFormula(int row, int column, string formula);
}
