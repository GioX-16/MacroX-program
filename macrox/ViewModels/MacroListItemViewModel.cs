using System.Windows.Input;
using macrox.Mvvm;

namespace macrox.ViewModels;

/// <summary>
/// Una macro de la biblioteca, seleccionable al aplicar un libro.
/// </summary>
public sealed class MacroListItemViewModel : ObservableObject
{
    private bool _isSelected;
    private string _sourceNote;

    public MacroListItemViewModel(
        string id,
        string displayName,
        string category,
        string description,
        bool isSelected)
    {
        Id = id;
        DisplayName = displayName;
        Category = category;
        Description = description;
        _isSelected = isSelected;
        _sourceNote = "Incluida en el catálogo";
        DeleteCommand = new RelayCommand(() => { });
    }

    public ICommand DeleteCommand { get; private set; }

    public void SetDelete(Action delete)
    {
        ArgumentNullException.ThrowIfNull(delete);
        DeleteCommand = new RelayCommand(delete);
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string Category { get; }

    public string Description { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                RelayCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SourceNote
    {
        get => _sourceNote;
        set => SetProperty(ref _sourceNote, value);
    }
}
