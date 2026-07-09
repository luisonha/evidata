using Evidata.Functions.Notifications.Email;
using Evidata.Functions.Notifications.Handlers;
using Evidata.Modules.GapManagement.Application.Notifications;
using Evidata.Modules.GapManagement.Domain;
using Evidata.Modules.ProcessingInventory.Application.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace Evidata.Tests.Unit.Notifications;

public class NotificationHandlersTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId  = Guid.NewGuid();
    private static readonly Guid OwnerId  = Guid.NewGuid();
    private static readonly Guid GapId    = Guid.NewGuid();
    private static readonly Guid RatId    = Guid.NewGuid();

    // ── Fake email sender ─────────────────────────────────────────────────────

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];
        public Task SendAsync(EmailMessage message, CancellationToken ct = default)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // GapNotificationHandler
    // ═══════════════════════════════════════════════════════════

    private static GapNotificationHandler BuildGapHandler(FakeEmailSender email) =>
        new(email, NullLogger<GapNotificationHandler>.Instance);

    private static string GapPayload(GapSeverity severity = GapSeverity.High, Guid? ownerId = null) =>
        JsonSerializer.Serialize(new GapEventPayload(
            GapId, TenantId, "Brecha test", severity,
            GapStatus.Open, "GapManagement", Guid.NewGuid(),
            ownerId, DateTimeOffset.UtcNow, ActorId));

    [Fact]
    public async Task GapCreated_sends_email_to_actor()
    {
        var email = new FakeEmailSender();
        var handler = BuildGapHandler(email);

        var result = await handler.HandleAsync(GapEventTypes.GapCreated, GapPayload(), CancellationToken.None);

        Assert.True(result);
        Assert.Single(email.Sent);
        Assert.Contains("Nueva brecha", email.Sent[0].Subject);
    }

    [Fact]
    public async Task GapAssigned_sends_email_when_owner_present()
    {
        var email = new FakeEmailSender();
        var handler = BuildGapHandler(email);

        var result = await handler.HandleAsync(GapEventTypes.GapAssigned, GapPayload(ownerId: OwnerId), CancellationToken.None);

        Assert.True(result);
        Assert.Single(email.Sent);
        Assert.Contains("asignó", email.Sent[0].Subject);
    }

    [Fact]
    public async Task GapAssigned_no_email_when_no_owner()
    {
        var email = new FakeEmailSender();
        var handler = BuildGapHandler(email);

        var result = await handler.HandleAsync(GapEventTypes.GapAssigned, GapPayload(), CancellationToken.None);

        Assert.True(result);
        Assert.Empty(email.Sent);
    }

    [Fact]
    public async Task GapBlocked_sends_email_only_for_high_severity()
    {
        var email = new FakeEmailSender();
        var handler = BuildGapHandler(email);

        await handler.HandleAsync(GapEventTypes.GapBlocked, GapPayload(GapSeverity.High), CancellationToken.None);
        Assert.Single(email.Sent);

        email.Sent.Clear();
        await handler.HandleAsync(GapEventTypes.GapBlocked, GapPayload(GapSeverity.Low), CancellationToken.None);
        Assert.Empty(email.Sent);
    }

    [Fact]
    public async Task GapBlocked_sends_email_for_critical_severity()
    {
        var email = new FakeEmailSender();
        var handler = BuildGapHandler(email);

        var result = await handler.HandleAsync(GapEventTypes.GapBlocked, GapPayload(GapSeverity.Critical), CancellationToken.None);

        Assert.True(result);
        Assert.Single(email.Sent);
        Assert.Contains("bloqueada", email.Sent[0].Subject);
    }

    [Fact]
    public async Task GapResolved_sends_confirmation_email()
    {
        var email = new FakeEmailSender();
        var handler = BuildGapHandler(email);

        var result = await handler.HandleAsync(GapEventTypes.GapResolved, GapPayload(), CancellationToken.None);

        Assert.True(result);
        Assert.Single(email.Sent);
        Assert.Contains("resuelta", email.Sent[0].Subject);
    }

    [Fact]
    public async Task GapProgressed_returns_true_no_email()
    {
        var email = new FakeEmailSender();
        var handler = BuildGapHandler(email);

        var result = await handler.HandleAsync(GapEventTypes.GapProgressed, GapPayload(), CancellationToken.None);

        Assert.True(result);
        Assert.Empty(email.Sent);
    }

    [Fact]
    public async Task Unknown_gap_type_returns_false()
    {
        var email = new FakeEmailSender();
        var handler = BuildGapHandler(email);

        var result = await handler.HandleAsync("gap.unknown.v99", GapPayload(), CancellationToken.None);

        Assert.False(result);
        Assert.Empty(email.Sent);
    }

    [Fact]
    public async Task Email_failure_does_not_throw()
    {
        var failingSender = new FailingEmailSender();
        var handler = new GapNotificationHandler(failingSender, NullLogger<GapNotificationHandler>.Instance);

        // No debe relanzar — el mensaje ya fue procesado
        var result = await handler.HandleAsync(GapEventTypes.GapCreated, GapPayload(), CancellationToken.None);

        Assert.True(result);
    }

    private sealed class FailingEmailSender : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken ct = default) =>
            Task.FromException(new InvalidOperationException("SMTP not available"));
    }

    // ═══════════════════════════════════════════════════════════
    // RatNotificationHandler
    // ═══════════════════════════════════════════════════════════

    private static RatNotificationHandler BuildRatHandler(FakeEmailSender email) =>
        new(email, NullLogger<RatNotificationHandler>.Instance);

    private static string RatPayload(string? reason = null) =>
        JsonSerializer.Serialize(new RatEventPayload(
            RatId, TenantId, "Gestión de Nómina", "UnderReview",
            ActorId, "actor@empresa.com", DateTimeOffset.UtcNow, reason));

    [Fact]
    public async Task RatSubmittedForReview_sends_email()
    {
        var email = new FakeEmailSender();
        var handler = BuildRatHandler(email);

        var result = await handler.HandleAsync(RatEventTypes.RatSubmittedForReview, RatPayload(), CancellationToken.None);

        Assert.True(result);
        Assert.Single(email.Sent);
        Assert.Contains("revisión", email.Sent[0].Subject);
        Assert.Equal("actor@empresa.com", email.Sent[0].ToAddress);
    }

    [Fact]
    public async Task RatApproved_sends_confirmation_email()
    {
        var email = new FakeEmailSender();
        var handler = BuildRatHandler(email);

        var result = await handler.HandleAsync(RatEventTypes.RatApproved, RatPayload(), CancellationToken.None);

        Assert.True(result);
        Assert.Single(email.Sent);
        Assert.Contains("aprobado", email.Sent[0].Subject);
    }

    [Fact]
    public async Task RatRejected_sends_email_with_reason()
    {
        var email = new FakeEmailSender();
        var handler = BuildRatHandler(email);

        var result = await handler.HandleAsync(RatEventTypes.RatRejected, RatPayload("Falta base de licitud"), CancellationToken.None);

        Assert.True(result);
        Assert.Single(email.Sent);
        Assert.Contains("correcciones", email.Sent[0].Subject);
        Assert.Contains("Falta base de licitud", email.Sent[0].HtmlBody);
    }

    [Fact]
    public async Task RatArchived_returns_true_no_email()
    {
        var email = new FakeEmailSender();
        var handler = BuildRatHandler(email);

        var result = await handler.HandleAsync(RatEventTypes.RatArchived, RatPayload(), CancellationToken.None);

        Assert.True(result);
        Assert.Empty(email.Sent);
    }

    [Fact]
    public async Task Unknown_rat_type_returns_false()
    {
        var email = new FakeEmailSender();
        var handler = BuildRatHandler(email);

        var result = await handler.HandleAsync("rat.unknown.v99", RatPayload(), CancellationToken.None);

        Assert.False(result);
    }

    // ═══════════════════════════════════════════════════════════
    // EmailTemplates
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void EmailTemplates_gap_created_contains_title()
    {
        var html = EmailTemplates.GapCreated("Brecha Privacidad", "Critical", "MiEmpresa", null);
        Assert.Contains("Brecha Privacidad", html);
        Assert.Contains("Critical", html);
    }

    [Fact]
    public void EmailTemplates_rat_rejected_contains_reason()
    {
        var html = EmailTemplates.RatRejected("RAT Test", "MiEmpresa", "dpo@empresa.com", "Falta articulo 12");
        Assert.Contains("Falta articulo 12", html);
    }

    [Fact]
    public void EmailTemplates_html_escapes_special_chars()
    {
        var html = EmailTemplates.GapCreated("<script>alert('xss')</script>", "Low", "Empresa", null);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }
}
