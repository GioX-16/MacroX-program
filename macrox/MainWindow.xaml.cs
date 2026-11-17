using System.Windows;
using macrox.ViewModels;

namespace macrox
{
    /// <summary>
    /// Shell principal. Solo compone la vista con su ViewModel; la lógica vive en
    /// <see cref="MainViewModel"/>.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
