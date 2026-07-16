using Microsoft.AspNetCore.Identity.UI.Services;

namespace FreelancePlatform.FreelancePlatform.Tests.Web;

/// <summary>
/// Тестовая реализация сервиса отправки электронной почты.
/// Используется вместо реального EmailSender в модульных тестах,
/// чтобы избежать отправки настоящих писем.
/// </summary>
public class FakeEmailSender : IEmailSender
{
    /// <summary>
    /// Имитирует отправку электронного письма без выполнения реальной отправки.
    /// Используется в тестовой среде для проверки логики, зависящей от IEmailSender.
    /// </summary>
    /// <param name="toEmail">Адрес получателя письма.</param>
    /// <param name="subject">Тема письма.</param>
    /// <param name="body">Содержимое письма.</param>
    /// <returns>Завершенную задачу без выполнения отправки письма.</returns>
    public Task SendEmailAsync(string toEmail, string subject, string body)
    {
        // Ничего не делаем
        return Task.CompletedTask;
    }
}