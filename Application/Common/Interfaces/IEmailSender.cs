using Domain.Auth;

namespace Application.Common.Interfaces;

public interface IEmailSender
{
    Task SendConfirmEmailAsync(ApplicationUser user, string token, CancellationToken ct = default);

    Task SendResetPasswordAsync(ApplicationUser user, string token, CancellationToken ct = default);
}
