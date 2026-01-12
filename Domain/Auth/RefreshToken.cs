namespace Domain.Auth;

/// <summary>
/// Представляет refresh-токен для аутентификации пользователя.
/// </summary>
public sealed class RefreshToken
{
    /// <summary>
    /// Уникальный идентификатор токена.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя, которому принадлежит токен.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Хэш токена.
    /// </summary>
    public string TokenHash { get; set; } = null!;

    /// <summary>
    /// Идентификатор устройства, если применимо.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// User-Agent устройства, если применимо.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Время истечения срока действия токена (UTC).
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Время создания токена (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Время отзыва токена (UTC), если токен был отозван.
    /// </summary>
    public DateTime? RevokedAtUtc { get; private set; }

    /// <summary>
    /// Идентификатор токена, который заменил данный токен, если применимо.
    /// </summary>
    public Guid? ReplacedByTokenId { get; private set; }

    /// <summary>
    /// Возвращает true, если токен истёк.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

    /// <summary>
    /// Возвращает true, если токен был отозван.
    /// </summary>
    public bool IsRevoked => RevokedAtUtc.HasValue;

    /// <summary>
    /// Возвращает true, если токен активен (не истёк и не отозван).
    /// </summary>
    public bool IsActive => !IsExpired && !IsRevoked;

    /// <summary>
    /// Отзывает токен, устанавливая время отзыва и, при необходимости, идентификатор заменяющего токена.
    /// </summary>
    /// <param name="replacedByTokenId">Идентификатор токена, который заменяет данный токен.</param>
    public void Revoke(Guid? replacedByTokenId = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAtUtc = DateTime.UtcNow;
        ReplacedByTokenId = replacedByTokenId;
    }
}
