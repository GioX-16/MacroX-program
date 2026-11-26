using System.Windows.Controls;
using macrox.ViewModels;

namespace macrox.Views;

public partial class ExcelWorkspaceView : UserControl
{
    public ExcelWorkspaceView()
    {
        InitializeComponent();
    }

    private void SheetGrid_OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (DataContext is not ExcelWorkspaceViewModel workspace
            || !e.PropertyName.StartsWith('C')
            || !int.TryParse(e.PropertyName[1..], out var index)
            || index < 0
            || index >= workspace.ColumnHeaders.Count)
        {
            return;
        }

        e.Column.Header = workspace.ColumnHeaders[index];
    }
}
