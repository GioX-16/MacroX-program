using MacroX.Domain.Macros;

namespace MacroX.Application.Workbooks;

/// <summary>
/// Resultado de aplicar las macros seleccionadas a un libro.
/// </summary>
public sealed class ApplyMacrosResult
{
    private ApplyMacrosResult(
        bool isSuccess,
        string message,
        string? outputPath,
        IReadOnlyList<MacroResult> steps)
    {
        IsSuccess = isSuccess;
        Message = message;
        OutputPath = outputPath;
        Steps = steps;
    }

    public bool IsSuccess { get; }

    public string Message { get; }

    public string? OutputPath { get; }

    public IReadOnlyList<MacroResult> Steps { get; }

    public int AffectedCells => Steps.Sum(step => step.AffectedCells);

    public static ApplyMacrosResult Success(string outputPath, string message, IReadOnlyList<MacroResult> steps) =>
        new(true, message, outputPath, steps);

    public static ApplyMacrosResult Failure(string message, IReadOnlyList<MacroResult>? steps = null) =>
        new(false, message, null, steps ?? []);
}
