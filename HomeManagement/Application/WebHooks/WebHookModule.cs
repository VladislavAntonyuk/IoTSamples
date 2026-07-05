using HomeManagement.Application.Login;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace HomeManagement.Application.WebHooks;

public static class WebHookModule
{
    public static void MapWebHook(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/webhook", async (
            IOptions<StaticAuthOptions> authOptions,
            [FromHeader] string key,
            [FromBody] WebHookModel model,
            Channel<WebHookModel> channel,
            CancellationToken token) =>
        {
            var cfg = authOptions.Value;
            if (key == cfg.Key)
            {
                await channel.Writer.WriteAsync(model, token);
                return Results.Ok();
            }

            return Results.Unauthorized();
        }).ShortCircuit();
    }
}