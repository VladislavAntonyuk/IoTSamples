using HomeManagement.Application.IpCameras;
using HomeManagement.Application.Login;
using HomeManagement.Application.Router;
using HomeManagement.Application.WebHooks;
using HomeManagement.Application.WebHooks.Email;
using HomeManagement.Application.WebHooks.Telegram;
using HomeManagement.Components;
using HomeManagement.Infrastructure;
using LiveStreamingServerNet;
using LiveStreamingServerNet.AdminPanelUI;
using LiveStreamingServerNet.Flv.Installer;
using LiveStreamingServerNet.Standalone;
using LiveStreamingServerNet.Standalone.Installer;
using LiveStreamingServerNet.StreamProcessor.AspNetCore.Installer;
using LiveStreamingServerNet.StreamProcessor.Installer;
using LiveStreamingServerNet.StreamProcessor.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<HomeManagementDbContext>(options => options.UseSqlite("Data Source=home_management.db"));
builder.Services.AddMudServices();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<StaticAuthOptions>(builder.Configuration.GetSection("Auth"));
builder.Services.Configure<TelegramSettings>(builder.Configuration.GetSection("TelegramSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Cookie authentication for Blazor UI + API key authentication for MCP
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.LogoutPath = "/Account/Logout";
        o.AccessDeniedPath = "/Account/Login";
        o.SlidingExpiration = true;
        o.ExpireTimeSpan = TimeSpan.FromHours(12);
    })
    .AddScheme<AuthenticationSchemeOptions, McpApiKeyAuthenticationHandler>(
        McpApiKeyAuthenticationDefaults.AuthenticationScheme,
        _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(McpApiKeyAuthenticationDefaults.PolicyName, policy =>
    {
        policy.AuthenticationSchemes.Add(McpApiKeyAuthenticationDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });
});

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
            configuration.Condition = new ConfigurationStreamProcessorCondition();
            configuration.Name = "live-camera-archive";
            configuration.FFmpegPath = ExecutableFinder.FindExecutableFromPATH("ffmpeg")!;
            configuration.FFmpegArguments =
                "-i {inputPath} " +
                "-c:v copy " +
                "-c:a copy " +
                "-f mpegts {outputPath}";
            var outputDir = builder.Configuration.GetValue<string>("StreamProcessor:StoragePath") ?? Path.Combine(Directory.GetCurrentDirectory(), "live-camera-archive");
            configuration.OutputPathResolver = new LiveCameraOutputPathResolver(outputDir);
        })
    )
);
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ISender, TelegramSender>();
builder.Services.AddScoped<ISender, EmailSender>();
builder.Services.AddSingleton<SenderRequestFactory>();
builder.Services.AddSingleton(_ => Channel.CreateBounded<WebHookModel>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait
}));
builder.Services.AddScoped<IRouterController, AsusRouterController>((sp) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("AsusRouter");
    httpClient.BaseAddress = new Uri(configuration.GetValue<string>("Router:IpAddress"));
    var username = configuration.GetValue<string>("Router:Username");
    var password = configuration.GetValue<string>("Router:Password");
    return new AsusRouterController(httpClient, username, password);
});

builder.Services.AddHostedService<WebHookMessageProcessor>();

builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .AddAuthorizationFilters()
    .WithTools<HomeManagementMcpTools>();
    
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

app.MapMcp("/mcp").RequireAuthorization(McpApiKeyAuthenticationDefaults.PolicyName);
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();