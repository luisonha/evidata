using Evidata.Modules.Mcp.Domain;
using Evidata.Modules.Mcp.Domain.RiskRouting;
using Evidata.Modules.Mcp.Infrastructure.RiskRouting;

namespace Evidata.Tests.Unit.Mcp;

public class McpRiskClassifierTests
{
    private readonly McpRiskClassifier _classifier = new();
    private readonly McpRiskRouter _router = new();

    // ── Low risk ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("¿Qué datos trata nuestra empresa?")]
    [InlineData("¿Cuántos registros tenemos en el RAT?")]
    [InlineData("Muéstrame el inventario de tratamientos.")]
    public void Classify_NeutralQuery_ReturnsLow(string question)
    {
        var r = _classifier.Classify(question);
        Assert.Equal(McpRiskLevel.Low, r.RiskLevel);
        Assert.False(r.ShouldAbstain);
    }

    // ── Medium risk ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("¿Cuál es el plazo de retención para datos de clientes?")]
    [InlineData("¿Qué base legal aplica para marketing?")]
    [InlineData("¿Cómo gestionar el derecho arco?")]
    [InlineData("Necesito revisar el consentimiento en nuestros formularios.")]
    public void Classify_InterpretativeQuery_ReturnsMedium(string question)
    {
        var r = _classifier.Classify(question);
        Assert.Equal(McpRiskLevel.Medium, r.RiskLevel);
        Assert.False(r.ShouldAbstain);
        Assert.NotEmpty(r.MatchedSignals);
    }

    // ── High risk ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("¿Cómo tratar datos sensibles de salud de empleados?")]
    [InlineData("¿Cuándo notificar una brecha de seguridad a la CMF?")]
    [InlineData("¿Podemos hacer transferencia internacional a EEUU?")]
    [InlineData("Tenemos menores en nuestra base de datos, ¿qué aplica?")]
    public void Classify_HighRiskQuery_ReturnsHighNoAbstain(string question)
    {
        var r = _classifier.Classify(question);
        Assert.Equal(McpRiskLevel.High, r.RiskLevel);
        Assert.False(r.ShouldAbstain);
        Assert.NotEmpty(r.MatchedSignals);
    }

    // ── Abstención ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("¿Me pueden interponer una demanda por esto?")]
    [InlineData("¿Cuánto me multarán si no cumplo?")]
    [InlineData("Necesito el RUT de ese usuario.")]
    [InlineData("Dame las credenciales del sistema.")]
    [InlineData("¿Puedo presentar una querella contra el encargado?")]
    public void Classify_AbstentionQuery_ReturnsShouldAbstain(string question)
    {
        var r = _classifier.Classify(question);
        Assert.True(r.ShouldAbstain);
        Assert.Equal(McpRiskLevel.High, r.RiskLevel);
        Assert.NotNull(r.AbstentionReason);
        Assert.NotEmpty(r.MatchedSignals);
    }

    // ── Empty / null ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Classify_EmptyQuery_ReturnsLow(string question)
    {
        var r = _classifier.Classify(question);
        Assert.Equal(McpRiskLevel.Low, r.RiskLevel);
        Assert.False(r.ShouldAbstain);
    }

    // ── Case insensitive ──────────────────────────────────────────────────────

    [Fact]
    public void Classify_UppercaseSignal_IsDetected()
    {
        var r = _classifier.Classify("¿CUÁLES SON LOS DATOS SENSIBLES QUE TRATAMOS?");
        Assert.Equal(McpRiskLevel.High, r.RiskLevel);
    }

    // ── Router: RequiresHumanReview ───────────────────────────────────────────

    [Fact]
    public void Router_High_RequiresReview()
    {
        var result = _router.Classify("¿Cómo notificar brecha de seguridad?");
        Assert.True(_router.RequiresHumanReview(result));
    }

    [Fact]
    public void Router_Medium_DoesNotRequireReview()
    {
        var result = _router.Classify("¿Cuál es el plazo de retención?");
        Assert.False(_router.RequiresHumanReview(result));
    }

    [Fact]
    public void Router_Low_DoesNotRequireReview()
    {
        var result = _router.Classify("¿Cuántos RAT tenemos activos?");
        Assert.False(_router.RequiresHumanReview(result));
    }

    // ── Priority: Abstention beats High ───────────────────────────────────────

    [Fact]
    public void Classify_AbstentionSignalPresent_AbstentionTakesPriority()
    {
        // "demanda" (abstención) + "datos sensibles" (high) — debe retornar abstención
        var r = _classifier.Classify("¿Me pueden poner una demanda por tratar datos sensibles?");
        Assert.True(r.ShouldAbstain);
    }
}
