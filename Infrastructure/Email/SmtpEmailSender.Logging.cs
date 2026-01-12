using MailKit.Security;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Email;

public sealed partial class SmtpEmailSender
{
    private static readonly Action<ILogger, int, TimeSpan, string, Exception?> LogRetrying =
        LoggerMessage.Define<int, TimeSpan, string>(
            LogLevel.Warning,
            new EventId(1000, nameof(LogRetrying)),
            "Retry {Attempt} after {Delay} for email to {To}");

    private static readonly Action<ILogger, Exception?> LogCertificateValidationDisabled =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1001, nameof(LogCertificateValidationDisabled)),
            "Certificate validation is disabled for SMTP. Use only in development.");

    private static readonly Action<ILogger, string, int, SecureSocketOptions, Exception?> LogConnecting =
        LoggerMessage.Define<string, int, SecureSocketOptions>(
            LogLevel.Debug,
            new EventId(1002, nameof(LogConnecting)),
            "Connecting to SMTP server {Host}:{Port} with {SecureOption}");

    private static readonly Action<ILogger, string, Exception?> LogAuthenticating =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(1003, nameof(LogAuthenticating)),
            "Authenticating as {User}");

    private static readonly Action<ILogger, string, string, Exception?> LogSendingEmail =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1004, nameof(LogSendingEmail)),
            "Sending email to {To} with subject '{Subject}'");

    private static readonly Action<ILogger, string, Exception?> LogEmailSent =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(1005, nameof(LogEmailSent)),
            "Email sent successfully. Server response: {Response}");

    private static readonly Action<ILogger, string, int, Exception?> LogSendFailed =
        LoggerMessage.Define<string, int>(
            LogLevel.Error,
            new EventId(1006, nameof(LogSendFailed)),
            "Email sending failed to {To} after {Attempts} attempts");

    private static readonly Action<ILogger, string?, Exception?> LogSmtpAuthFailed =
        LoggerMessage.Define<string?>(
            LogLevel.Error,
            new EventId(2000, nameof(LogSmtpAuthFailed)),
            "SMTP authentication failed for {User}");

    private static readonly Action<ILogger, string, Exception?> LogRecipientRejected =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2001, nameof(LogRecipientRejected)),
            "Recipient {To} was not accepted by SMTP server");

    private static readonly Action<ILogger, string, Exception?> LogSmtpCommandError =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2002, nameof(LogSmtpCommandError)),
            "SMTP command error sending to {To}");

    private static readonly Action<ILogger, string, Exception?> LogNetworkError =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2003, nameof(LogNetworkError)),
            "Network error sending email to {To}");

    private static readonly Action<ILogger, string, Exception?> LogEmailCancelled =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(2004, nameof(LogEmailCancelled)),
            "Email sending to {To} was cancelled");

    private static readonly Action<ILogger, Exception?> LogErrorDisposingClient =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(2005, nameof(LogErrorDisposingClient)),
            "Error disposing SMTP client");
}
