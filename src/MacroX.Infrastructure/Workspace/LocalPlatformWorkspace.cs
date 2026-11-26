using MacroX.Application.Workspace;

namespace MacroX.Infrastructure.Workspace;

/// <summary>
/// Guarda macros, libros y resultados bajo la carpeta local de la aplicación.
/// </summary>
public sealed class LocalPlatformWorkspace : IPlatformWorkspace
{
    private readonly string _root;

    public LocalPlatformWorkspace(string? root = null)
    {
        _root = string.IsNullOrWhiteSpace(root)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MacroX")
            : Path.GetFullPath(root);
    }

    public Task<string> ImportMacroModuleAsync(string sourcePath, CancellationToken cancellationToken) =>
        ImportAsync(sourcePath, Path.Combine(_root, "macros"), ".bas", cancellationToken);

    public Task<string> ImportWorkbookAsync(string sourcePath, CancellationToken cancellationToken) =>
        ImportAsync(sourcePath, Path.Combine(_root, "books"), ".xlsx", cancellationToken);

    public string CreateResultPath(string workbookFileName)
    {
        var stem = Path.GetFileNameWithoutExtension(Sanitize(workbookFileName, ".xlsx"));
        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = "resultado";
        }

        return NextAvailable(Path.Combine(_root, "results"), stem + "_macrox.xlsx");
    }

    public IReadOnlyList<string> ListWorkbooks()
    {
        var directory = Path.Combine(_root, "books");
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory.GetFiles(directory, "*.xlsx")
            .OrderBy(path => File.GetLastWriteTimeUtc(path))
            .ToArray();
    }

    public IReadOnlyList<string> ListMacroModules()
    {
        var directory = Path.Combine(_root, "macros");
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory.GetFiles(directory, "*.bas")
            .OrderBy(path => File.GetLastWriteTimeUtc(path))
            .ToArray();
    }

    public void DeleteWorkbook(string storedPath) =>
        DeleteInside(storedPath, Path.Combine(_root, "books"));

    public void DeleteMacroModule(string storedPath) =>
        DeleteInside(storedPath, Path.Combine(_root, "macros"));

    public void HideCatalogMacro(string operationId)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            throw new ArgumentException("Falta el identificador de la macro.", nameof(operationId));
        }

        var id = operationId.Trim();
        if (ListHiddenCatalogMacros().Contains(id, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        Directory.CreateDirectory(_root);
        File.AppendAllLines(Path.Combine(_root, "hidden-macros.txt"), [id]);
    }

    public void RestoreCatalogMacro(string operationId)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            throw new ArgumentException("Falta el identificador de la macro.", nameof(operationId));
        }

        var path = Path.Combine(_root, "hidden-macros.txt");
        if (!File.Exists(path))
        {
            return;
        }

        var remaining = ListHiddenCatalogMacros()
            .Where(id => !string.Equals(id, operationId.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (remaining.Length == 0)
        {
            File.Delete(path);
            return;
        }

        File.WriteAllLines(path, remaining);
    }

    public IReadOnlyList<string> ListHiddenCatalogMacros()
    {
        var path = Path.Combine(_root, "hidden-macros.txt");
        if (!File.Exists(path))
        {
            return [];
        }

        return File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void DeleteInside(string storedPath, string directory)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            throw new ArgumentException("Falta la ruta del archivo.", nameof(storedPath));
        }

        var fullPath = Path.GetFullPath(storedPath);
        var fullDirectory = Path.GetFullPath(directory);
        var prefix = fullDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Solo se pueden eliminar archivos añadidos a la plataforma.");
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private static async Task<string> ImportAsync(
        string sourcePath,
        string directory,
        string requiredExtension,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Falta la ruta del archivo.", nameof(sourcePath));
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("No se encontró el archivo.", sourcePath);
        }

        var extension = Path.GetExtension(sourcePath);
        if (!string.Equals(extension, requiredExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"El archivo tiene que ser {requiredExtension}.");
        }

        var fullSource = Path.GetFullPath(sourcePath);
        var fullDirectory = Path.GetFullPath(directory);
        if (fullSource.StartsWith(fullDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return fullSource;
        }

        var destination = NextAvailable(directory, Sanitize(Path.GetFileName(fullSource), requiredExtension));
        Directory.CreateDirectory(directory);

        await using (var input = new FileStream(fullSource, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        await using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
        }

        return destination;
    }

    private static string NextAvailable(string directory, string fileName)
    {
        Directory.CreateDirectory(directory);
        var candidate = Path.Combine(directory, fileName);
        if (!File.Exists(candidate))
        {
            return candidate;
        }

        var stem = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        for (var index = 2; index < 1000; index++)
        {
            candidate = Path.Combine(directory, $"{stem} ({index}){extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new IOException("No hay un nombre libre para guardar el archivo en la plataforma.");
    }

    private static string Sanitize(string fileName, string fallbackExtension)
    {
        var name = Path.GetFileName(fileName);
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }

        if (string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(name)))
        {
            name = "archivo" + fallbackExtension;
        }

        return name;
    }
}
