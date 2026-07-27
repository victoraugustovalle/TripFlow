namespace TripFlow.Application.Common;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "no-reply@tripflow.app";
    public string FromName { get; set; } = "TripFlow";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}
