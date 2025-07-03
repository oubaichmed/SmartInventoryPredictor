using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SmartInventoryPredictor.API.Models.Auth;

namespace SmartInventoryPredictor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IOptions<JwtSettings> jwtSettings, ILogger<AuthController> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Invalid model data"
                });
            }

            // Validate user credentials
            var userInfo = await ValidateUserAsync(model.Username, model.Password);
            if (userInfo == null)
            {
                _logger.LogWarning("Failed login attempt for username: {Username}", model.Username);
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                });
            }

            // Generate tokens
            var token = GenerateJwtToken(userInfo);
            var refreshToken = GenerateRefreshToken();

            _logger.LogInformation("User {Username} logged in successfully", model.Username);

            return Ok(new LoginResponse
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                Username = userInfo.Username,
                Role = userInfo.Role,
                Message = "Login successful"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", model.Username);
            return StatusCode(500, new LoginResponse
            {
                Success = false,
                Message = "An error occurred during login"
            });
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Invalid refresh token"
                });
            }

            // In production, validate refresh token from database
            var isValidRefreshToken = await ValidateRefreshTokenAsync(request.RefreshToken);
            if (!isValidRefreshToken)
            {
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Message = "Invalid or expired refresh token"
                });
            }

            // For demo, extract username from refresh token (in production, get from database)
            var username = ExtractUsernameFromRefreshToken(request.RefreshToken);
            var userInfo = await GetUserInfoAsync(username);

            if (userInfo == null)
            {
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            // Generate new tokens
            var newToken = GenerateJwtToken(userInfo);
            var newRefreshToken = GenerateRefreshToken();

            return Ok(new LoginResponse
            {
                Success = true,
                Token = newToken,
                RefreshToken = newRefreshToken,
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                Username = userInfo.Username,
                Role = userInfo.Role,
                Message = "Token refreshed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new LoginResponse
            {
                Success = false,
                Message = "An error occurred during token refresh"
            });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(200)]
    public async Task<ActionResult> Logout()
    {
        try
        {
            var username = User.Identity?.Name;

            // In production, invalidate refresh tokens in database
            if (!string.IsNullOrEmpty(username))
            {
                await InvalidateUserRefreshTokensAsync(username);
                _logger.LogInformation("User {Username} logged out", username);
            }

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }

    [HttpGet("validate")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> ValidateToken()
    {
        try
        {
            var username = User.Identity?.Name;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            return Ok(new
            {
                valid = true,
                username = username,
                role = role,
                expires = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token validation");
            return StatusCode(500, new { message = "An error occurred during token validation" });
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(object), 400)]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid model data" });
            }

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest(new { message = "User not found" });
            }

            // Validate current password
            var isCurrentPasswordValid = await ValidatePasswordAsync(username, model.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }

            // Change password (in production, hash and store in database)
            var success = await ChangeUserPasswordAsync(username, model.NewPassword);
            if (!success)
            {
                return BadRequest(new { message = "Failed to change password" });
            }

            _logger.LogInformation("Password changed for user {Username}", username);
            return Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(500, new { message = "An error occurred while changing password" });
        }
    }

    [HttpGet("user-info")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfoDto), 200)]
    public async Task<ActionResult<UserInfoDto>> GetUserInfo()
    {
        try
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest(new { message = "User not found" });
            }

            var userInfo = await GetUserInfoAsync(username);
            if (userInfo == null)
            {
                return NotFound(new { message = "User information not found" });
            }

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user info");
            return StatusCode(500, new { message = "An error occurred while retrieving user information" });
        }
    }

    #region Private Methods

    private async Task<UserInfoDto?> ValidateUserAsync(string username, string password)
    {
        // Demo authentication - in production, validate against database/Identity
        var validUsers = new Dictionary<string, (string password, string role)>
       {
           { "admin", ("admin123", "Administrator") },
           { "demo", ("demo123", "User") },
           { "manager", ("manager123", "Manager") },
           { "user", ("user123", "User") },
           { "analyst", ("analyst123", "Analyst") }
       };

        if (validUsers.ContainsKey(username.ToLower()) &&
            validUsers[username.ToLower()].password == password)
        {
            var userRole = validUsers[username.ToLower()].role;
            return new UserInfoDto
            {
                Username = username,
                Role = userRole,
                LastLogin = DateTime.UtcNow,
                IsActive = true
            };
        }

        return null;
    }

    private string GenerateJwtToken(UserInfoDto userInfo)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
           new Claim(ClaimTypes.Name, userInfo.Username),
           new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
           new Claim(ClaimTypes.Role, userInfo.Role),
           new Claim("username", userInfo.Username),
           new Claim("role", userInfo.Role),
           new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
           new Claim(JwtRegisteredClaimNames.Iat,
               new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
               ClaimValueTypes.Integer64),
           new Claim(JwtRegisteredClaimNames.Sub, userInfo.Username),
           new Claim(JwtRegisteredClaimNames.Aud, _jwtSettings.Audience),
           new Claim(JwtRegisteredClaimNames.Iss, _jwtSettings.Issuer)
       };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var refreshToken = Convert.ToBase64String(randomBytes);

        // In production, store refresh token in database with expiration
        return refreshToken;
    }

    private async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
    {
        // In production, validate against database
        // Check if token exists, not expired, and not revoked
        await Task.CompletedTask;

        // For demo, accept any non-empty token
        return !string.IsNullOrEmpty(refreshToken) && refreshToken.Length > 10;
    }

    private string ExtractUsernameFromRefreshToken(string refreshToken)
    {
        // In production, lookup username from database using refresh token
        // For demo, return default user
        return "demo";
    }

    private async Task<UserInfoDto?> GetUserInfoAsync(string username)
    {
        // In production, fetch from database
        await Task.CompletedTask;

        var userRole = username.ToLower() switch
        {
            "admin" => "Administrator",
            "manager" => "Manager",
            "analyst" => "Analyst",
            _ => "User"
        };

        return new UserInfoDto
        {
            Username = username,
            Role = userRole,
            LastLogin = DateTime.UtcNow,
            IsActive = true
        };
    }

    private async Task InvalidateUserRefreshTokensAsync(string username)
    {
        // In production, mark all refresh tokens for user as revoked
        await Task.CompletedTask;
        _logger.LogInformation("Invalidated refresh tokens for user {Username}", username);
    }

    private async Task<bool> ValidatePasswordAsync(string username, string password)
    {
        // In production, validate hashed password from database
        await Task.CompletedTask;

        var validUsers = new Dictionary<string, string>
       {
           { "admin", "admin123" },
           { "demo", "demo123" },
           { "manager", "manager123" },
           { "user", "user123" },
           { "analyst", "analyst123" }
       };

        return validUsers.ContainsKey(username.ToLower()) &&
               validUsers[username.ToLower()] == password;
    }

    private async Task<bool> ChangeUserPasswordAsync(string username, string newPassword)
    {
        // In production, hash password and update in database
        await Task.CompletedTask;

        // For demo, always return success
        return true;
    }

    #endregion
}