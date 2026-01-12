using System.Security.Authentication;
using Application.Common.Interfaces;
using Domain.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password, string? DeviceId, string? UserAgent) : IRequest<LoginResponse>;

public record LoginResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

public class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IApplicationDbContext dbContext) : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new AuthenticationException("Invalid credentials");
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            throw new AuthenticationException("Invalid credentials");
        }

        // Optional: Check if email is confirmed 
        // if (!await userManager.IsEmailConfirmedAsync(user)) ...

        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = tokenService.CreateAccessToken(user, roles.ToList());

        var refreshToken = tokenService.CreateRefreshToken();
        var refreshTokenHash = tokenService.HashRefreshToken(refreshToken);

        var tokenEntity = new Domain.Auth.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(10), // Configurable in real app
            DeviceId = request.DeviceId,
            UserAgent = request.UserAgent
        };

        dbContext.RefreshTokens.Add(tokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResponse(accessToken, refreshToken, expiresAt);
    }
}
