using Domain.Auth;

namespace Application.Common.Interfaces;

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAtUtc) CreateAccessToken(ApplicationUser user, IReadOnlyCollection<string> roles);
    (string Token, DateTimeOffset ExpiresAtUtc) CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
