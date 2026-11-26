using MacroX.Application.Catalog;
using MacroX.Domain.Macros;
using MacroX.Domain.Workbook;

namespace MacroX.Application.Workbooks;

/// <summary>
/// Aplica las macros elegidas, en el orden indicado, y escribe un archivo nuevo.
/// </summary>
public sealed class ApplyMacrosUseCase
{
    private readonly IMacroCatalog _catalog;
    private readonly IWorkbookSessionFactory _files;

    public ApplyMacrosUseCase(IMacroCatalog catalog, IWorkbookSessionFactory files)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public async Task<ApplyMacrosResult> ExecuteAsync(ApplyMacrosRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (request.OperationIds.Count == 0)
        {
            return ApplyMacrosResult.Failure("Selecciona al menos una macro.");
        }

        if (SamePath(request.SourcePath, request.OutputPath))
        {
            return ApplyMacrosResult.Failure("El archivo nuevo tiene que ser distinto del original.");
        }

        var operations = new List<IMacroOperation>(request.OperationIds.Count);
        foreach (var operationId in request.OperationIds)
        {
            var operation = _catalog.Find(operationId);
            if (operation is null)
            {
                return ApplyMacrosResult.Failure($"La macro «{operationId}» no está en la biblioteca.");
            }

            operations.Add(operation);
        }

        await using var session = await _files.OpenCopyAsync(request.SourcePath, cancellationToken).ConfigureAwait(false);
        var sheet = session.Workbook.FindSheet(request.SheetName);
        if (sheet is null)
        {
            return ApplyMacrosResult.Failure($"La hoja «{request.SheetName}» no existe.");
        }

        var steps = new List<MacroResult>(operations.Count);
        foreach (var operation in operations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await operation.ExecuteAsync(
                session.Workbook,
                BuildParameters(operation, sheet, request),
                cancellationToken).ConfigureAwait(false);
            steps.Add(result);
            if (!result.IsSuccess)
            {
                return ApplyMacrosResult.Failure(result.Message, steps);
            }
        }

        await session.SaveAsAsync(request.OutputPath, cancellationToken).ConfigureAwait(false);
        var detail = string.Join(" ", steps.Select(step => step.Message)).Trim();
        if (detail.Length == 0)
        {
            detail = "Lista. Las macros se aplicaron.";
        }

        return ApplyMacrosResult.Success(request.OutputPath, detail + " El original no se modificó.", steps);
    }

    private static Dictionary<string, object?> BuildParameters(
        IMacroOperation operation,
        IWorksheetContext sheet,
        ApplyMacrosRequest request)
    {
        var parameters = new Dictionary<string, object?>();
        if (operation.Parameters.Any(parameter => parameter.Key == TrimOperation.ColumnsParameter))
        {
            var headers = new List<string>();
            for (var column = 1; column <= sheet.LastColumn; column++)
            {
                if (sheet.GetCellValue(1, column) is string text && !string.IsNullOrWhiteSpace(text))
                {
                    headers.Add(text.Trim());
                }
            }

            parameters[TrimOperation.ColumnsParameter] = headers;
        }

        if (operation.Parameters.Any(parameter => parameter.Key == SplitByColumnOperation.ColumnParameter)
            && !string.IsNullOrWhiteSpace(request.ColumnName))
        {
            parameters[SplitByColumnOperation.ColumnParameter] = request.ColumnName;
        }

        if (operation.Parameters.Any(parameter => parameter.Key == TrimOperation.SheetParameter)
            || operation.Parameters.Any(parameter => parameter.Key == SplitByColumnOperation.SheetParameter))
        {
            parameters["sheet"] = sheet.Name;
        }

        return parameters;
    }

    private static bool SamePath(string left, string right)
    {
        var fullLeft = Path.GetFullPath(left);
        var fullRight = Path.GetFullPath(right);
        return string.Equals(fullLeft, fullRight, StringComparison.OrdinalIgnoreCase);
    }
}
