using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Globalization;
using System.Windows.Input;
using MacroX.Application.Workbooks;
using MacroX.Domain.Macros;
using MacroX.Application.Workspace;
using macrox.Mvvm;
using macrox.Services;

namespace macrox.ViewModels;

/// <summary>
/// Hoja de Excel abierta, con la acción de aplicar las macros elegidas y guardar otro archivo.
/// </summary>
public sealed class ExcelWorkspaceViewModel : ObservableObject
{
    private readonly ObservableCollection<MacroListItemViewModel> _macros;
    private readonly WorkbookPreviewService _preview;
    private readonly ApplyMacrosUseCase _apply;
    private readonly IFileDialogService _dialogs;
    private readonly IPlatformWorkspace _workspace;
    private readonly IShellState _shell;
    private readonly Action _restoreMacros;
    private bool _restored;

    private string? _sourcePath;
    private string? _previewPath;
    private string? _resultPath;
    private string? _selectedSheetName;
    private string? _selectedSplitColumn;
    private bool _showApplyPanel;
    private bool _suppressSheetReload;
    private IReadOnlyList<string> _sheetNames = [];
    private DataView? _tableView;

    public ExcelWorkspaceViewModel(
        ObservableCollection<MacroListItemViewModel> macros,
        WorkbookPreviewService preview,
        ApplyMacrosUseCase apply,
        IFileDialogService dialogs,
        IPlatformWorkspace workspace,
        IShellState shell,
        Action restoreMacros)
    {
        _macros = macros ?? throw new ArgumentNullException(nameof(macros));
        _preview = preview ?? throw new ArgumentNullException(nameof(preview));
        _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _restoreMacros = restoreMacros ?? throw new ArgumentNullException(nameof(restoreMacros));
        foreach (var macro in _macros)
        {
            WatchMacro(macro);
        }

        _macros.CollectionChanged += (_, args) =>
        {
            OnPropertyChanged(nameof(HasNoMacros));
            if (args.NewItems is null)
            {
                return;
            }

            foreach (var item in args.NewItems)
            {
                if (item is MacroListItemViewModel macro)
                {
                    WatchMacro(macro);
                }
            }

            OnPropertyChanged(nameof(NeedsColumnChoice));
        };
        ImportedBooks = new ObservableCollection<ImportedBookViewModel>();
        RefreshBooks(null);
        OpenCommand = new AsyncRelayCommand(ImportAsync);
        ApplyCommand = new AsyncRelayCommand(ApplyAsync, CanApply);
        RestoreMacrosCommand = new RelayCommand(RestoreMacros);
    }

    public ObservableCollection<ImportedBookViewModel> ImportedBooks { get; }

    public bool HasNoImportedBooks => ImportedBooks.Count == 0;

    public ObservableCollection<MacroListItemViewModel> Macros => _macros;

    public bool HasNoMacros => _macros.Count == 0;

    public bool NeedsColumnChoice => _macros.Any(macro =>
        macro.IsSelected
        && string.Equals(macro.Id, SplitByColumnOperation.OperationId, StringComparison.OrdinalIgnoreCase));

    public string? SelectedSplitColumn
    {
        get => _selectedSplitColumn;
        set => SetProperty(ref _selectedSplitColumn, value);
    }

    public ICommand OpenCommand { get; }

    public ICommand ApplyCommand { get; }

    public ICommand RestoreMacrosCommand { get; }

    public IReadOnlyList<string> SheetNames
    {
        get => _sheetNames;
        private set => SetProperty(ref _sheetNames, value);
    }

    public IReadOnlyList<string> ColumnHeaders { get; private set; } = [];

    public DataView? TableView
    {
        get => _tableView;
        private set => SetProperty(ref _tableView, value);
    }

