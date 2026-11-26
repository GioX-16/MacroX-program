using MacroX.Domain.Macros;
using MacroX.Domain.Tests.Support;
using MacroX.Domain.Workbook;

namespace MacroX.Domain.Tests.Macros;

public class TrimOperationTests
{
    private readonly TrimOperation _sut = new();

    [Fact]
    public async Task ExecuteAsync_TextoConEspacios_RecortaSoloLasColumnasPedidas()
    {
        var workbook = new InMemoryWorkbook();
        var sheet = workbook.AddSheet("Datos");
        sheet.SetCellValue(1, 1, "Nombre");
        sheet.SetCellValue(1, 2, "Ciudad");
        sheet.SetCellValue(2, 1, "  Ana  ");
        sheet.SetCellValue(2, 2, "  León  ");
        sheet.SetCellValue(3, 1, "Luis");
        sheet.SetCellValue(3, 2, "  Lima");

        var result = await _sut.ExecuteAsync(
            workbook,
            Parameters("nombre"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TrimOperation.OperationId, result.OperationId);
        Assert.Equal(1, result.AffectedRows);
        Assert.Equal("Ana", sheet.GetCellValue(2, 1));
        Assert.Equal("Luis", sheet.GetCellValue(3, 1));
        Assert.Equal("  León  ", sheet.GetCellValue(2, 2));
        Assert.Equal("  Lima", sheet.GetCellValue(3, 2));
        Assert.Equal("Nombre", sheet.GetCellValue(1, 1));
    }

    [Fact]
    public async Task ExecuteAsync_CeldaVacia_LaDejaVacia()
    {
        var workbook = new InMemoryWorkbook();
        var sheet = workbook.AddSheet("Datos");
        sheet.SetCellValue(1, 1, "Nombre");
        sheet.SetCellValue(2, 1, "  Ana  ");
        sheet.SetCellValue(4, 1, "  Luz  ");

        var result = await _sut.ExecuteAsync(workbook, Parameters("Nombre"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.AffectedRows);
        Assert.Equal("Ana", sheet.GetCellValue(2, 1));
        Assert.Null(sheet.GetCellValue(3, 1));
        Assert.Equal("Luz", sheet.GetCellValue(4, 1));
    }

    [Fact]
    public async Task ExecuteAsync_SoloEspacios_LaDejaVacia()
    {
        var workbook = new InMemoryWorkbook();
        var sheet = workbook.AddSheet("Datos");
        sheet.SetCellValue(1, 1, "Nombre");
        sheet.SetCellValue(2, 1, "   ");

        var result = await _sut.ExecuteAsync(workbook, Parameters("Nombre"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.AffectedRows);
        Assert.Equal(string.Empty, sheet.GetCellValue(2, 1));
    }

    [Fact]
    public async Task ExecuteAsync_ColumnaAusente_FallaSinEscribir()
    {
        var workbook = new InMemoryWorkbook();
        var sheet = workbook.AddSheet("Datos");
        sheet.SetCellValue(1, 1, "Nombre");
        sheet.SetCellValue(2, 1, "  Ana  ");

        var result = await _sut.ExecuteAsync(
            workbook,
            Parameters("Nombre", "País"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, result.AffectedRows);
        Assert.Contains("País", result.Message, StringComparison.Ordinal);
        Assert.Equal("  Ana  ", sheet.GetCellValue(2, 1));
    }

    [Fact]
    public async Task ExecuteAsync_ParametroColumnasFaltante_Falla()
    {
        var workbook = new InMemoryWorkbook();
        workbook.AddSheet("Datos").SetCellValue(1, 1, "Nombre");

        var result = await _sut.ExecuteAsync(
            workbook,
            new Dictionary<string, object?>(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, result.AffectedRows);
        Assert.Contains("Columnas objetivo", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_TokenCancelado_LanzaOperationCanceledException()
    {
        var workbook = new InMemoryWorkbook();
        var sheet = workbook.AddSheet("Datos");
        sheet.SetCellValue(1, 1, "Nombre");
        sheet.SetCellValue(2, 1, "  Ana  ");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.ExecuteAsync(workbook, Parameters("Nombre"), cts.Token));

        Assert.Equal("  Ana  ", sheet.GetCellValue(2, 1));
    }

    [Fact]
    public async Task ExecuteAsync_CanceladoDuranteLasFilas_LanzaYConservaLoYaRecortado()
    {
        var workbook = new InMemoryWorkbook();
        var sheet = workbook.AddSheet("Datos");
        sheet.SetCellValue(1, 1, "Nombre");
        sheet.SetCellValue(2, 1, "  Ana  ");
        sheet.SetCellValue(3, 1, "  Luz  ");
        sheet.SetCellValue(4, 1, "  Sol  ");

        using var cts = new CancellationTokenSource();
        var gate = new CancelWhenRowIsRead(sheet, cts, cancelOnRow: 3);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.ExecuteAsync(new SingleSheetWorkbook(gate), Parameters("Nombre"), cts.Token));

        Assert.Equal("Ana", sheet.GetCellValue(2, 1));
        Assert.Equal("  Sol  ", sheet.GetCellValue(4, 1));
    }

    [Fact]
    public async Task ExecuteAsync_EspaciosInternos_LosColapsa()
    {
        var workbook = new InMemoryWorkbook();
        var sheet = workbook.AddSheet("Datos");
        sheet.SetCellValue(1, 1, "Cliente");
        sheet.SetCellValue(2, 1, "María   López  ");

        var result = await _sut.ExecuteAsync(workbook, Parameters("Cliente"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.AffectedCells);
        Assert.Equal("María López", sheet.GetCellValue(2, 1));
    }

    [Fact]
    public async Task ExecuteAsync_CeldaConFormula_NoLaModifica()
    {
        var workbook = new InMemoryWorkbook();
        var sheet = workbook.AddSheet("Datos");
        sheet.SetCellValue(1, 1, "Ciudad");
        sheet.SetCellValue(2, 1, "  Managua  ");
        sheet.MarkFormula(2, 1);

        var result = await _sut.ExecuteAsync(workbook, Parameters("Ciudad"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.AffectedCells);
        Assert.Equal("  Managua  ", sheet.GetCellValue(2, 1));
        Assert.NotNull(sheet.GetFormula(2, 1));
    }

    [Fact]
    public async Task ExecuteAsync_HojaInexistente_FallaSinEscribir()
    {
        var workbook = new InMemoryWorkbook();
        var datos = workbook.AddSheet("Datos");
        var otras = workbook.AddSheet("Otras");
        datos.SetCellValue(1, 1, "Nombre");
        datos.SetCellValue(2, 1, "  Ana  ");
        otras.SetCellValue(1, 1, "Nombre");
        otras.SetCellValue(2, 1, "  Luz  ");

        var parameters = new Dictionary<string, object?>
        {
            [TrimOperation.ColumnsParameter] = new[] { "Nombre" },
            [TrimOperation.SheetParameter] = "Ventas",
        };

        var result = await _sut.ExecuteAsync(workbook, parameters, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Ventas", result.Message, StringComparison.Ordinal);
        Assert.Equal("  Ana  ", datos.GetCellValue(2, 1));
        Assert.Equal("  Luz  ", otras.GetCellValue(2, 1));
    }

    private static Dictionary<string, object?> Parameters(params string[] columns) =>
        new()
        {
            [TrimOperation.ColumnsParameter] = columns,
        };

    private sealed class SingleSheetWorkbook : IWorkbookContext
    {
        private readonly IWorksheetContext _sheet;

        public SingleSheetWorkbook(IWorksheetContext sheet)
        {
            _sheet = sheet;
        }

        public IReadOnlyList<string> SheetNames => [_sheet.Name];

        public IWorksheetContext? FindSheet(string name) =>
            string.Equals(name, _sheet.Name, StringComparison.OrdinalIgnoreCase) ? _sheet : null;

        public IWorksheetContext AddSheet(string name) =>
            throw new InvalidOperationException("Este libro de prueba no crea hojas.");
    }

    private sealed class CancelWhenRowIsRead : IWorksheetContext
    {
        private readonly IWorksheetContext _inner;
        private readonly CancellationTokenSource _cancellation;
        private readonly int _cancelOnRow;

        public CancelWhenRowIsRead(IWorksheetContext inner, CancellationTokenSource cancellation, int cancelOnRow)
        {
            _inner = inner;
            _cancellation = cancellation;
            _cancelOnRow = cancelOnRow;
        }

        public string Name => _inner.Name;

        public int LastRow => _inner.LastRow;

        public int LastColumn => _inner.LastColumn;

        public object? GetCellValue(int row, int column)
        {
            if (row == _cancelOnRow)
            {
                _cancellation.Cancel();
            }

            return _inner.GetCellValue(row, column);
        }

        public string? GetFormula(int row, int column) => _inner.GetFormula(row, column);

        public void SetCellValue(int row, int column, object? value) =>
            _inner.SetCellValue(row, column, value);

        public void SetFormula(int row, int column, string formula) =>
            _inner.SetFormula(row, column, formula);
    }
}
