using MacroX.Domain.Workbook;

namespace MacroX.Application.Workbooks;

/// <summary>
/// Libro abierto sobre una copia de trabajo. Guardar escribe en otra ruta.
/// </summary>
public interface IWorkbookSession : IAsyncDisposable
{
    IWorkbookContext Workbook { get; }

    Task SaveAsAsync(string outputPath, CancellationToken cancellationToken);
}