    public string? SelectedSheetName
    {
        get => _selectedSheetName;
        set
        {
            if (_suppressSheetReload)
            {
                _selectedSheetName = value;
                OnPropertyChanged();
                return;
            }

            if (!SetProperty(ref _selectedSheetName, value) || _previewPath is null || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            _ = ReloadSheetAsync(value);
        }
    }

    public bool ShowApplyPanel
    {
        get => _showApplyPanel;
        private set => SetProperty(ref _showApplyPanel, value);
    }

    public bool HasWorkbook => !string.IsNullOrWhiteSpace(_sourcePath);

    public bool HasNoWorkbook => !HasWorkbook;

    public string FileName =>
        string.IsNullOrWhiteSpace(_sourcePath) ? "Ningún Excel importado" : Path.GetFileName(_sourcePath);

    public string ResultLabel =>
        string.IsNullOrWhiteSpace(_resultPath)
            ? string.Empty
            : "Archivo nuevo en la plataforma: " + Path.GetFileName(_resultPath);

    private string _resultBanner = string.Empty;

    public string ResultBanner
    {
        get => _resultBanner;
        private set
        {
            if (SetProperty(ref _resultBanner, value))
            {
                OnPropertyChanged(nameof(HasBanner));
            }
        }
    }

    public bool HasBanner => !string.IsNullOrWhiteSpace(_resultBanner);

    public string? ResultPath
    {
        get => _resultPath;
        private set
        {
            if (SetProperty(ref _resultPath, value))
            {
                OnPropertyChanged(nameof(PreviewCaption));
                OnPropertyChanged(nameof(HasResult));
                OnPropertyChanged(nameof(ResultLabel));
            }
        }
    }

    public bool HasResult => !string.IsNullOrWhiteSpace(_resultPath);

    public string PreviewCaption => HasResult ? "Tabla del archivo nuevo, dentro de MacroX" : "Tabla importada";

    private bool CanApply() =>
        HasWorkbook
        && !_shell.IsBusy
        && !string.IsNullOrWhiteSpace(SelectedSheetName);

    private void RestoreMacros()
    {
        _restoreMacros();
        OnPropertyChanged(nameof(HasNoMacros));
        RelayCommand.RaiseCanExecuteChanged();
    }

    public async Task RestoreImportedAsync()
    {
        if (_restored || HasWorkbook)
        {
            return;
        }

        _restored = true;
        try
        {
            var books = _workspace.ListWorkbooks();
            if (books.Count == 0)
            {
                return;
            }

            await LoadStoredAsync(books[^1], "Se recuperó el Excel añadido a la plataforma.").ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _shell.StatusMessage = "No se pudo recuperar el Excel importado. " + ex.Message;
        }
    }

    private async Task ImportAsync()
    {
        var path = _dialogs.PickExcelToOpen();
        if (path is null)
        {
            return;
        }

        _shell.IsBusy = true;
        _shell.BusyMessage = "Añadiendo Excel…";
        try
        {
            var stored = await _workspace.ImportWorkbookAsync(path, CancellationToken.None).ConfigureAwait(true);
            _restored = true;
            await LoadStoredAsync(stored, Path.GetFileName(path) + " se añadió a la plataforma.").ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _shell.StatusMessage = "No se pudo importar el Excel. " + ex.Message;
        }
        finally
        {
            _shell.IsBusy = false;
            RelayCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task LoadStoredAsync(string storedPath, string status)
    {
        await ShowAsync(storedPath, null).ConfigureAwait(true);
        _sourcePath = storedPath;
        ResultPath = null;
        RefreshBooks(storedPath);
        NotifyFile();
        _shell.StatusMessage = status;
    }

    private void RefreshBooks(string? currentPath)
    {
        ImportedBooks.Clear();
        foreach (var path in _workspace.ListWorkbooks())
        {
            ImportedBooks.Add(new ImportedBookViewModel(
                path,
                string.Equals(path, currentPath, StringComparison.OrdinalIgnoreCase),
                OpenImportedBook,
                DeleteImportedBook));
        }

        OnPropertyChanged(nameof(HasNoImportedBooks));
    }

    private void OpenImportedBook(ImportedBookViewModel book) =>
        _ = LoadStoredAsync(book.StoredPath, book.FileName + " listo para aplicar macros.");

    private void DeleteImportedBook(ImportedBookViewModel book)
    {
        if (!_dialogs.Confirm($"¿Eliminar {book.FileName} de la plataforma? El archivo original no se modifica."))
        {
            return;
        }

        try
        {
            var wasCurrent = string.Equals(_sourcePath, book.StoredPath, StringComparison.OrdinalIgnoreCase);
            _workspace.DeleteWorkbook(book.StoredPath);
            if (wasCurrent)
            {
                ClearCurrentBook();
            }

            RefreshBooks(wasCurrent ? null : _sourcePath);
            var next = wasCurrent ? ImportedBooks.LastOrDefault() : null;
            if (next is not null)
            {
                _ = LoadStoredAsync(next.StoredPath, book.FileName + " se eliminó. Se muestra otro Excel de la plataforma.");
                return;
            }

            _shell.StatusMessage = book.FileName + " se eliminó de la plataforma.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _shell.StatusMessage = "No se pudo eliminar el Excel. " + ex.Message;
        }
    }

    private void ClearCurrentBook()
    {
        _sourcePath = null;
        _previewPath = null;
        _selectedSheetName = null;
        SheetNames = [];
        ColumnHeaders = [];
        TableView = null;
        ResultPath = null;
        ResultBanner = string.Empty;
        ShowApplyPanel = false;
        NotifyFile();
        OnPropertyChanged(nameof(SelectedSheetName));
    }

    private async Task ReloadSheetAsync(string sheetName)
    {
        if (_previewPath is null)
        {
            return;
        }

        _shell.IsBusy = true;
        _shell.BusyMessage = "Leyendo hoja…";
        try
        {
            await ShowAsync(_previewPath, sheetName).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _shell.StatusMessage = "No se pudo leer la hoja. " + ex.Message;
        }
        finally
        {
            _shell.IsBusy = false;
        }
    }

    private async Task ApplyAsync()
    {
        if (_sourcePath is null || string.IsNullOrWhiteSpace(SelectedSheetName))
        {
            return;
        }

        var selected = _macros.Where(macro => macro.IsSelected).Select(macro => macro.Id).ToArray();
        if (selected.Length == 0)
        {
            ResultBanner = _macros.Count == 0
                ? "No hay macros en la biblioteca. Pulsa Restaurar macros y vuelve a aplicar."
                : "Marca al menos una macro, por ejemplo Trim o Separar por columna.";
            _shell.StatusMessage = ResultBanner;
            return;
        }

        var needsColumn = selected.Contains(SplitByColumnOperation.OperationId, StringComparer.OrdinalIgnoreCase);
        if (needsColumn && string.IsNullOrWhiteSpace(SelectedSplitColumn))
        {
            ResultBanner = "Elige la columna para separar.";
            _shell.StatusMessage = ResultBanner;
            return;
        }

        var suggested = Path.GetFileNameWithoutExtension(_sourcePath) + "_macrox.xlsx";
        var downloads = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
        var output = _dialogs.PickExcelToSave(suggested, Directory.Exists(downloads) ? downloads : null);
        if (output is null)
        {
            ResultBanner = "Se canceló la descarga. El Excel original no se modificó.";
            _shell.StatusMessage = ResultBanner;
            return;
        }

        _shell.IsBusy = true;
        _shell.BusyMessage = "Aplicando macros…";
        try
        {
            var result = await _apply.ExecuteAsync(
                new ApplyMacrosRequest(
                    _sourcePath,
                    output,
                    SelectedSheetName,
                    selected,
                    needsColumn ? SelectedSplitColumn : null),
                CancellationToken.None).ConfigureAwait(true);
            var banner = result.IsSuccess
                ? result.Message + " Guardado en " + output
                : result.Message;
            ResultBanner = banner;
            _shell.StatusMessage = banner;
            if (!result.IsSuccess || result.OutputPath is null)
            {
                return;
            }

            ResultPath = result.OutputPath;
            await ShowAsync(result.OutputPath, SelectedSheetName).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ResultBanner = "No se pudo aplicar la macro. " + ex.Message;
            _shell.StatusMessage = ResultBanner;
        }
        finally
        {
            _shell.IsBusy = false;
            RelayCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task ShowAsync(string path, string? sheetName)
    {
        var preview = await _preview.ReadAsync(path, sheetName, CancellationToken.None).ConfigureAwait(true);
        _suppressSheetReload = true;
        SheetNames = preview.SheetNames;
        _selectedSheetName = preview.SheetName;
        OnPropertyChanged(nameof(SelectedSheetName));
        var table = BuildTable(preview.Rows);
        ColumnHeaders = table.headers;
        OnPropertyChanged(nameof(ColumnHeaders));
        TableView = table.table.DefaultView;
        EnsureSplitColumn();
        _previewPath = path;
        _suppressSheetReload = false;
    }

    private void WatchMacro(MacroListItemViewModel macro)
    {
        macro.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MacroListItemViewModel.IsSelected))
            {
                OnPropertyChanged(nameof(NeedsColumnChoice));
            }
        };
    }

    private void EnsureSplitColumn()
    {
        if (ColumnHeaders.Count == 0)
        {
            SelectedSplitColumn = null;
            return;
        }

        if (SelectedSplitColumn is not null
            && ColumnHeaders.Contains(SelectedSplitColumn, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        SelectedSplitColumn = ColumnHeaders.FirstOrDefault(header =>
                string.Equals(header, "Vendedor", StringComparison.OrdinalIgnoreCase))
            ?? ColumnHeaders[0];
    }

    private void NotifyFile()
    {
        OnPropertyChanged(nameof(HasWorkbook));
        OnPropertyChanged(nameof(HasNoWorkbook));
        OnPropertyChanged(nameof(FileName));
        OnPropertyChanged(nameof(PreviewCaption));
        RelayCommand.RaiseCanExecuteChanged();
    }

    private static (DataTable table, IReadOnlyList<string> headers) BuildTable(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var table = new DataTable();
        if (rows.Count == 0)
        {
            return (table, []);
        }

        var width = rows.Max(row => row.Count);
        var headers = new string[width];
        var headerRow = rows[0];
        for (var column = 0; column < width; column++)
        {
            table.Columns.Add("C" + column.ToString(CultureInfo.InvariantCulture));
            headers[column] = column < headerRow.Count && !string.IsNullOrWhiteSpace(headerRow[column])
                ? headerRow[column]
                : "Columna " + (column + 1).ToString(CultureInfo.InvariantCulture);
        }

        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var data = table.NewRow();
            var row = rows[rowIndex];
            for (var column = 0; column < width; column++)
            {
                data[column] = column < row.Count ? row[column] : string.Empty;
            }

            table.Rows.Add(data);
        }

        return (table, headers);
    }
}

/// <summary>
/// Excel copiado dentro de la plataforma. Se puede abrir o eliminar sin tocar el original.
/// </summary>
public sealed class ImportedBookViewModel
{
    public ImportedBookViewModel(
        string storedPath,
        bool isCurrent,
        Action<ImportedBookViewModel> open,
        Action<ImportedBookViewModel> delete)
    {
        StoredPath = storedPath;
        IsCurrent = isCurrent;
        FileName = Path.GetFileName(storedPath);
        OpenCommand = new RelayCommand(() => open(this));
        DeleteCommand = new RelayCommand(() => delete(this));
    }

    public string StoredPath { get; }

    public string FileName { get; }

    public bool IsCurrent { get; }

    public ICommand OpenCommand { get; }

    public ICommand DeleteCommand { get; }
}
