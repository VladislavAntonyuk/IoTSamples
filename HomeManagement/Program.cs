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
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Scalar.AspNetCore;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using HomeManagement.Application.WebHooks;
using LiveStreamingServerNet.StreamProcessor.Utilities;
using HomeManagement.Application.IpCameras;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<HomeManagementDbContext>(options => options.UseSqlite("Data Source=home_management.db"));
builder.Services.AddMudServices();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<StaticAuthOptions>(builder.Configuration.GetSection("Auth"));
builder.Services.Configure<PowerIsBackTelegramSettings>(builder.Configuration.GetSection("PowerIsBackTelegramSettings"));
builder.Services.Configure<PowerIsBackEmailSettings>(builder.Configuration.GetSection("PowerIsBackEmailSettings"));

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
    .AddHlsTransmuxer(hlsTransmuxerConfigurator => hlsTransmuxerConfigurator.Configure(config =>
        config.Condition = new HlsTransmuxingCondition()
    ))
    .AddFFmpeg(configure =>
        configure.ConfigureDefault(configuration =>
        {
            configuration.Name = "live-camera-archive";
            configuration.FFmpegPath = ExecutableFinder.FindExecutableFromPATH("ffmpeg")!;
            configuration.FFmpegArguments =
                "-i {inputPath} " +
                "-c:v libx264 -preset ultrafast -tune zerolatency " +
                "-b:v 290k -maxrate 290k -bufsize 580k " +
                "-r 10 -g 20 -keyint_min 20 -sc_threshold 0 " +
                "-force_key_frames \"expr:gte(t,n_forced*2)\" " +
                "-pix_fmt yuv420p " +
                "-c:a aac -b:a 32k " +
                "-f mpegts {outputPath}";
            configuration.OutputPathResolver = new LiveCameraOutputPathResolver(Path.Combine(Directory.GetCurrentDirectory(), "live-camera-archive"));
        })
    )
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

app.MapLogin();
app.MapWebHook();
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

await app.RunAsync();