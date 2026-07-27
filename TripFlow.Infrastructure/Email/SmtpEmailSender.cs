using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Common;

namespace TripFlow.Infrastructure.Email;

/// <summary>
/// SMTP generico (funciona com Brevo, Mailtrap, etc. - qualquer provedor free tier).
/// Sem host configurado, so loga o e-mail que seria enviado em vez de quebrar - da pra
/// rodar e testar o resto do fluxo de auth sem ter credencial de e-mail configurada.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogWarning("SMTP nao configurado - e-mail para {To} com assunto '{Subject}' nao foi enviado de verdade. Corpo: {Body}", to, subject, htmlBody);
            return;
        }

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            Credentials = new NetworkCredential(_options.User, _options.Password),
            EnableSsl = true
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        await client.SendMailAsync(message, cancellationToken);
    }
}
