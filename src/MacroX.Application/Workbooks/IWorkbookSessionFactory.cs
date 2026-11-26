namespace MacroX.Application.Workbooks;

/// <summary>
/// Abre una copia del archivo de entrada. El original no se abre para escritura.
/// </summary>
public interface IWorkbookSessionFactory
{
    Task<IWorkbookSession> OpenCopyAsync(string sourcePath, CancellationToken cancellationToken);
}
