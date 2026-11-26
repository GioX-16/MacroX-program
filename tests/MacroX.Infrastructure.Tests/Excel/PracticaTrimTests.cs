using ClosedXML.Excel;
using MacroX.Application.Catalog;
using MacroX.Application.Workbooks;
using MacroX.Domain.Macros;
using MacroX.Infrastructure.Excel;

namespace MacroX.Infrastructure.Tests.Excel;

public class PracticaTrimTests
{
    [Fact]
    public async Task ExecuteAsync_PracticaTrim_RecortaQuinceCeldasYNoTocaElOriginal()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "practica_trim.xlsx");
        var original = await File.ReadAllBytesAsync(source);
        var output = Path.Combine(Path.GetTempPath(), "MacroX", Guid.NewGuid().ToString("N") + ".xlsx");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);

        var sut = new ApplyMacrosUseCase(
            new MacroCatalog([new TrimOperation()]),
            new ClosedXmlWorkbookSessionFactory());

        try
        {
            var result = await sut.ExecuteAsync(
                new ApplyMacrosRequest(source, output, "Clientes", [TrimOperation.OperationId]),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(15, result.AffectedCells);
            Assert.Equal(original, await File.ReadAllBytesAsync(source));

            using var workbook = new XLWorkbook(output);
            var sheet = workbook.Worksheet("Clientes");
            Assert.Equal("Juan Pérez", sheet.Cell("B2").GetString());
            Assert.Equal("María López", sheet.Cell("B3").GetString());
            Assert.Equal("Managua", sheet.Cell("C2").GetString());
            Assert.Equal("Managua", sheet.Cell("C4").GetString());
            Assert.Equal("Ana Gómez", sheet.Cell("B5").GetString());
            Assert.Equal("Sofía Morales", sheet.Cell("B11").GetString());
            Assert.Equal(15000d, sheet.Cell("E2").GetDouble());
            Assert.Contains("IF", sheet.Cell("F2").FormulaA1, StringComparison.OrdinalIgnoreCase);
            Assert.True(sheet.Cell("F2").HasFormula);
        }
        finally
        {
            if (File.Exists(output))
            {
                File.Delete(output);
            }
        }
    }
}
