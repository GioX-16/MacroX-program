using MacroX.Domain.Macros;

namespace MacroX.Application.Catalog;

/// <summary>
/// Biblioteca de macros disponibles. Resuelve cada una por su identificador.
/// </summary>
public interface IMacroCatalog
{
    IReadOnlyList<IMacroOperation> Operations { get; }

    IMacroOperation? Find(string operationId);
}
