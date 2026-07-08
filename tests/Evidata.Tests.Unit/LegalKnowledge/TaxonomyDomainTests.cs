using Evidata.Modules.LegalKnowledge.Domain;
using Xunit;

namespace Evidata.Tests.Unit.LegalKnowledge;

public class TaxonomyDomainTests
{
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    // ── DataCategory ──────────────────────────────────────────────────────────

    [Fact]
    public void DataCategory_CreateGlobal_TenantIdIsEmpty()
    {
        var cat = DataCategory.CreateGlobal("DATOS-SENSIBLES", "Datos sensibles", true, AdminId);

        Assert.Equal(Guid.Empty, cat.TenantId);
        Assert.Equal("DATOS-SENSIBLES", cat.Code);
        Assert.True(cat.IsSensitive);
        Assert.True(cat.IsActive);
    }

    [Fact]
    public void DataCategory_CreateForTenant_HasTenantId()
    {
        var cat = DataCategory.CreateForTenant(TenantId, "DATOS-INTERNOS", "Datos internos", false, AdminId);

        Assert.Equal(TenantId, cat.TenantId);
        Assert.Equal("DATOS-INTERNOS", cat.Code);
        Assert.False(cat.IsSensitive);
    }

    [Fact]
    public void DataCategory_Code_NormalizedToUpper()
    {
        var cat = DataCategory.CreateGlobal("datos-identificacion", "Test", false, AdminId);
        Assert.Equal("DATOS-IDENTIFICACION", cat.Code);
    }

    [Fact]
    public void DataCategory_Deactivate_SetsIsActiveFalse()
    {
        var cat = DataCategory.CreateGlobal("CODE", "Name", false, AdminId);
        cat.Deactivate(AdminId);
        Assert.False(cat.IsActive);
        Assert.NotNull(cat.UpdatedAt);
    }

    [Fact]
    public void DataCategory_EmptyCode_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            DataCategory.CreateGlobal("", "Name", false, AdminId));
    }

    // ── DataSubjectCategory ───────────────────────────────────────────────────

    [Fact]
    public void DataSubjectCategory_CreateGlobal_RequiresSpecialSafeguards_Minors()
    {
        var cat = DataSubjectCategory.CreateGlobal(
            "MENORES", "Menores de edad", requiresSpecialSafeguards: true, AdminId,
            "Titulares menores de 14 años requieren consentimiento del representante legal.");

        Assert.True(cat.RequiresSpecialSafeguards);
        Assert.Equal(Guid.Empty, cat.TenantId);
        Assert.NotNull(cat.Description);
    }

    [Fact]
    public void DataSubjectCategory_CreateForTenant_HasCorrectTenantId()
    {
        var cat = DataSubjectCategory.CreateForTenant(
            TenantId, "CLIENTES-VIP", "Clientes VIP", false, AdminId);

        Assert.Equal(TenantId, cat.TenantId);
        Assert.False(cat.RequiresSpecialSafeguards);
    }

    // ── SecurityMeasure ───────────────────────────────────────────────────────

    [Fact]
    public void SecurityMeasure_CreateGlobal_MandatoryTechnical()
    {
        var measure = SecurityMeasure.CreateGlobal(
            "CIFRADO-TRANSITO", "Cifrado en tránsito (TLS 1.2+)",
            SecurityMeasureType.Technical, isMandatory: true, AdminId,
            "Todo tráfico debe usar TLS 1.2 o superior según art. 14 Ley 21.719.");

        Assert.Equal("CIFRADO-TRANSITO", measure.Code);
        Assert.Equal(SecurityMeasureType.Technical, measure.MeasureType);
        Assert.True(measure.IsMandatory);
        Assert.Equal(Guid.Empty, measure.TenantId);
    }

    [Fact]
    public void SecurityMeasure_CreateForTenant_HasTenantId()
    {
        var measure = SecurityMeasure.CreateForTenant(
            TenantId, "POLITICA-INTERNA", "Política retención interna",
            SecurityMeasureType.Organizational, isMandatory: false, AdminId);

        Assert.Equal(TenantId, measure.TenantId);
        Assert.False(measure.IsMandatory);
    }

    [Fact]
    public void SecurityMeasure_Deactivate_SetsInactive()
    {
        var measure = SecurityMeasure.CreateGlobal(
            "OLD-MEASURE", "Medida obsoleta",
            SecurityMeasureType.Physical, false, AdminId);

        measure.Deactivate(AdminId);

        Assert.False(measure.IsActive);
        Assert.Equal(AdminId, measure.UpdatedBy);
    }
}
