using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;

namespace FreelancePlatform.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public SmtpEmailSender(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string bodyHtml)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            throw new ArgumentException("Email получателя обязателен.", nameof(toEmail));
        if (string.IsNullOrWhiteSpace(subject))
            subject = "(Без темы)";
        if (string.IsNullOrWhiteSpace(bodyHtml))
            bodyHtml = "<p>Пустое письмо</p>";

        var smtpSection = _config.GetSection("Smtp");

        var host = smtpSection["Host"] ?? throw new InvalidOperationException("Smtp:Host не задан.");
        var fromEmail = smtpSection["FromEmail"] ?? throw new InvalidOperationException("Smtp:FromEmail не задан.");
        var username = smtpSection["Username"] ?? throw new InvalidOperationException("Smtp:Username не задан.");
        var password = smtpSection["Password"] ?? throw new InvalidOperationException("Smtp:Password не задан.");

        
        var port = smtpSection.GetValue<int>("Port", 587);
        var enableSsl = smtpSection.GetValue<bool>("EnableSsl", true);

        var socketOptions = port == 465
            ? SecureSocketOptions.SslOnConnect
            : (enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("FreelancePlatform", fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = bodyHtml };

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(host, port, socketOptions).ConfigureAwait(false);
            await client.AuthenticateAsync(username, password).ConfigureAwait(false);
            await client.SendAsync(message).ConfigureAwait(false);
        }
        finally
        {
            await client.DisconnectAsync(quit: true).ConfigureAwait(false);
        }
    }
}