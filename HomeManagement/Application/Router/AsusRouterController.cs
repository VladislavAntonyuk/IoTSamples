using System.Text;
using System.Text.Json;

namespace HomeManagement.Application.Router;

public class AsusRouterController : IRouterController
{
    public class LoginResult
    {
        public string? AsusToken { get; set; }
    }

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions(JsonSerializerOptions.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _httpClient;
    private readonly string _username;
    private readonly string _password;

    public AsusRouterController(HttpClient httpClient, string username, string password)
    {
        _httpClient = httpClient;
        _username = username;
        _password = password;
        httpClient.DefaultRequestHeaders.Add("User-Agent", "asusrouter-Android-DUTUtil-1.0.0.3.58-163");
    }

    private async Task<string?> AuthenticateAsync()
    {
        const string path = "/login.cgi";
        var formattedUsernamePassword = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}"));

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "login_authorization", formattedUsernamePassword }
        });

        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = content
        };

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var loginResult = await response.Content.ReadFromJsonAsync<LoginResult>(Options);
        return loginResult?.AsusToken;
    }

    private async Task<RouterResponse> ApplyAppPostAsync(Dictionary<string, string> payload)
    {
        var asusToken = await AuthenticateAsync();
        if (string.IsNullOrEmpty(asusToken))
        {
            return new RouterResponse { ErrorStatus = "Authentication failed" };
        }

        const string path = "/apply.cgi";
        var content = new FormUrlEncodedContent(payload);

        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = content
        };
        request.Headers.Add("Cookie", $"asus_token={asusToken}");

        var response = await _httpClient.SendAsync(request);
        var r = response.IsSuccessStatusCode;
        var c = await response.Content.ReadAsStringAsync();
        return new RouterResponse { ErrorStatus = r ? null : c };
    }

    public Task<RouterResponse> SetLeds(bool enabled)
    {
        var configJson = JsonSerializer.Serialize(new { led_val = enabled ? 1 : 0 });
        return ApplyAppPostAsync(new Dictionary<string, string>
        {
            { "config", configJson },
            { "action_mode", "config_changed" }
        });
    }

    public Task<RouterResponse> RebootModem()
    {
        return ApplyAppPostAsync(new Dictionary<string, string>
        {
            { "action_mode", "rebootmodem" }
        });
    }

    public Task<RouterResponse> Reboot()
    {
        return ApplyAppPostAsync(new Dictionary<string, string>
        {
            { "action_mode", "reboot" }
        });
    }
}