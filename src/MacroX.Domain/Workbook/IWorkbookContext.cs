namespace MacroX.Domain.Workbook;

/// <summary>
/// Libro de trabajo visto por las macros. Oculta el motor Excel concreto.
/// <see cref="FindSheet"/> no distingue mayúsculas.
/// </summary>
public interface IWorkbookContext
{
    /// <summary>Nombres de hoja en el orden del libro.</summary>
    IReadOnlyList<string> SheetNames { get; }

    IWorksheetContext? FindSheet(string name);

    /// <summary>Añade una hoja vacía al final. El nombre ya debe ser válido para Excel.</summary>
    IWorksheetContext AddSheet(string name);
}
