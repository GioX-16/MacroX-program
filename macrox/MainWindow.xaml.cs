using System.Windows;
using macrox.Interop;
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
            SourceInitialized += (_, _) => DarkTitleBar.Apply(this);
        }

        private void ListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }
    }
}
