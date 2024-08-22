using Hangfire;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

namespace starterapi.Services;

public interface IEmailService
{
    void EnqueuePasswordResetEmail(string email, string token);
    Task SendEmailAsync(string email, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void EnqueuePasswordResetEmail(string email, string token)
    {
        BackgroundJob.Enqueue(() => SendPasswordResetEmailAsync(email, token));
    }

    public async Task SendPasswordResetEmailAsync(string email, string token)
    {
        await SendEmailAsync(email, "Password Reset Request", $"Your password reset token is: {token}");
    }

    public async Task SendEmailAsync(string email, string subject, string body)
    {
        var client = new MailjetClient(
            _configuration["Mailjet:ApiKey"],
            _configuration["Mailjet:ApiSecret"]
        );

        var request = new MailjetRequest
        {
            Resource = SendV31.Resource,
        }
        .Property(Send.Messages, new JArray {
            new JObject {
                {"From", new JObject {
                    {"Email", _configuration["Mailjet:SenderEmail"]},
                    {"Name", "Your App Name"}
                }},
                {"To", new JArray {
                    new JObject {
                        {"Email", email},
                        {"Name", "User"}
                    }
                }},
                {"Subject", subject},
                {"TextPart", body},
                {"HTMLPart", $"<h3>{subject}</h3><p>{body}</p>"}
            }
        });

        var response = await client.PostAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to send email to {email}. Status code: {response.StatusCode}");
        }
    }
}