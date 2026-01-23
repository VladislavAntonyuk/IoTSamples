using System.Threading.Channels;
using HomeManagement.Application.Login;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HomeManagement.Application.WebHooks;

public static class WebHookModule
{
    public static void MapWebHook(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/webhook", async (
            IOptions<StaticAuthOptions> authOptions,
            [FromHeader] string key,
            [FromBody] WebHookModel model,
            Channel<WebHookActions> channel,
            CancellationToken token) =>
        {
            var cfg = authOptions.Value;
            if (key == cfg.Key)
            {
                await channel.Writer.WriteAsync(model.Action, token);
                return Results.Ok();
            }

            return Results.Unauthorized();
        }).ShortCircuit();
    }
}