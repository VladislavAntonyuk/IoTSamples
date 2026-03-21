using LiveStreamingServerNet.Networking.Contracts;
using LiveStreamingServerNet.Rtmp.Server.Auth;
using LiveStreamingServerNet.Rtmp.Server.Auth.Contracts;
using Microsoft.Extensions.Options;

namespace HomeManagement.Application.Login;

public class StreamAuthorizationHandler(IHttpContextAccessor httpContextAccessor, IOptions<StaticAuthOptions> authOptions) : IAuthorizationHandler
{
    public Task<AuthorizationResult> AuthorizePublishingAsync(
        ISessionInfo client,
        string streamPath,
        IReadOnlyDictionary<string, string> streamArguments,
        string publishingType)
    {
        // Accepting only the publishing path that includes a valid password parameter
        // For example: rtmp://127.0.0.1:1935/live/stream?password=123456
        if (streamArguments.TryGetValue("password", out var password) && authOptions.Value.Key == password)
        {
            return Task.FromResult(AuthorizationResult.Authorized());
        }

        return Task.FromResult(AuthorizationResult.Unauthorized("incorrect password"));
    }

    public Task<AuthorizationResult> AuthorizeSubscribingAsync(
        ISessionInfo client,
        string streamPath,
        IReadOnlyDictionary<string, string> streamArguments)
    {
        if (httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true)
        {
            return Task.FromResult(AuthorizationResult.Authorized());
        }

        return Task.FromResult(AuthorizationResult.Unauthorized("incorrect password"));
    }
}