namespace Sycota.Web.Models;

public class EmailSettings
{
    public string ResendApiKey { get; set; } = string.Empty;
    public string ResendBaseUrl { get; set; } = "https://api.resend.com";
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "SYCOTA+";
    public string? ReplyTo { get; set; }
    public string ContactRecipientEmail { get; set; } = string.Empty;
}
