using Infrastructure.Auth;

namespace Infrastructure.Email;

public interface IEmailSender
{
    Task SendConfirmEmailAsync(ApplicationUser user, string token, CancellationToken ct = default);

    Task SendResetPasswordAsync(ApplicationUser user, string token, CancellationToken ct = default);
}
