using Evidata.Modules.GapManagement.Domain;
using Evidata.Modules.GapManagement.Infrastructure.Persistence.Seeding;

namespace Evidata.Tests.Unit.GapManagement;

/// <summary>
/// Tests para GapRuleInitializer.GetDefaultRules().
/// Verifica la integridad del catálogo de 11 reglas de detección de brechas.
/// </summary>
public class GapRuleInitializerTests
{
    [Fact]
    public void GetDefaultRules_Returns11Rules()
    {
        var rules = GapRuleInitializer.GetDefaultRules();
        Assert.NotNull(rules);
        Assert.Equal(11, rules.Count);
    }

    [Fact]
    public void GetDefaultRules_AllRuleCodesAreUnique()
    {
        var rules = GapRuleInitializer.GetDefaultRules();
        var ruleCodes = rules.Select(r => r.RuleCode).ToList();
        var uniqueCodes = ruleCodes.Distinct().ToList();
        
        Assert.Equal(ruleCodes.Count, uniqueCodes.Count);
    }

    [Fact]
    public void GetDefaultRules_AllRulesHaveRequiredFields()
    {
        var rules = GapRuleInitializer.GetDefaultRules();
        
        foreach (var rule in rules)
        {
            // RuleCode no vacío
            Assert.NotNull(rule.RuleCode);
            Assert.NotEmpty(rule.RuleCode);
            
            // Description no vacío
            Assert.NotNull(rule.Description);
            Assert.NotEmpty(rule.Description);
            
            // TestFixtureName no vacío
            Assert.NotNull(rule.TestFixtureName);
            Assert.NotEmpty(rule.TestFixtureName);
            
            // BlocksApproval debe estar set
            Assert.IsType<bool>(rule.BlocksApproval);
            
            // Severity debe estar definido
            Assert.True(
                rule.Severity == GapSeverity.Critical || 
                rule.Severity == GapSeverity.High ||
                rule.Severity == GapSeverity.Medium ||
                rule.Severity == GapSeverity.Low,
                $"Severity inválido para {rule.RuleCode}: {rule.Severity}");
            
            // IsFullyImplemented debe estar set
            Assert.IsType<bool>(rule.IsFullyImplemented);
        }
    }

    [Fact]
    public void GetDefaultRules_ContainsExpectedRuleCodes()
    {
        var rules = GapRuleInitializer.GetDefaultRules();
        var ruleCodes = rules.Select(r => r.RuleCode).ToHashSet();
        
        var expectedRules = new[]
        {
            "TRANSFER_WITHOUT_DESTINATION_COUNTRY",
            "TRANSFER_WITHOUT_RECEIVER",
            "TRANSFER_WITHOUT_SAFEGUARD",
            "TRANSFER_WITHOUT_BLOCKING_EVIDENCE",
            "SENSITIVE_DATA_WITHOUT_SECURITY_REVIEW",
            "LEGAL_BASIS_MISSING",
            "RETENTION_UNDEFINED",
            "DATA_CATEGORIES_EMPTY",
            "PURPOSE_UNDEFINED",
            "DATA_SUBJECTS_EMPTY",
            "SYSTEMS_WITHOUT_OWNER"
        };

        foreach (var expected in expectedRules)
        {
            Assert.Contains(expected, ruleCodes);
        }
    }

    [Fact]
    public void GetDefaultRules_AllCriticalRulesBlockApproval()
    {
        var rules = GapRuleInitializer.GetDefaultRules();
        var criticalRules = rules.Where(r => r.Severity == GapSeverity.Critical).ToList();
        
        Assert.NotEmpty(criticalRules);
        foreach (var rule in criticalRules)
        {
            Assert.True(rule.BlocksApproval, $"Critical rule {rule.RuleCode} debe bloquear aprobación");
        }
    }
}
