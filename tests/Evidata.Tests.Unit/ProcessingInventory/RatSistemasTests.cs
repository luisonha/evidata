using Evidata.Modules.ProcessingInventory.Domain;

namespace Evidata.Tests.Unit.ProcessingInventory;

public class RatSistemasTests
{
    private static ProcessingActivity BuildDraft() =>
        ProcessingActivity.Create(Guid.NewGuid(), "Tratamiento Test", Guid.NewGuid());

    private static ProcessingActivity BuildApproved()
    {
        var act = BuildDraft();
        act.SetPurpose(PurposeSection.Create("Gestión de nómina", LegalBasis.ContractExecution, "Contrato laboral"), Guid.NewGuid());
        act.SetDataCategories([DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary)], Guid.NewGuid());
        act.SetDataSubjects([DataSubjectEntry.Create(DataSubjectType.Employees)], Guid.NewGuid());
        act.SetRetention(RetentionSection.Create("5 años"), Guid.NewGuid());
        act.SubmitForReview(Guid.NewGuid());
        act.Approve(Guid.NewGuid());
        return act;
    }

    // ── SystemEntry ───────────────────────────────────────────────────────────

    [Fact]
    public void SetSystems_Valid_AssignsList()
    {
        var act = BuildDraft();
        act.SetSystems([
            SystemEntry.Create("SAP HR", role: "Nómina", isExternal: false),
            SystemEntry.Create("Workday", isExternal: true)
        ], Guid.NewGuid());

        Assert.Equal(2, act.Systems.Count);
        Assert.Equal("SAP HR", act.Systems[0].SystemName);
        Assert.True(act.Systems[1].IsExternal);
    }

    [Fact]
    public void SetSystems_Empty_ClearsListOK()
    {
        var act = BuildDraft();
        act.SetSystems([SystemEntry.Create("SAP")], Guid.NewGuid());
        act.SetSystems([], Guid.NewGuid());
        Assert.Empty(act.Systems);
    }

    [Fact]
    public void SystemEntry_EmptyName_Throws() =>
        Assert.Throws<ArgumentException>(() => SystemEntry.Create(""));

    [Fact]
    public void SetSystems_OnApproved_Throws()
    {
        var act = BuildApproved();
        Assert.Throws<InvalidOperationException>(() =>
            act.SetSystems([SystemEntry.Create("SAP")], Guid.NewGuid()));
    }

    // ── SupplierEntry ─────────────────────────────────────────────────────────

    [Fact]
    public void SetSuppliers_Valid_AssignsList()
    {
        var act = BuildDraft();
        act.SetSuppliers([
            SupplierEntry.Create("AWS", country: "US", hasDataProcessingAgreement: true),
            SupplierEntry.Create("Proveedor Local", country: "CL")
        ], Guid.NewGuid());

        Assert.Equal(2, act.Suppliers.Count);
        Assert.True(act.Suppliers[0].HasDataProcessingAgreement);
        Assert.Equal("CL", act.Suppliers[1].Country);
    }

    [Fact]
    public void SupplierEntry_EmptyName_Throws() =>
        Assert.Throws<ArgumentException>(() => SupplierEntry.Create(""));

    [Fact]
    public void SetSuppliers_Replaces_Previous()
    {
        var act = BuildDraft();
        act.SetSuppliers([SupplierEntry.Create("Antiguo")], Guid.NewGuid());
        act.SetSuppliers([SupplierEntry.Create("Nuevo A"), SupplierEntry.Create("Nuevo B")], Guid.NewGuid());
        Assert.Equal(2, act.Suppliers.Count);
        Assert.Equal("Nuevo A", act.Suppliers[0].SupplierName);
    }

    // ── RetentionSection ──────────────────────────────────────────────────────

    [Fact]
    public void SetRetention_Valid_Assigned()
    {
        var act = BuildDraft();
        var retention = RetentionSection.Create(
            "5 años desde extinción del contrato laboral",
            retentionMonths: 60,
            legalJustification: "Art. 159 Código del Trabajo");

        act.SetRetention(retention, Guid.NewGuid());

        Assert.NotNull(act.Retention);
        Assert.Equal(60, act.Retention.RetentionMonths);
        Assert.Equal("5 años desde extinción del contrato laboral", act.Retention.PeriodDescription);
    }

    [Fact]
    public void RetentionSection_EmptyDescription_Throws() =>
        Assert.Throws<ArgumentException>(() => RetentionSection.Create(""));

    [Fact]
    public void RetentionSection_NegativeMonths_Throws() =>
        Assert.Throws<ArgumentException>(() => RetentionSection.Create("desc", retentionMonths: -1));

    [Fact]
    public void RetentionSection_NullMonths_OK()
    {
        var r = RetentionSection.Create("Indefinido hasta retiro consentimiento");
        Assert.Null(r.RetentionMonths);
    }

    // ── SecurityMeasureEntry ──────────────────────────────────────────────────

    [Fact]
    public void SetSecurityMeasures_Valid_AssignsList()
    {
        var act = BuildDraft();
        act.SetSecurityMeasures([
            SecurityMeasureEntry.Create(SecurityMeasureType.Technical, "Cifrado AES-256 en reposo"),
            SecurityMeasureEntry.Create(SecurityMeasureType.Organizational, "Capacitación anual RRHH"),
            SecurityMeasureEntry.Create(SecurityMeasureType.Physical, "Control de acceso biométrico")
        ], Guid.NewGuid());

        Assert.Equal(3, act.SecurityMeasures.Count);
        Assert.Equal(SecurityMeasureType.Technical, act.SecurityMeasures[0].MeasureType);
    }

    [Fact]
    public void SecurityMeasureEntry_EmptyDescription_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            SecurityMeasureEntry.Create(SecurityMeasureType.Technical, ""));

    [Fact]
    public void SetSecurityMeasures_WithCatalogId_OK()
    {
        var catalogId = Guid.NewGuid();
        var entry = SecurityMeasureEntry.Create(
            SecurityMeasureType.Technical, "TLS 1.3 en tránsito", catalogId);
        Assert.Equal(catalogId, entry.SecurityMeasureCatalogId);
    }

    // ── Touch propagation ─────────────────────────────────────────────────────

    [Fact]
    public void SetRetention_UpdatesLastModified()
    {
        var act = BuildDraft();
        var userId = Guid.NewGuid();
        act.SetRetention(RetentionSection.Create("5 años"), userId);
        Assert.Equal(userId, act.LastModifiedBy);
    }
}
