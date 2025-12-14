using HomeManagement.Application.Login;
using HomeManagement.Components;
using HomeManagement.Infrastructure;
using LiveStreamingServerNet;
using LiveStreamingServerNet.AdminPanelUI;
using LiveStreamingServerNet.Flv.Installer;
using LiveStreamingServerNet.Standalone;
using LiveStreamingServerNet.Standalone.Installer;
using LiveStreamingServerNet.StreamProcessor.AspNetCore.Installer;
using LiveStreamingServerNet.StreamProcessor.Installer;
using MailerSendNetCore.Common.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using Scalar.AspNetCore;
using System.Net;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using HomeManagement.Application.WebHooks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<HomeManagementDbContext>(options => options.UseSqlite("Data Source=home_management.db"));
builder.Services.AddMudServices();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<StaticAuthOptions>(builder.Configuration.GetSection("Auth"));
builder.Services.Configure<PowerIsBackTelegramSettings>(builder.Configuration.GetSection("PowerIsBackTelegramSettings"));
builder.Services.Configure<PowerIsBackEmailSettings>(builder.Configuration.GetSection("PowerIsBackEmailSettings"));
builder.Services.AddMailerSendEmailClient(builder.Configuration.GetSection("PowerIsBackEmailSettings"));

// Cookie authentication only (no Identity)
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.LogoutPath = "/Account/Logout";
        o.AccessDeniedPath = "/Account/Login";
        o.SlidingExpiration = true;
        o.ExpireTimeSpan = TimeSpan.FromHours(12);
    });

builder.Services.AddAuthorization();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddLiveStreamingServer(
    new IPEndPoint(IPAddress.Any, 1935),
    options => options
    .AddAuthorizationHandler<StreamAuthorizationHandler>()
    .AddStandaloneServices()
    .AddFlv()
    .AddStreamProcessor()
    .AddHlsTransmuxer()
);
builder.Services.AddHttpContextAccessor();
builder.Services.AddKeyedScoped<IHandler, PowerIsBackTelegramHandler>(WebHookActions.PowerOn);
builder.Services.AddKeyedScoped<IHandler, PowerIsBackEmailHandler>(WebHookActions.PowerOn);
builder.Services.AddSingleton(_ => Channel.CreateBounded<WebHookActions>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait
}));

builder.Services.AddHostedService<WebHookMessageProcessor>();

builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HomeManagementDbContext>>();
    await using var dbContext = await dbContextFactory.CreateDbContextAsync();
    await dbContext.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.MapOpenApi();

app.MapScalarApiReference();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.UseWhen(c => c.Request.Path.StartsWithSegments("/ip-cameras"), branch =>
{
    branch.Use(async (ctx, next) =>
    {
        if (!(ctx.User.Identity?.IsAuthenticated ?? false))
        {
            var returnUrl = Uri.EscapeDataString(ctx.Request.Path + ctx.Request.QueryString);
            ctx.Response.Redirect($"/Account/Login?returnUrl={returnUrl}");
            return;
        }
        await next();
    });
});

app.UseHttpFlv();
app.UseHlsFiles();
app.MapStandaloneServerApiEndPoints();
app.UseAdminPanelUI(new AdminPanelUIOptions
{
    BasePath = "/ip-cameras",
    HasHttpFlvPreview = true,
    HasHlsPreview = true,
    HttpFlvUriPattern = "{streamPath}.flv",
    HlsUriPattern = "{streamPath}/output.m3u8"
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

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

    return Results.Json(new { error = "Unauthorized" }, statusCode: 401);
});

app.MapPost("api/account/login", async (
    HttpContext context,
    IOptions<StaticAuthOptions> authOptions,
    [FromForm] string username,
    [FromForm] string key,
    [FromForm] bool remember,
    [FromForm] string? returnUrl) =>
{
    var cfg = authOptions.Value;
    if (key == cfg.Key)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var props = new AuthenticationProperties
        {
            IsPersistent = remember,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

        var target = NormalizeReturnUrl(returnUrl);
        return Results.Redirect(target);
    }

    var redirect = "/Account/Login?error=1" + (string.IsNullOrWhiteSpace(returnUrl) ? "" : $"&returnUrl={Uri.EscapeDataString(returnUrl)}");
    return Results.Redirect(redirect);

    static string NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        if (Uri.TryCreate(returnUrl, UriKind.Relative, out _))
        {
            return returnUrl;
        }

        return "/";
    }
});

app.MapPost("api/account/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync();
    return Results.Redirect("/");
});

await app.RunAsync();