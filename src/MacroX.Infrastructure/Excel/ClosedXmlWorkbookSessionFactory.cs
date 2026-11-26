using MacroX.Application.Workbooks;

namespace MacroX.Infrastructure.Excel;

/// <summary>
/// Abre una copia temporal del libro con ClosedXML. El archivo de entrada no se escribe.
/// </summary>
public sealed class ClosedXmlWorkbookSessionFactory : IWorkbookSessionFactory
{
    public async Task<IWorkbookSession> OpenCopyAsync(string sourcePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Falta la ruta del archivo.", nameof(sourcePath));
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("No se encontró el archivo de Excel.", sourcePath);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var directory = Path.Combine(Path.GetTempPath(), "MacroX");
        Directory.CreateDirectory(directory);
        var tempPath = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".xlsx");

        try
        {
            await using (var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            await using (var output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
            }

            return await Task.Run(
                () => (IWorkbookSession)new ClosedXmlWorkbookSession(tempPath),
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
