using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace macrox.Services;

/// <summary>
/// Diálogos nativos de Windows para abrir un libro, guardar el resultado o elegir un módulo.
/// </summary>
public sealed class WpfFileDialogService : IFileDialogService
{
    public string? PickExcelToOpen()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Abrir Excel",
            Filter = "Libros de Excel (*.xlsx)|*.xlsx",
            CheckFileExists = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickExcelToSave(string suggestedFileName, string? initialDirectory)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Guardar Excel nuevo",
            Filter = "Libros de Excel (*.xlsx)|*.xlsx",
            FileName = suggestedFileName,
            AddExtension = true,
            DefaultExt = ".xlsx",
            OverwritePrompt = true,
        };

        if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
        {
            dialog.InitialDirectory = initialDirectory;
        }

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickMacroModule()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Importar macro",
            Filter = "Módulo VBA (*.bas)|*.bas|Todos los archivos (*.*)|*.*",
            CheckFileExists = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public bool Confirm(string message)
    {
        var answer = MessageBox.Show(
            message,
            "MacroX",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        return answer == MessageBoxResult.Yes;
    }
}
