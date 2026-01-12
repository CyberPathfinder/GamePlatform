using Application.Common.Interfaces;
using Domain.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Features.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest;

public class ForgotPasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender) : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        // Security: always return success to avoid enumerating users
        if (user == null || !await userManager.IsEmailConfirmedAsync(user))
        {
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        await emailSender.SendResetPasswordAsync(user, token, cancellationToken);
    }
}
