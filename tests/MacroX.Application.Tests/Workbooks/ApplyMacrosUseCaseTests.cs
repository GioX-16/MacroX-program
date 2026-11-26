using MacroX.Application.Catalog;
using MacroX.Application.Workbooks;
using MacroX.Domain.Macros;
using MacroX.Domain.Workbook;

namespace MacroX.Application.Tests.Workbooks;

public class ApplyMacrosUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Trim_RecortaYGuardaEnOtraRuta()
    {
        var session = new MemorySession(Sample());
        var sut = CreateUseCase(session);

        var result = await sut.ExecuteAsync(
            new ApplyMacrosRequest("C:\\origen\\clientes.xlsx", "C:\\salida\\clientes_macrox.xlsx", "Clientes", [TrimOperation.OperationId]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.AffectedCells);
        Assert.Equal("C:\\salida\\clientes_macrox.xlsx", session.SavedPath);
        Assert.Equal("María López", session.Sheet.GetCellValue(2, 2));
        Assert.Equal("Managua", session.Sheet.GetCellValue(2, 3));
        Assert.Equal("  Managua  ", session.Sheet.GetCellValue(3, 3));
        Assert.NotNull(session.Sheet.GetFormula(3, 3));
    }

    [Fact]
    public async Task ExecuteAsync_MismaRuta_NoAbreElArchivo()
    {
        var session = new MemorySession(Sample());
        var sut = CreateUseCase(session);

        var result = await sut.ExecuteAsync(
            new ApplyMacrosRequest("C:\\origen\\clientes.xlsx", "C:\\origen\\clientes.xlsx", "Clientes", [TrimOperation.OperationId]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, session.OpenCount);
        Assert.Null(session.SavedPath);
    }

    [Fact]
    public async Task ExecuteAsync_MacroDesconocida_NoGuarda()
    {
        var session = new MemorySession(Sample());
        var sut = CreateUseCase(session);

        var result = await sut.ExecuteAsync(
            new ApplyMacrosRequest("C:\\origen\\clientes.xlsx", "C:\\salida\\nuevo.xlsx", "Clientes", ["borrar"]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(session.SavedPath);
        Assert.Equal("María   López  ", session.Sheet.GetCellValue(2, 2));
    }

    private static ApplyMacrosUseCase CreateUseCase(MemorySession session) =>
        new(new MacroCatalog([new TrimOperation()]), new MemoryFactory(session));

    private static MemorySheet Sample()
    {
        var sheet = new MemorySheet("Clientes");
        sheet.SetCellValue(1, 1, "ID");
        sheet.SetCellValue(1, 2, "Cliente");
        sheet.SetCellValue(1, 3, "Ciudad");
        sheet.SetCellValue(2, 1, "C001");
        sheet.SetCellValue(2, 2, "María   López  ");
        sheet.SetCellValue(2, 3, "Managua ");
        sheet.SetCellValue(3, 1, "C002");
        sheet.SetCellValue(3, 2, "Ana");
        sheet.SetCellValue(3, 3, "  Managua  ");
        sheet.MarkFormula(3, 3);
        return sheet;
    }

    private sealed class MemoryFactory : IWorkbookSessionFactory
    {
        private readonly MemorySession _session;

        public MemoryFactory(MemorySession session)
        {
            _session = session;
        }

        public Task<IWorkbookSession> OpenCopyAsync(string sourcePath, CancellationToken cancellationToken)
        {
            _session.OpenCount++;
            return Task.FromResult<IWorkbookSession>(_session);
        }
    }

    private sealed class MemorySession : IWorkbookSession
    {
        private readonly MemoryWorkbook _workbook;

        public MemorySession(MemorySheet sheet)
        {
            Sheet = sheet;
            _workbook = new MemoryWorkbook(sheet);
        }

        public MemorySheet Sheet { get; }

        public int OpenCount { get; set; }

        public string? SavedPath { get; private set; }

        public IWorkbookContext Workbook => _workbook;

        public Task SaveAsAsync(string outputPath, CancellationToken cancellationToken)
        {
            SavedPath = outputPath;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class MemoryWorkbook : IWorkbookContext
    {
        private readonly MemorySheet _sheet;

        public MemoryWorkbook(MemorySheet sheet)
        {
            _sheet = sheet;
        }

        public IReadOnlyList<string> SheetNames => [_sheet.Name];

        public IWorksheetContext? FindSheet(string name) =>
            string.Equals(name, _sheet.Name, StringComparison.OrdinalIgnoreCase) ? _sheet : null;

        public IWorksheetContext AddSheet(string name) =>
            throw new InvalidOperationException("Este libro de prueba no crea hojas.");
    }

    private sealed class MemorySheet : IWorksheetContext
    {
        private readonly Dictionary<(int Row, int Column), object?> _cells = [];
        private readonly Dictionary<(int Row, int Column), string> _formulas = [];

        public MemorySheet(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public int LastRow => _cells.Count == 0 ? 0 : _cells.Keys.Max(cell => cell.Row);

        public int LastColumn => _cells.Count == 0 ? 0 : _cells.Keys.Max(cell => cell.Column);

        public object? GetCellValue(int row, int column) =>
            _cells.TryGetValue((row, column), out var value) ? value : null;

        public string? GetFormula(int row, int column) =>
            _formulas.TryGetValue((row, column), out var formula) ? formula : null;

        public void SetCellValue(int row, int column, object? value) => _cells[(row, column)] = value;

        public void SetFormula(int row, int column, string formula) =>
            _formulas[(row, column)] = formula.Trim().TrimStart('=');

        public void MarkFormula(int row, int column) => _formulas[(row, column)] = "IF(C3=\"Managua\",\"Sí\",\"No\")";
    }
}
