using Evidata.Modules.Search.Domain;

namespace Evidata.Tests.Unit.Search;

public class SearchQueryLogTests
{
    private static readonly Guid _tenant = Guid.NewGuid();
    private static readonly Guid _user = Guid.NewGuid();

    private static SearchQueryLog Build(int results = 5) =>
        SearchQueryLog.Record(_tenant, _user, "datos personales", "legal", results);

    // ── Record ────────────────────────────────────────────────────────────────

    [Fact]
    public void Record_SetsAllFields()
    {
        var l = Build();
        Assert.Equal(_tenant, l.TenantId);
        Assert.Equal(_user, l.UserId);
        Assert.Equal("datos personales", l.Query);
        Assert.Equal("legal", l.Source);
        Assert.Equal(5, l.ResultCount);
        Assert.Null(l.HadSelection);
        Assert.NotEqual(Guid.Empty, l.Id);
    }

    [Fact]
    public void Record_NormalizesSourceToLowercase()
    {
        var l = SearchQueryLog.Record(_tenant, _user, "query", "LEGAL", 3);
        Assert.Equal("legal", l.Source);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Record_EmptyQuery_Throws(string q)
    {
        Assert.Throws<ArgumentException>(() =>
            SearchQueryLog.Record(_tenant, _user, q, "legal", 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Record_EmptySource_Throws(string src)
    {
        Assert.Throws<ArgumentException>(() =>
            SearchQueryLog.Record(_tenant, _user, "query", src, 0));
    }

    [Fact]
    public void Record_NegativeResultCount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SearchQueryLog.Record(_tenant, _user, "query", "legal", -1));
    }

    // ── RecordSelection ───────────────────────────────────────────────────────

    [Fact]
    public void RecordSelection_SetsHadSelectionTrue()
    {
        var l = Build();
        l.RecordSelection();
        Assert.True(l.HadSelection);
    }

    [Fact]
    public void RecordSelection_CalledTwice_Throws()
    {
        var l = Build();
        l.RecordSelection();
        Assert.Throws<InvalidOperationException>(() => l.RecordSelection());
    }

    [Fact]
    public void RecordSelection_AlreadySetViaFactory_Throws()
    {
        var l = SearchQueryLog.Record(_tenant, _user, "q", "legal", 1, hadSelection: true);
        Assert.Throws<InvalidOperationException>(() => l.RecordSelection());
    }

    // ── ZeroResults ───────────────────────────────────────────────────────────

    [Fact]
    public void Record_ZeroResults_IsValid()
    {
        var l = Build(0);
        Assert.Equal(0, l.ResultCount);
    }

    // ── FiltersJson ───────────────────────────────────────────────────────────

    [Fact]
    public void Record_WithFilters_StoresJson()
    {
        var filters = "{\"module\":\"ProcessingInventory\"}";
        var l = SearchQueryLog.Record(_tenant, _user, "q", "tenant", 2, filtersJson: filters);
        Assert.Equal(filters, l.FiltersJson);
    }

    [Fact]
    public void Record_NoFilters_FiltersJsonIsNull()
    {
        var l = Build();
        Assert.Null(l.FiltersJson);
    }

    // ── MinQueryLength rule ───────────────────────────────────────────────────

    [Fact]
    public void Record_ShortQuery_IsAllowedByDomain_ValidationIsAppLayer()
    {
        // El dominio no valida longitud mínima — esa regla vive en ISearchQueryLogService.MinQueryLength
        // Este test documenta el contrato: el dominio acepta queries cortas
        var l = SearchQueryLog.Record(_tenant, _user, "ab", "legal", 0);
        Assert.Equal("ab", l.Query);
    }
}
