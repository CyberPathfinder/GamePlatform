using System.Net.Sockets;
using MailKit.Net.Smtp;

namespace Infrastructure.Email;

internal static class SmtpExceptionClassifier
{
    public static bool IsTransient(Exception ex)
    {
        return ex is IOException ||
               ex is TimeoutException ||
               ex is SocketException ||
               ex is SmtpProtocolException ||
               (ex is SmtpCommandException smtpEx && IsTransientStatus(smtpEx.StatusCode));
    }

    private static bool IsTransientStatus(SmtpStatusCode code) =>
        code is SmtpStatusCode.ServiceNotAvailable
            or SmtpStatusCode.MailboxBusy
            or SmtpStatusCode.InsufficientStorage
            or SmtpStatusCode.TransactionFailed;
}
