namespace macrox.Services;

/// <summary>
/// Diálogos de archivo de la ventana. Las pruebas sustituyen esta interfaz.
/// </summary>
public interface IFileDialogService
{
    string? PickExcelToOpen();

    string? PickExcelToSave(string suggestedFileName, string? initialDirectory);

    string? PickMacroModule();

    bool Confirm(string message);
}
