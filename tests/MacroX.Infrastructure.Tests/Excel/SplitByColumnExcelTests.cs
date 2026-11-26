using ClosedXML.Excel;
using MacroX.Application.Catalog;
using MacroX.Application.Workbooks;
using MacroX.Domain.Macros;
using MacroX.Infrastructure.Excel;

namespace MacroX.Infrastructure.Tests.Excel;

public class SplitByColumnExcelTests
{
    [Fact]
    public async Task ExecuteAsync_SeparaPorVendedorYGuardaLasHojasNuevas()
    {
        var directory = Path.Combine(Path.GetTempPath(), "MacroX", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var source = Path.Combine(directory, "ventas.xlsx");
        var output = Path.Combine(directory, "ventas_macrox.xlsx");

        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Ventas");
            sheet.Cell(1, 1).Value = "Vendedor";
            sheet.Cell(1, 2).Value = "Ciudad";
            sheet.Cell(1, 3).Value = "Total";
            sheet.Cell(2, 1).Value = "Ana";
            sheet.Cell(2, 2).Value = "Managua";
            sheet.Cell(2, 3).Value = 10d;
            sheet.Cell(3, 1).Value = "Luis";
            sheet.Cell(3, 2).Value = "Granada";
            sheet.Cell(3, 3).Value = 7d;
            workbook.SaveAs(source);
        }

        var sut = new ApplyMacrosUseCase(
            new MacroCatalog([new SplitByColumnOperation()]),
            new ClosedXmlWorkbookSessionFactory());

        try
        {
            var result = await sut.ExecuteAsync(
                new ApplyMacrosRequest(source, output, "Ventas", [SplitByColumnOperation.OperationId], "Vendedor"),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Contains("Ana", result.Message, StringComparison.Ordinal);

            using var saved = new XLWorkbook(output);
            Assert.Equal(["Ventas", "Ana", "Luis"], saved.Worksheets.Select(sheet => sheet.Name).ToArray());
            Assert.Equal("Ana", saved.Worksheet("Ventas").Cell(2, 1).GetString());
            Assert.Equal("TOTAL", saved.Worksheet("Ana").Cell(3, 2).GetString());
            Assert.Equal("SUM(C2:C2)", saved.Worksheet("Ana").Cell(3, 3).FormulaA1);
            Assert.Equal(10d, saved.Worksheet("Ana").Cell(2, 3).GetDouble());
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
