using HomeManagement.Application.Login;
using HomeManagement.Application.Router;
using HomeManagement.Application.WebHooks;
using HomeManagement.Application.Workflows;
using HomeManagement.Application.WebHooks.Email;
using HomeManagement.Application.WebHooks.Telegram;
using HomeManagement.Components;
using HomeManagement.Infrastructure;
using LiveStreamingServerNet;
using LiveStreamingServerNet.Flv.Installer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.AspNetCore.Components;
using HomeManagement.Infrastructure.IpCameras;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<HomeManagementDbContext>(options => options.UseSqlite("Data Source=home_management.db"));
builder.Services.AddMudServices();
builder.Services.AddHybridCache();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<StaticAuthOptions>(builder.Configuration.GetSection("Auth"));
builder.Services.Configure<TelegramSettings>(builder.Configuration.GetSection("TelegramSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<InternetWatchdogOptions>(builder.Configuration.GetSection("InternetWatchdog"));
builder.Services.Configure<WorkflowAutomationOptions>(builder.Configuration.GetSection("WorkflowAutomation"));

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
    .AddFlv()
);
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<HomeManagement.Application.DeviceManagement.IDeviceActionExecutor, HomeManagement.Application.DeviceManagement.DeviceActionExecutor>();
builder.Services.AddSingleton<IWorkflowRunner, WorkflowRunner>();
builder.Services.AddSingleton<IWorkflowTriggerPreviewService, WorkflowTriggerPreviewService>();
builder.Services.AddScoped<ISender, TelegramSender>();
builder.Services.AddScoped<ISender, EmailSender>();
builder.Services.AddSingleton<SenderRequestFactory>();
builder.Services.AddSingleton(_ => Channel.CreateBounded<WebHookModel>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait
}));
builder.Services.AddSingleton<IRouterController, AsusRouterController>((sp) =>
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
builder.Services.AddHostedService<InternetWatchdogService>();
builder.Services.AddHostedService<WorkflowTriggerService>();

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

app.UseHttpFlv();

app.MapMcp("/mcp").RequireAuthorization(McpApiKeyAuthenticationDefaults.PolicyName);
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .WithBrowserOptions(options =>
    {
        options.AddAutoPause();
    });

await app.RunAsync();