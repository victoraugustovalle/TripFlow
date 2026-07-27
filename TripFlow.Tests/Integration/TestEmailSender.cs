using System.Collections.Concurrent;
using TripFlow.Application.Abstractions;

namespace TripFlow.Tests.Integration;

/// <summary>Substitui o SmtpEmailSender nos testes - so guarda o que seria enviado, pra
/// da pra extrair codigo de confirmacao/token de convite sem precisar de um SMTP de verdade.</summary>
public class TestEmailSender : IEmailSender
{
    public ConcurrentBag<(string To, string Subject, string Body)> SentEmails { get; } = new();

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        SentEmails.Add((to, subject, htmlBody));
        return Task.CompletedTask;
    }

    public string? GetCode(string to, string subject)
    {
        var email = SentEmails.FirstOrDefault(e => e.To == to && e.Subject == subject);
        if (email == default)
            return null;

        var match = System.Text.RegularExpressions.Regex.Match(email.Body, "<strong>(.*?)</strong>");
        return match.Success ? match.Groups[1].Value : null;
    }
}
