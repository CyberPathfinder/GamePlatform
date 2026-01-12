using System.ComponentModel.DataAnnotations;
using Domain.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Application.Features.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(string UserId, string Token, string NewPassword) : IRequest;

public class ResetPasswordCommandHandler(UserManager<ApplicationUser> userManager) : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
             // Do not reveal if user exists. We might log this.
             throw new ValidationException("Invalid password reset request.");
        }

        var decodedTokenBytes = WebEncoders.Base64UrlDecode(request.Token);
        var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);

        var result = await userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
        if (!result.Succeeded)
        {
             var errors = string.Join("; ", result.Errors.Select(e => e.Description));
             throw new ValidationException($"Password reset failed: {errors}");
        }
    }
}
