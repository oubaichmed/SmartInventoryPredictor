namespace SmartInventoryPredictor.API.Models.Auth;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 1440; // 24 hours default
    public int RefreshTokenExpiryDays { get; set; } = 30;
    public string Algorithm { get; set; } = "HS256";
}

public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public string? Username { get; set; }
    public string? Role { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Error { get; set; }
}

public class RefreshToken
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; }
}