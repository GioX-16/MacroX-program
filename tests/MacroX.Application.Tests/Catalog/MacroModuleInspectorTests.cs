using MacroX.Application.Catalog;
using MacroX.Domain.Macros;

namespace MacroX.Application.Tests.Catalog;

public class MacroModuleInspectorTests
{
    [Fact]
    public void Inspect_ModuloTrim_LoVinculaSinEjecutarlo()
    {
        const string module = """
            Attribute VB_Name = "ModTrim"
            Sub LimpiarEspacios()
                c.Value = Application.WorksheetFunction.Trim(c.Value)
            End Sub
            """;

        var result = MacroModuleInspector.Inspect(module);

        Assert.True(result.IsLinked);
        Assert.Equal(TrimOperation.OperationId, result.OperationId);
        Assert.Contains("no ejecuta", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Inspect_ModuloSepararPorColumna_LoVinculaSinEjecutarlo()
    {
        const string module = """
            Attribute VB_Name = "ModSepararPorColumna"
            Sub SepararPorColumna()
                nombreCol = InputBox("Escribe el nombre de la columna para separar:")
            End Sub
            """;

        var result = MacroModuleInspector.Inspect(module);

        Assert.True(result.IsLinked);
        Assert.Equal(SplitByColumnOperation.OperationId, result.OperationId);
        Assert.Contains("no ejecuta", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Inspect_CodigoAjeno_NoLoVincula()
    {
        const string module = """
            Sub BorrarHoja()
                ActiveSheet.Delete
            End Sub
            """;

        var result = MacroModuleInspector.Inspect(module);

        Assert.False(result.IsLinked);
        Assert.Null(result.OperationId);
        Assert.Contains("no ejecuta", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
