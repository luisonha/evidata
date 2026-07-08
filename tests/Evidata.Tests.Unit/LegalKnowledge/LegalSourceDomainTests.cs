using Evidata.Modules.LegalKnowledge.Domain;
using Xunit;

namespace Evidata.Tests.Unit.LegalKnowledge;

public class LegalSourceDomainTests
{
    private static readonly Guid AdminId = Guid.NewGuid();

    private static LegalSource CreateSample() =>
        LegalSource.Create(
            code: "CL-LEY-21719",
            name: "Ley 21.719 — Protección de Datos Personales",
            jurisdiction: "CL",
            type: LegalSourceType.Law,
            publishedAt: new DateOnly(2024, 12, 13),
            createdBy: AdminId,
            officialUrl: "https://bcn.cl/ley21719");

    [Fact]
    public void Create_ValidParams_SetsExpectedValues()
    {
        var source = CreateSample();

        Assert.Equal("CL-LEY-21719", source.Code);
        Assert.Equal("CL", source.Jurisdiction);
        Assert.Equal(LegalSourceType.Law, source.Type);
        Assert.True(source.IsActive);
        Assert.Empty(source.Versions);
        Assert.NotEqual(Guid.Empty, source.Id);
    }

    [Fact]
    public void Create_CodeAndJurisdiction_NormalizedToUpper()
    {
        var source = LegalSource.Create(
            code: "cl-ley-21719",
            name: "Test",
            jurisdiction: "cl",
            type: LegalSourceType.Law,
            publishedAt: new DateOnly(2024, 1, 1),
            createdBy: AdminId);

        Assert.Equal("CL-LEY-21719", source.Code);
        Assert.Equal("CL", source.Jurisdiction);
    }

    [Fact]
    public void AddVersion_FirstVersion_GetsVersionNumber1()
    {
        var source = CreateSample();

        var v1 = source.AddVersion(
            versionTag: "v1.0",
            effectiveDate: new DateOnly(2024, 12, 13),
            summary: "Texto original promulgado",
            createdBy: AdminId);

        Assert.Single(source.Versions);
        Assert.Equal(1, v1.VersionNumber);
        Assert.Equal("v1.0", v1.VersionTag);
        Assert.Equal(source.Id, v1.LegalSourceId);
    }

    [Fact]
    public void AddVersion_Multiple_VersionNumbersAutoIncrement()
    {
        var source = CreateSample();

        source.AddVersion("v1.0", new DateOnly(2024, 12, 13), "Texto original", AdminId);
        source.AddVersion("v1.1", new DateOnly(2025, 3, 1), "Primera modificación", AdminId);
        var v3 = source.AddVersion("v1.2", new DateOnly(2025, 9, 1), "Segunda modificación", AdminId);

        Assert.Equal(3, source.Versions.Count);
        Assert.Equal(3, v3.VersionNumber);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var source = CreateSample();

        source.Deactivate(AdminId);

        Assert.False(source.IsActive);
        Assert.NotNull(source.UpdatedAt);
        Assert.Equal(AdminId, source.UpdatedBy);
    }

    [Fact]
    public void Create_EmptyCode_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            LegalSource.Create("", "Name", "CL", LegalSourceType.Law,
                new DateOnly(2024, 1, 1), AdminId));
    }

    [Fact]
    public void LegalSourceVersion_EmptyTag_Throws()
    {
        var source = CreateSample();

        Assert.Throws<ArgumentException>(() =>
            source.AddVersion("", new DateOnly(2024, 1, 1), "Summary", AdminId));
    }

    [Fact]
    public void AddVersion_UpdatesSourceUpdatedAt()
    {
        var source = CreateSample();
        var before = source.UpdatedAt;

        source.AddVersion("v1.0", new DateOnly(2024, 12, 13), "Original", AdminId);

        Assert.NotNull(source.UpdatedAt);
        Assert.NotEqual(before, source.UpdatedAt);
    }
}
