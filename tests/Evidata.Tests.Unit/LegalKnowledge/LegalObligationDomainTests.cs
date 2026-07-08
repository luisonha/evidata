using Evidata.Modules.LegalKnowledge.Domain;
using Xunit;

namespace Evidata.Tests.Unit.LegalKnowledge;

public class LegalObligationDomainTests
{
    private static readonly Guid AdminId = Guid.NewGuid();

    private static LegalObligation CreateSample() =>
        LegalObligation.Create(
            code: "LEY21719-ART6-OBL1",
            title: "Informar derechos del titular",
            legalText: "El responsable deberá informar al titular de forma clara y accesible los derechos que le asisten conforme a esta ley.",
            legalSourceName: "Ley 21.719",
            frequency: ObligationFrequency.OnEvent,
            createdBy: AdminId,
            sourceArticle: "Art. 6°",
            deadlineDays: 30,
            notes: "Aplica en el momento de recolección de datos.");

    [Fact]
    public void Create_ValidParams_SetsExpectedValues()
    {
        var obligation = CreateSample();

        Assert.Equal("LEY21719-ART6-OBL1", obligation.Code);
        Assert.Equal("Informar derechos del titular", obligation.Title);
        Assert.Equal(ObligationStatus.Active, obligation.Status);
        Assert.Equal(ObligationFrequency.OnEvent, obligation.Frequency);
        Assert.Equal(30, obligation.DeadlineDays);
        Assert.Equal("Art. 6°", obligation.SourceArticle);
        Assert.NotEqual(Guid.Empty, obligation.Id);
    }

    [Fact]
    public void Create_CodeNormalized_ToUpperInvariant()
    {
        var obligation = LegalObligation.Create(
            code: "ley21719-art6-obl1",
            title: "Test",
            legalText: "Texto legal",
            legalSourceName: "Ley 21.719",
            frequency: ObligationFrequency.OneTime,
            createdBy: AdminId);

        Assert.Equal("LEY21719-ART6-OBL1", obligation.Code);
    }

    [Fact]
    public void Supersede_ChangesStatusToSuperseded()
    {
        var obligation = CreateSample();

        obligation.Supersede(AdminId);

        Assert.Equal(ObligationStatus.Superseded, obligation.Status);
        Assert.NotNull(obligation.UpdatedAt);
        Assert.Equal(AdminId, obligation.UpdatedBy);
    }

    [Fact]
    public void Repeal_ChangesStatusToRepealed()
    {
        var obligation = CreateSample();

        obligation.Repeal(AdminId);

        Assert.Equal(ObligationStatus.Repealed, obligation.Status);
    }

    [Fact]
    public void UpdateNotes_SetsNotes()
    {
        var obligation = CreateSample();
        const string newNotes = "Nota actualizada tras revisión legal.";

        obligation.UpdateNotes(newNotes, AdminId);

        Assert.Equal(newNotes, obligation.Notes);
        Assert.NotNull(obligation.UpdatedAt);
    }

    [Fact]
    public void Create_EmptyCode_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            LegalObligation.Create("", "Title", "Text", "Ley", ObligationFrequency.OneTime, AdminId));
    }

    [Fact]
    public void Create_NegativeDeadline_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LegalObligation.Create("CODE", "Title", "Text", "Ley", ObligationFrequency.OneTime, AdminId,
                deadlineDays: -1));
    }

    [Fact]
    public void Create_NullDeadline_IsAllowed()
    {
        var obligation = LegalObligation.Create(
            "CODE-ND", "Sin plazo", "Texto", "Ley", ObligationFrequency.Continuous, AdminId);

        Assert.Null(obligation.DeadlineDays);
    }
}
