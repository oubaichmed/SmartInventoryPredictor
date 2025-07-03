using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartInventoryPredictor.Client.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<AuthService> _logger;
    private string? _cachedToken;

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime, ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var loginRequest = new { Username = username, Password = password };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.Token))
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", loginResponse.Token);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", loginResponse.RefreshToken);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "username", loginResponse.Username);

                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", loginResponse.Token);

                    _cachedToken = loginResponse.Token;
                    return true;
                }
            }

            _logger.LogWarning("Login failed for user: {Username}", username);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _httpClient.PostAsync("/api/auth/logout", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout API call");
        }
        finally
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "username");

            _httpClient.DefaultRequestHeaders.Authorization = null;
            _cachedToken = null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token))
                return false;

            var jwtHandler = new JwtSecurityTokenHandler();
            var jwt = jwtHandler.ReadJwtToken(token);

            return jwt.ValidTo > DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authentication status");
            return false;
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        if (!string.IsNullOrEmpty(_cachedToken))
            return _cachedToken;

        try
        {
            _cachedToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            return _cachedToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving token");
            return null;
        }
    }

    public async Task<string?> GetUsernameAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "username");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving username");
            return null;
        }
    }

    public async Task InitializeAsync()
    {
        var token = await GetTokenAsync();
        if (!string.IsNullOrEmpty(token) && await IsAuthenticatedAsync())
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime Expires { get; set; }
    public string Username { get; set; } = string.Empty;
}