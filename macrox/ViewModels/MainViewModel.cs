using macrox.Mvvm;

namespace macrox.ViewModels
{
    /// <summary>
    /// ViewModel del shell principal: navegación entre secciones y estado global de la UI.
    /// </summary>
    public sealed class MainViewModel : ObservableObject
    {
        private NavigationSection _selectedSection;
        private bool _isBusy;
        private string _busyMessage = "Cargando…";
        private string _statusMessage = "Listo";

        public MainViewModel()
        {
            Sections = new List<NavigationSection>
            {
                new("home", "Inicio", "Vista general de la aplicación.", "\uE80F"),
                new("macros", "Macros", "Crea y ejecuta tus macros.", "\uE765"),
                new("excel", "Excel", "Importa y exporta libros.", "\uE8A5"),
                new("settings", "Configuración", "Preferencias locales.", "\uE713"),
            }.AsReadOnly();

            _selectedSection = Sections[0];
        }

        public string ApplicationTitle => "MacroX";

        public string ApplicationSubtitle => "Automatización local";

        public string DisplayVersion =>
            $"v{typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0"}";

        public IReadOnlyList<NavigationSection> Sections { get; }

        public NavigationSection SelectedSection
        {
            get => _selectedSection;
            set => SetProperty(ref _selectedSection, value);
        }

        /// <summary>
        /// Estado de carga global, visible para el usuario en el shell.
        /// </summary>
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
    }
}
