using System.Windows;
using MacroX.Application.Catalog;
using MacroX.Application.Workbooks;
using MacroX.Domain.Macros;
using MacroX.Infrastructure.Excel;
using MacroX.Infrastructure.Workspace;
using macrox.Services;
using macrox.ViewModels;

namespace macrox;

/// <summary>
/// Arranque de la aplicación. Aquí se compone el catálogo, el motor Excel y la ventana.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var catalog = new MacroCatalog([new TrimOperation(), new SplitByColumnOperation()]);
        var files = new ClosedXmlWorkbookSessionFactory();
        var workspace = new LocalPlatformWorkspace();
        var dialogs = new WpfFileDialogService();

        var window = new MainWindow
        {
            DataContext = new MainViewModel(catalog, files, dialogs, workspace),
        };
        window.Show();
    }
}
