using MacroX.Domain.Macros;
using MacroX.Domain.Tests.Support;

namespace MacroX.Domain.Tests.Macros;

public class SplitByColumnOperationTests
{
    private readonly SplitByColumnOperation _sut = new();

    [Fact]
    public async Task ExecuteAsync_DosVendedores_CreaUnaHojaPorValorConTotal()
    {
        var workbook = new InMemoryWorkbook();
        var source = workbook.AddSheet("Ventas");
        source.SetCellValue(1, 1, "Vendedor");
        source.SetCellValue(1, 2, "Ciudad");
        source.SetCellValue(1, 3, "Total");
        source.SetCellValue(2, 1, "Ana");
        source.SetCellValue(2, 2, "Managua");
        source.SetCellValue(2, 3, 10d);
        source.SetCellValue(3, 1, "Ana");
        source.SetCellValue(3, 2, "León");
        source.SetCellValue(3, 3, 5d);
        source.SetCellValue(4, 1, "Luis");
        source.SetCellValue(4, 2, "Granada");
        source.SetCellValue(4, 3, 7d);
        source.MarkFormula(2, 2, "IF(TRUE,\"Managua\",\"No\")");

        var result = await _sut.ExecuteAsync(workbook, Parameters("Vendedor"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.AffectedRows);
        Assert.Equal(["Ventas", "Ana", "Luis"], workbook.SheetNames);
        Assert.Equal("Ana", source.GetCellValue(2, 1));

        var ana = workbook.FindSheet("Ana")!;
        Assert.Equal("Vendedor", ana.GetCellValue(1, 1));
        Assert.Equal("Managua", ana.GetCellValue(2, 2));
        Assert.Null(ana.GetFormula(2, 2));
        Assert.Equal("TOTAL", ana.GetCellValue(4, 2));
        Assert.Equal("SUM(C2:C3)", ana.GetFormula(4, 3));

        var luis = workbook.FindSheet("Luis")!;
        Assert.Equal("SUM(C2:C2)", luis.GetFormula(3, 3));
        Assert.Contains("Ana, Luis", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_ValorVacioOConBarras_UsaNombreDeHojaValido()
    {
        var workbook = new InMemoryWorkbook();
        var source = workbook.AddSheet("Datos");
        source.SetCellValue(1, 1, "Grupo");
        source.SetCellValue(2, 1, "  ");
        source.SetCellValue(3, 1, "Norte/Sur");

        var result = await _sut.ExecuteAsync(workbook, Parameters("Grupo"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("Sin_dato", workbook.SheetNames);
        Assert.Contains("Norte-Sur", workbook.SheetNames);
        Assert.DoesNotContain(workbook.SheetNames, name => name.Contains('/', StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_LaHojaDeOrigenYaSeLlamaComoElValor_NoLaReemplaza()
    {
        var workbook = new InMemoryWorkbook();
        var source = workbook.AddSheet("Ana");
        source.SetCellValue(1, 1, "Vendedor");
        source.SetCellValue(2, 1, "Ana");

        var result = await _sut.ExecuteAsync(workbook, Parameters("Vendedor"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Ana", "Ana (2)"], workbook.SheetNames);
        Assert.Equal("Ana", source.GetCellValue(2, 1));
    }

    [Fact]
    public async Task ExecuteAsync_ColumnaInexistente_NoCreaHojas()
    {
        var workbook = new InMemoryWorkbook();
        var source = workbook.AddSheet("Ventas");
        source.SetCellValue(1, 1, "Ciudad");
        source.SetCellValue(2, 1, "León");

        var result = await _sut.ExecuteAsync(workbook, Parameters("Vendedor"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Vendedor", result.Message, StringComparison.Ordinal);
        Assert.Equal(["Ventas"], workbook.SheetNames);
    }

    private static Dictionary<string, object?> Parameters(string column) =>
        new()
        {
            [SplitByColumnOperation.ColumnParameter] = column,
        };
}
