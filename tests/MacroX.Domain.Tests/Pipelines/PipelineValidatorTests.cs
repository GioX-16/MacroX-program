using MacroX.Domain.Pipelines;

namespace MacroX.Domain.Tests.Pipelines;

public class PipelineValidatorTests
{
    private readonly PipelineValidator _sut = new();

    [Fact]
    public void Validate_SinPasos_EsInvalido()
    {
        var result = _sut.Validate(new Pipeline([]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("no tiene pasos", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_IdentificadorVacio_EsInvalido()
    {
        var result = _sut.Validate(new Pipeline([new PipelineStep("  ", 1)]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("sin identificador", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_OrdenMenorQueUno_EsInvalido()
    {
        var result = _sut.Validate(new Pipeline([new PipelineStep("trim", 0)]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("empieza en 1", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_OrdenRepetido_EsInvalido()
    {
        var pipeline = new Pipeline(
        [
            new PipelineStep("trim", 1),
            new PipelineStep("uppercase", 1),
        ]);

        var result = _sut.Validate(pipeline);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("repetido", StringComparison.Ordinal));
    }

    [Fact]
    public void TryGetExecutionOrder_PipelineInvalido_NoDevuelvePasos()
    {
        var delivered = _sut.TryGetExecutionOrder(
            new Pipeline([]),
            out var orderedSteps,
            out var validation);

        Assert.False(delivered);
        Assert.False(validation.IsValid);
        Assert.Empty(orderedSteps);
    }

    [Fact]
    public void TryGetExecutionOrder_PasosDesordenados_LosEntregaEnOrden()
    {
        var pipeline = new Pipeline(
        [
            new PipelineStep("uppercase", 2),
            new PipelineStep("trim", 1),
        ]);

        var delivered = _sut.TryGetExecutionOrder(pipeline, out var orderedSteps, out var validation);

        Assert.True(delivered);
        Assert.True(validation.IsValid);
        Assert.Equal(["trim", "uppercase"], orderedSteps.Select(step => step.OperationId).ToArray());
    }

    [Fact]
    public void ExecutionSignature_OrdenDistinto_FirmaDeEjecucionDistinta()
    {
        var trimThenCase = new Pipeline(
        [
            new PipelineStep("trim", 1),
            new PipelineStep("uppercase", 2),
        ]);
        var caseThenTrim = new Pipeline(
        [
            new PipelineStep("trim", 2),
            new PipelineStep("uppercase", 1),
        ]);

        Assert.Equal("1:trim|2:uppercase", trimThenCase.ExecutionSignature);
        Assert.Equal("1:uppercase|2:trim", caseThenTrim.ExecutionSignature);
        Assert.NotEqual(trimThenCase.ExecutionSignature, caseThenTrim.ExecutionSignature);
    }
}
