using System.Collections.ObjectModel;
using MacroX.Application.Catalog;
using MacroX.Application.Workbooks;
using MacroX.Application.Workspace;
using MacroX.Domain.Macros;
using macrox.Mvvm;
using macrox.Services;

namespace macrox.ViewModels;

/// <summary>
/// ViewModel del shell principal: navegación entre secciones y estado global de la UI.
/// </summary>
public sealed class MainViewModel : ObservableObject, IShellState
{
    private readonly MacroLibraryViewModel _library;
    private readonly ExcelWorkspaceViewModel _excel;
    private NavigationSection _selectedSection;
    private object _currentPage;
    private bool _isBusy;
    private string _busyMessage = "Cargando…";
    private string _statusMessage = "Listo";

    public MainViewModel()
        : this(
            new MacroCatalog([new TrimOperation(), new SplitByColumnOperation()]),
            new UnavailableWorkbookSessionFactory(),
            new NullFileDialogService(),
            new NullPlatformWorkspace())
    {
    }

    public MainViewModel(
        IMacroCatalog catalog,
        IWorkbookSessionFactory files,
        IFileDialogService dialogs,
        IPlatformWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(workspace);

        var hidden = new HashSet<string>(workspace.ListHiddenCatalogMacros(), StringComparer.OrdinalIgnoreCase);
        var macros = new ObservableCollection<MacroListItemViewModel>(
            catalog.Operations
                .Where(operation => !hidden.Contains(operation.Id))
                .Select(CreateMacroItem));

        var preview = new WorkbookPreviewService(files);
        var apply = new ApplyMacrosUseCase(catalog, files);
        _library = new MacroLibraryViewModel(macros, catalog, CreateMacroItem, dialogs, workspace, this);
        _excel = new ExcelWorkspaceViewModel(
            macros,
            preview,
            apply,
            dialogs,
            workspace,
            this,
            _library.RestoreCatalogMacros);

        Sections = new List<NavigationSection>
        {
            new("home", "Inicio", "Vista general de la aplicación.", "\uE80F"),
            new("macros", "Macros", "Biblioteca de macros.", "\uE765"),
            new("excel", "Excel", "Importa un libro y aplica macros.", "\uE8A5"),
            new("settings", "Configuración", "Preferencias locales.", "\uE713"),
        }.AsReadOnly();

        _selectedSection = Sections[0];
        _currentPage = _selectedSection;
    }

    public string ApplicationTitle => "MacroX";

    public string ApplicationSubtitle => "Automatización local";

    public string DisplayVersion =>
        $"v{typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0"}";

    public IReadOnlyList<NavigationSection> Sections { get; }

    public NavigationSection SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (SetProperty(ref _selectedSection, value))
            {
                ShowSelectedPage();
            }
        }
    }

    public object CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string BusyMessage
    {
        get => _busyMessage;
        set => SetProperty(ref _busyMessage, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private void ShowSelectedPage()
    {
        CurrentPage = _selectedSection.Key switch
        {
            "macros" => _library,
            "excel" => _excel,
            _ => _selectedSection,
        };

        if (_selectedSection.Key == "excel")
        {
            _ = _excel.RestoreImportedAsync();
        }
    }

    private static MacroListItemViewModel CreateMacroItem(IMacroOperation operation) =>
        new(
            operation.Id,
            operation.DisplayName,
            operation.Category,
            Describe(operation),
            isSelected: string.Equals(operation.Id, TrimOperation.OperationId, StringComparison.OrdinalIgnoreCase));

    private static string Describe(IMacroOperation operation)
    {
        if (string.Equals(operation.Id, TrimOperation.OperationId, StringComparison.OrdinalIgnoreCase))
        {
            return "Quita espacios al inicio, al final y los espacios dobles internos. No modifica fórmulas.";
        }

        return operation.Parameters.Count == 0
            ? operation.Category
            : operation.Parameters[0].Description;
    }

    private sealed class UnavailableWorkbookSessionFactory : IWorkbookSessionFactory
    {
        public Task<IWorkbookSession> OpenCopyAsync(string sourcePath, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("El motor de Excel no está disponible en este contexto.");
    }

    private sealed class NullFileDialogService : IFileDialogService
    {
        public string? PickExcelToOpen() => null;

        public string? PickExcelToSave(string suggestedFileName, string? initialDirectory) => null;

        public string? PickMacroModule() => null;

        public bool Confirm(string message) => false;
    }

    private sealed class NullPlatformWorkspace : IPlatformWorkspace
    {
        public Task<string> ImportMacroModuleAsync(string sourcePath, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("La plataforma no está disponible en este contexto.");

        public Task<string> ImportWorkbookAsync(string sourcePath, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("La plataforma no está disponible en este contexto.");

        public string CreateResultPath(string workbookFileName) =>
            throw new InvalidOperationException("La plataforma no está disponible en este contexto.");

        public IReadOnlyList<string> ListWorkbooks() => [];

        public IReadOnlyList<string> ListMacroModules() => [];

        public void DeleteWorkbook(string storedPath)
        {
        }

        public void DeleteMacroModule(string storedPath)
        {
        }

        public void HideCatalogMacro(string operationId)
        {
        }

        public void RestoreCatalogMacro(string operationId)
        {
        }

        public IReadOnlyList<string> ListHiddenCatalogMacros() => [];
    }
}
