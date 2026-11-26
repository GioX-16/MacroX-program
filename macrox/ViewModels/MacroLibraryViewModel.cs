using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using MacroX.Application.Catalog;
using MacroX.Application.Workspace;
using MacroX.Domain.Macros;
using macrox.Mvvm;
using macrox.Services;

namespace macrox.ViewModels;

/// <summary>
/// Biblioteca de macros. Importar un .bas solo vincula un módulo conocido; no ejecuta VBA.
/// </summary>
public sealed class MacroLibraryViewModel : ObservableObject
{
    private readonly IMacroCatalog _catalog;
    private readonly Func<IMacroOperation, MacroListItemViewModel> _createItem;
    private readonly IFileDialogService _dialogs;
    private readonly IPlatformWorkspace _workspace;
    private readonly IShellState _shell;

    public MacroLibraryViewModel(
        ObservableCollection<MacroListItemViewModel> macros,
        IMacroCatalog catalog,
        Func<IMacroOperation, MacroListItemViewModel> createItem,
        IFileDialogService dialogs,
        IPlatformWorkspace workspace,
        IShellState shell)
    {
        Macros = macros ?? throw new ArgumentNullException(nameof(macros));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _createItem = createItem ?? throw new ArgumentNullException(nameof(createItem));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        foreach (var macro in Macros)
        {
            AttachDelete(macro);
        }

        Macros.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoMacros));

        ImportedModules = new ObservableCollection<ImportedMacroViewModel>(
            workspace.ListMacroModules().Select(path => CreateImported(path, LinkLabel(path))));
        ImportCommand = new AsyncRelayCommand(ImportAsync);
        RestoreMacrosCommand = new RelayCommand(RestoreCatalogMacros);
    }

    public ObservableCollection<MacroListItemViewModel> Macros { get; }

    public ObservableCollection<ImportedMacroViewModel> ImportedModules { get; }

    public bool HasImportedModules => ImportedModules.Count > 0;

    public bool HasNoImportedModules => ImportedModules.Count == 0;

    public bool HasNoMacros => Macros.Count == 0;

    public ICommand RestoreMacrosCommand { get; }

    public void RestoreCatalogMacros()
    {
        try
        {
            var restored = 0;
            foreach (var id in _workspace.ListHiddenCatalogMacros().ToArray())
            {
                _workspace.RestoreCatalogMacro(id);
                if (Macros.Any(macro => string.Equals(macro.Id, id, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var operation = _catalog.Find(id);
                if (operation is null)
                {
                    continue;
                }

                var item = _createItem(operation);
                AttachDelete(item);
                Macros.Add(item);
                restored++;
            }

            _shell.StatusMessage = restored == 0
                ? "No había macros ocultas para restaurar."
                : "Las macros del catálogo volvieron a la biblioteca. Ya puedes aplicarlas.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _shell.StatusMessage = "No se pudieron restaurar las macros. " + ex.Message;
        }
    }

    public ICommand ImportCommand { get; }

    private async Task ImportAsync()
    {
        var path = _dialogs.PickMacroModule();
        if (path is null)
        {
            return;
        }

        _shell.IsBusy = true;
        _shell.BusyMessage = "Añadiendo macro…";
        try
        {
            var info = new FileInfo(path);
            if (info.Length > MacroModuleInspector.MaxCharacters)
            {
                _shell.StatusMessage = "El archivo es demasiado grande para añadirlo.";
                return;
            }

            var result = MacroModuleInspector.Inspect(await File.ReadAllTextAsync(path).ConfigureAwait(true));
            if (!result.IsLinked || result.OperationId is null)
            {
                _shell.StatusMessage = result.Message;
                return;
            }

            var stored = await _workspace.ImportMacroModuleAsync(path, CancellationToken.None).ConfigureAwait(true);
            var linkedName = _catalog.Find(result.OperationId)?.DisplayName ?? result.OperationId;
            var match = Macros.FirstOrDefault(macro =>
                string.Equals(macro.Id, result.OperationId, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                match.SourceNote = "Añadida a la plataforma desde " + info.Name;
            }

            RememberImport(stored, linkedName);
            _shell.StatusMessage = info.Name + " ya aparece en la biblioteca, vinculada a " + linkedName + ".";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _shell.StatusMessage = "No se pudo añadir la macro. " + ex.Message;
        }
        finally
        {
            _shell.IsBusy = false;
        }
    }

    private void RememberImport(string storedPath, string linkedName)
    {
        if (ImportedModules.Any(module => string.Equals(module.StoredPath, storedPath, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        ImportedModules.Add(CreateImported(storedPath, linkedName));
        OnPropertyChanged(nameof(HasImportedModules));
        OnPropertyChanged(nameof(HasNoImportedModules));
    }

    private string LinkLabel(string storedPath)
    {
        try
        {
            var result = MacroModuleInspector.Inspect(File.ReadAllText(storedPath));
            if (result.OperationId is null)
            {
                return "Sin vincular";
            }

            return _catalog.Find(result.OperationId)?.DisplayName ?? result.OperationId;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "Sin vincular";
        }
    }

    private ImportedMacroViewModel CreateImported(string storedPath, string linkedName) =>
        new(storedPath, linkedName, RemoveImported);

    private void RemoveImported(ImportedMacroViewModel module)
    {
        if (!_dialogs.Confirm($"¿Eliminar {module.FileName} de la biblioteca? El archivo original no se modifica."))
        {
            return;
        }

        try
        {
            _workspace.DeleteMacroModule(module.StoredPath);
            ImportedModules.Remove(module);
            OnPropertyChanged(nameof(HasImportedModules));
            OnPropertyChanged(nameof(HasNoImportedModules));
            _shell.StatusMessage = module.FileName + " se eliminó de la biblioteca.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _shell.StatusMessage = "No se pudo eliminar la macro. " + ex.Message;
        }
    }

    private void AttachDelete(MacroListItemViewModel macro) =>
        macro.SetDelete(() => RemoveCatalogMacro(macro));

    private void RemoveCatalogMacro(MacroListItemViewModel macro)
    {
        if (!_dialogs.Confirm($"¿Quitar {macro.DisplayName} de la biblioteca?"))
        {
            return;
        }

        try
        {
            _workspace.HideCatalogMacro(macro.Id);
            Macros.Remove(macro);
            _shell.StatusMessage = macro.DisplayName + " se quitó de la biblioteca.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _shell.StatusMessage = "No se pudo quitar la macro. " + ex.Message;
        }
    }
}

/// <summary>
/// Módulo .bas añadido a la plataforma y vinculado a una macro del catálogo.
/// </summary>
public sealed class ImportedMacroViewModel
{
    public ImportedMacroViewModel(string storedPath, string linkedMacro, Action<ImportedMacroViewModel> delete)
    {
        StoredPath = storedPath;
        FileName = Path.GetFileName(storedPath);
        LinkedMacro = linkedMacro;
        DeleteCommand = new RelayCommand(() => delete(this));
    }

    public string StoredPath { get; }

    public string FileName { get; }

    public string LinkedMacro { get; }

    public string Summary => "Vinculada a " + LinkedMacro;

    public ICommand DeleteCommand { get; }
}
