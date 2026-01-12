namespace Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = null!;
    public int SmtpPort { get; set; } = 587;
    public string SecureSocketOptions { get; set; } = "StartTls";

    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }

    public string FromAddress { get; set; } = null!;
    public string FromName { get; set; } = "GameVault";

    public string ApplicationBaseUrl { get; set; } = null!;

    /// <summary>MailKit SmtpClient.Timeout in milliseconds</summary>
    public int Timeout { get; set; } = 10000;

    public bool IgnoreCertificateErrors { get; set; }
    public int RetryCount { get; set; } = 3;
}