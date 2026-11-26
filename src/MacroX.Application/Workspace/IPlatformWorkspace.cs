namespace MacroX.Application.Workspace;

/// <summary>
/// Carpeta de la plataforma. Los archivos importados se copian aquí; el original no se modifica.
/// </summary>
public interface IPlatformWorkspace
{
    Task<string> ImportMacroModuleAsync(string sourcePath, CancellationToken cancellationToken);

    Task<string> ImportWorkbookAsync(string sourcePath, CancellationToken cancellationToken);

    string CreateResultPath(string workbookFileName);

    IReadOnlyList<string> ListWorkbooks();

    IReadOnlyList<string> ListMacroModules();

    void DeleteWorkbook(string storedPath);

    void DeleteMacroModule(string storedPath);

    void HideCatalogMacro(string operationId);

    void RestoreCatalogMacro(string operationId);

    IReadOnlyList<string> ListHiddenCatalogMacros();
}
