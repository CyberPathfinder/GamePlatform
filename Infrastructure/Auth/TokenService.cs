using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces;
using Domain.Auth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Auth;

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;
    private readonly byte[] _refreshPepper;

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        var secretBytes = Encoding.UTF8.GetBytes(_options.Secret);
        if (secretBytes.Length < 32)
        {
            throw new InvalidOperationException("JWT secret must be at least 32 bytes.");
        }

        var key = new SymmetricSecurityKey(secretBytes);
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // В идеале: отдельный secret из конфигурации, например JwtOptions.RefreshPepper
        _refreshPepper = Encoding.UTF8.GetBytes(_options.RefreshPepper);
        if (_refreshPepper.Length < 32)
        {
            throw new InvalidOperationException("Refresh pepper must be at least 32 bytes.");
        }
    }

    public (string Token, DateTimeOffset ExpiresAtUtc) CreateAccessToken(ApplicationUser user, IReadOnlyCollection<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes);

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: _signingCredentials);

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expiresAtUtc);
    }

    public (string Token, DateTimeOffset ExpiresAtUtc) CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = WebEncoders.Base64UrlEncode(bytes);
        var expiresAtUtc = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenExpirationDays);
        return (token, expiresAtUtc);
    }

    public string HashRefreshToken(string refreshToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        var hash = HMACSHA256.HashData(_refreshPepper, tokenBytes);
        return Convert.ToBase64String(hash);
    }
}
