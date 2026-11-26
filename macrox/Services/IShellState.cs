namespace macrox.Services;

/// <summary>
/// Estado global del shell que las secciones actualizan durante una lectura o una ejecución.
/// </summary>
public interface IShellState
{
    bool IsBusy { get; set; }

    string BusyMessage { get; set; }

    string StatusMessage { get; set; }
}
