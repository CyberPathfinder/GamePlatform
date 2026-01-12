using System.ComponentModel.DataAnnotations;
using Domain.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Application.Features.Auth.Commands.ConfirmEmail;

public record ConfirmEmailCommand(string UserId, string Token) : IRequest;

public class ConfirmEmailCommandHandler(UserManager<ApplicationUser> userManager) : IRequestHandler<ConfirmEmailCommand>
{
    public async Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            // Do not reveal if user exists
            throw new ValidationException("Invalid email confirmation request.");
        }

        var decodedTokenBytes = WebEncoders.Base64UrlDecode(request.Token);
        var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);

        var result = await userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
        {
            throw new ValidationException("Email confirmation failed.");
        }
    }
}
