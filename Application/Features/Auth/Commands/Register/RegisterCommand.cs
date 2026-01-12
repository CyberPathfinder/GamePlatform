using System.ComponentModel.DataAnnotations;
using Application.Common.Interfaces;
using Domain.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Features.Auth.Commands.Register;

public record RegisterCommand(string Email, string Password) : IRequest;

public class RegisterCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender) : IRequestHandler<RegisterCommand>
{
    public async Task Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            throw new ValidationException(string.Join("; ", errors));
        }

        // Add default role if needed
        await userManager.AddToRoleAsync(user, "User");

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await emailSender.SendConfirmEmailAsync(user, token, cancellationToken);
    }
}
