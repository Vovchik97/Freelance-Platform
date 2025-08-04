using Microsoft.AspNetCore.Identity.UI.Services;

namespace FreelancePlatform.FreelancePlatform.Tests.Web;

public class FakeEmailSender : IEmailSender
{
    public Task SendEmailAsync(string toEmail, string subject, string body)
    {
        // Ничего не делаем
        return Task.CompletedTask;
    }
}