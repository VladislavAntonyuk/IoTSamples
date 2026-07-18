using HomeManagement.Application.Login;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace HomeManagement.Infrastructure;

public sealed class McpApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<StaticAuthOptions> authOptions) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(McpApiKeyAuthenticationDefaults.HeaderName, out var providedKeyValues))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Missing {McpApiKeyAuthenticationDefaults.HeaderName} header."));
        }

        var expectedKey = authOptions.Value.Key;
        var providedKey = providedKeyValues.ToString();
        if (string.IsNullOrWhiteSpace(expectedKey) || string.IsNullOrWhiteSpace(providedKey) || !KeysMatch(expectedKey, providedKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "mcp-client"),
            new Claim(ClaimTypes.Name, "mcp-client")
        };

        var identity = new ClaimsIdentity(claims, McpApiKeyAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, McpApiKeyAuthenticationDefaults.AuthenticationScheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool KeysMatch(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
