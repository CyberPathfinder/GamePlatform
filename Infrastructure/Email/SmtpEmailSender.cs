using System.Text;
using Application.Common.Interfaces;
using Domain.Auth;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;

namespace Infrastructure.Email;

public sealed partial class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly IHostEnvironment _environment;
    private readonly string _baseUrl;
    private readonly SecureSocketOptions _socketOptions;

    public SmtpEmailSender(
        IOptions<EmailOptions> options,
        ILogger<SmtpEmailSender> logger,
        IHostEnvironment environment)
    {
        _options = options.Value;
        _logger = logger;
        _environment = environment;

        ValidateConfiguration(_options);

        _baseUrl = _options.ApplicationBaseUrl.TrimEnd('/');

        if (!Enum.TryParse(_options.SecureSocketOptions, ignoreCase: true, out _socketOptions))
        {
            throw new InvalidOperationException(
                "Email:SecureSocketOptions must be one of: None, StartTls, SslOnConnect, Auto.");
        }
    }

    public async Task SendConfirmEmailAsync(ApplicationUser user, string token, CancellationToken ct = default)
    {
        var to = RequireEmail(user);

        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var url = $"{_baseUrl}/api/v1/auth/confirm-email?userId={user.Id}&token={encodedToken}";

        const string subject = "Confirm your GameVault account";
        var html = EmailTemplates.ConfirmEmailHtml(url);

        var context = new Dictionary<string, object>
        {
            ["To"] = to,
            ["Purpose"] = "ConfirmEmail",
        };

        await ExecuteWithRetryAsync(
            context,
            cancellation => SendAsync(to, subject, html, cancellation),
            ct);
    }

    public async Task SendResetPasswordAsync(ApplicationUser user, string token, CancellationToken ct = default)
    {
        var to = RequireEmail(user);

        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var url = $"{_baseUrl}/api/v1/auth/reset-password?userId={user.Id}&token={encodedToken}";

        const string subject = "Reset your GameVault password";
        var html = EmailTemplates.ResetPasswordHtml(url, _baseUrl);

        var context = new Dictionary<string, object>
        {
            ["To"] = to,
            ["Purpose"] = "ResetPassword",
        };

        await ExecuteWithRetryAsync(
            context,
            cancellation => SendAsync(to, subject, html, cancellation),
            ct);
    }

    private async Task ExecuteWithRetryAsync(
        Dictionary<string, object> context,
        Func<CancellationToken, Task> action,
        CancellationToken ct)
    {
        var retryCount = Math.Max(0, _options.RetryCount);

        for (var attempt = 1; attempt <= retryCount + 1; attempt++)
        {
            var toValue = context.TryGetValue("To", out var toRaw) ? toRaw?.ToString() ?? "unknown" : "unknown";

            try
            {
                await action(ct);
                return;
            }
            catch (Exception ex)
            {
                if (SmtpExceptionClassifier.IsTransient(ex) && attempt <= retryCount)
                {
                    var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250));
                    var delay = baseDelay + jitter;

                    LogRetrying(
                        _logger,
                        attempt,
                        delay,
                        toValue,
                        ex);

                    await Task.Delay(delay, ct);
                    continue;
                }

                LogSendFailed(_logger, toValue, attempt, ex);
                throw;
            }
        }
    }

    private async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        using var message = BuildMessage(to, subject, htmlBody);

        using var client = new SmtpClient();

        try
        {
            client.Timeout = Math.Max(1_000, _options.Timeout);

            if (_options.IgnoreCertificateErrors)
            {
                if (!_environment.IsDevelopment())
                {
                    throw new InvalidOperationException("IgnoreCertificateErrors can only be enabled in Development.");
                }

#pragma warning disable CA5359
#pragma warning disable S4830
                client.ServerCertificateValidationCallback = (_, _, _, _) => true;
#pragma warning restore S4830
#pragma warning restore CA5359
                LogCertificateValidationDisabled(_logger, null);
            }

            LogConnecting(_logger, _options.SmtpHost, _options.SmtpPort, _socketOptions, null);

            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, _socketOptions, ct);

            if (!string.IsNullOrWhiteSpace(_options.SmtpUser))
            {
                LogAuthenticating(_logger, _options.SmtpUser, null);
                await client.AuthenticateAsync(_options.SmtpUser, _options.SmtpPassword ?? string.Empty, ct);
            }

            LogSendingEmail(_logger, to, subject, null);

            var response = await client.SendAsync(message, ct);
            LogEmailSent(_logger, response, null);
        }
        catch (AuthenticationException ex)
        {
            LogSmtpAuthFailed(_logger, _options.SmtpUser, ex);
            throw new InvalidOperationException("Email authentication failed. Check SMTP credentials.", ex);
        }
        catch (SmtpCommandException ex) when (ex.ErrorCode == SmtpErrorCode.RecipientNotAccepted)
        {
            LogRecipientRejected(_logger, to, ex);
            throw new InvalidOperationException($"Email address '{to}' was rejected by the server.", ex);
        }
        catch (SmtpCommandException ex)
        {
            LogSmtpCommandError(_logger, to, ex);
            throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            LogNetworkError(_logger, to, ex);
            throw new InvalidOperationException("Network error while sending email.", ex);
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            LogEmailCancelled(_logger, to, ex);
            throw;
        }
        finally
        {
            if (client.IsConnected)
            {
                try
                {
                    await client.DisconnectAsync(true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    LogErrorDisposingClient(_logger, ex);
                }
            }
        }
    }

    private MimeMessage BuildMessage(string to, string subject, string htmlBody)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Date = DateTimeOffset.UtcNow.UtcDateTime;

        var fromDomain = MailboxAddress.Parse(_options.FromAddress).Address.Split('@').LastOrDefault();
        message.MessageId = !string.IsNullOrWhiteSpace(fromDomain)
            ? MimeUtils.GenerateMessageId(fromDomain)
            : MimeUtils.GenerateMessageId();

        message.Headers.Replace(HeaderId.UserAgent, "GameVault Mailer");

        var plainText = HtmlToTextConverter.Convert(htmlBody);

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = plainText,
        };

        message.Body = bodyBuilder.ToMessageBody();
        return message;
    }

    private static string RequireEmail(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException($"User {user.Id} has no email address.");
        }

        if (!MailboxAddress.TryParse(user.Email, out _))
        {
            throw new InvalidOperationException($"User {user.Id} has invalid email address: {user.Email}");
        }

        return user.Email;
    }

    private static void ValidateConfiguration(EmailOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.ApplicationBaseUrl))
        {
            throw new InvalidOperationException("Email:ApplicationBaseUrl is required.");
        }

        if (!Uri.TryCreate(o.ApplicationBaseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Email:ApplicationBaseUrl must be an absolute http/https URL.");
        }

        if (string.IsNullOrWhiteSpace(o.SmtpHost))
        {
            throw new InvalidOperationException("Email:SmtpHost is required.");
        }

        if (o.SmtpPort is <= 0 or > 65535)
        {
            throw new InvalidOperationException("Email:SmtpPort must be between 1 and 65535.");
        }

        if (string.IsNullOrWhiteSpace(o.FromAddress))
        {
            throw new InvalidOperationException("Email:FromAddress is required.");
        }

        if (!MailboxAddress.TryParse(o.FromAddress, out _))
        {
            throw new InvalidOperationException("Email:FromAddress is not a valid email address.");
        }

        if (o.Timeout <= 0)
        {
            throw new InvalidOperationException("Email:Timeout must be greater than zero.");
        }

        if (o.RetryCount < 0)
        {
            throw new InvalidOperationException("Email:RetryCount cannot be negative.");
        }
    }
}
