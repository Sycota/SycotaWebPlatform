using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using Sycota.Web.Models;

namespace Sycota.Web.Services;

public class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly EmailSettings _settings;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(HttpClient httpClient, IOptions<EmailSettings> settings, ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (string.IsNullOrWhiteSpace(_settings.ResendApiKey) || string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            throw new InvalidOperationException("Resend email settings are not configured.");
        }

        var payload = new Dictionary<string, object?>
        {
            ["from"] = $"{_settings.FromName} <{_settings.FromEmail}>",
            ["to"] = new[] { email },
            ["subject"] = subject,
            ["html"] = htmlMessage
        };

        if (!string.IsNullOrWhiteSpace(_settings.ReplyTo))
        {
            payload["reply_to"] = _settings.ReplyTo;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/emails")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ResendApiKey);

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Resend failed with status {StatusCode}. Response: {Response}", response.StatusCode, responseBody);
            throw new InvalidOperationException("Email sending failed via Resend.");
        }

        _logger.LogInformation("Email sent to {Email} with subject {Subject} via Resend", email, subject);
    }
}
