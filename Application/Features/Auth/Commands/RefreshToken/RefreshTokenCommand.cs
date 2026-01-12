using System.Security.Authentication;
using Application.Common.Interfaces;
using Domain.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken, string? DeviceId, string? UserAgent) : IRequest<RefreshTokenResponse>;

public record RefreshTokenResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

public class RefreshTokenCommandHandler(
    ITokenService tokenService,
    IApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        ApplicationUser? user = null;
        string newRefreshToken = null!;

        try
        {
            var existingToken = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

            if (existingToken == null)
            {
                throw new AuthenticationException("Invalid token");
            }

            if (existingToken.IsRevoked)
            {
                // Security: If a revoked token is used, it might be token theft. Revoke all descendant tokens.
                // Simplified for MVP: just fail.
                throw new AuthenticationException("Token revoked");
            }

            if (existingToken.IsExpired)
            {
                throw new AuthenticationException("Token expired");
            }

            // Strict Device Validation
            if (!string.IsNullOrEmpty(existingToken.DeviceId))
            {
                if (string.IsNullOrEmpty(request.DeviceId) || existingToken.DeviceId != request.DeviceId)
                {
                    throw new AuthenticationException("Invalid device");
                }
            }

            user = await userManager.FindByIdAsync(existingToken.UserId.ToString());
            if (user == null)
            {
                throw new AuthenticationException("User not found");
            }

            // Rotate token
            var (token, tokenExpiresAt) = tokenService.CreateRefreshToken();
            newRefreshToken = token;
            // Capture this for the response
            
            var newRefreshTokenHash = tokenService.HashRefreshToken(newRefreshToken);

            var newTokenEntity = new Domain.Auth.RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = newRefreshTokenHash,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = tokenExpiresAt.UtcDateTime,
                DeviceId = request.DeviceId,
                UserAgent = request.UserAgent
            };

            // Revoke old token
            existingToken.Revoke(newTokenEntity.Id);

            dbContext.RefreshTokens.Add(newTokenEntity);
            
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            // Assign outer variables for response
            // (done above for user and newRefreshToken)
        }
        catch (DbUpdateConcurrencyException)
        {
            try
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            catch
            {
                // Ignore rollback failure to prioritize the original concurrency exception signal
            }
            throw new AuthenticationException("Token has already been used");
        }
        catch
        {
             try
             {
                 await transaction.RollbackAsync(cancellationToken);
             }
             catch
             {
                 // Ignore rollback failure so the original exception is thrown
             }
             throw;
        }

        var roles = await userManager.GetRolesAsync(user!);
        var (accessToken, accessTokenExpiresAt) = tokenService.CreateAccessToken(user!, roles.ToList());

        return new RefreshTokenResponse(accessToken, newRefreshToken, accessTokenExpiresAt);
    }
}
