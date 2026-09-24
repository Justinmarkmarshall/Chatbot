using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using AIPlatform.Api.Options;
using AIPlatform.Api.Services;
using Chatbot.Components;
using Scalar.AspNetCore;
using Marshall.Authentication.Google;
using Chatbot.Services;
using Chatbot.Persistence;
using Npgsql;

if (args.Contains("--document-worker"))
{
    var worker = Host.CreateApplicationBuilder(args.Where(a => a != "--document-worker").ToArray());
    if (worker.Environment.IsDevelopment()) worker.Configuration.AddUserSecrets<Chatbot.Processing.DocumentWorker>();
    worker.Services.AddSingleton(_ => NpgsqlDataSource.Create(DatabaseConfiguration.ConnectionString(worker.Configuration)));
    worker.Services.AddSingleton(_ => Chatbot.Processing.DocumentWorker.LoadModel(worker.Configuration["Documents:ModelRoot"]
        ?? throw new InvalidOperationException("Configure Documents:ModelRoot with the offline model directory containing minilm/.")));
    worker.Services.AddSingleton<Chatbot.Processing.DocumentProcessor>();
    worker.Services.AddHostedService<Chatbot.Processing.DocumentWorker>();
    using var host = worker.Build();
    await ChatDatabase.InitializeAsync(host.Services.GetRequiredService<NpgsqlDataSource>());
    await host.RunAsync();
    return;
}
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:KeysPath"] ?? "/keys"))
    .SetApplicationName("Chatbot");
// Includes the MVC antiforgery authorization filter used by cookie-authenticated API writes.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddGoogleSessionAuthentication<ChatbotGoogleSessionHandler>(google =>
{
    google.ClientId = builder.Configuration["Authentication:Google:ClientId"]
        ?? throw new InvalidOperationException("Configure Authentication:Google:ClientId.");
    google.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
        ?? throw new InvalidOperationException("Configure Authentication:Google:ClientSecret.");
    google.Events.OnRemoteFailure = context =>
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Chatbot.Authentication");
        logger.LogWarning(context.Failure,
            "Google sign-in failed. Request scheme={Scheme}, host={Host}, callback={CallbackPath}",
            context.Request.Scheme, context.Request.Host.Value, context.Options.CallbackPath.Value);
        context.HandleResponse();
        context.Response.Redirect("/login?error=auth_failed");
        return Task.CompletedTask;
    };
}, options =>
{
    options.ApplicationCookieName = "Chatbot.Auth";
    options.ExternalCookieName = "Chatbot.External";
});
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(DatabaseConfiguration.ConnectionString(builder.Configuration)));
builder.Services.AddScoped<ChatStore>();
builder.Services.AddScoped<IChatStore>(sp => sp.GetRequiredService<ChatStore>());
builder.Services.AddSingleton<IQueryEmbedding, QueryEmbedding>();
builder.Services.AddScoped<DocumentRetrieval>();
builder.Services.AddScoped<IDocumentRetrieval>(sp => sp.GetRequiredService<DocumentRetrieval>());
builder.Services.AddScoped<ChatService>();
builder.Services.AddSingleton<DocumentUploads>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgres");
builder.Services.AddOpenApi();

builder.Services
    .AddOptions<OllamaOptions>()
    .Bind(builder.Configuration.GetSection(OllamaOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<IOllamaClient, OllamaClient>(
    (serviceProvider, client) =>
    {
        var options = serviceProvider
            .GetRequiredService<
                Microsoft.Extensions.Options.IOptions<OllamaOptions>>()
            .Value;

        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = TimeSpan.FromMinutes(5);
    });

var app = builder.Build();

await ChatDatabase.InitializeAsync(app.Services.GetRequiredService<NpgsqlDataSource>());

app.MapChatbotHealthEndpoints();
app.MapOpenApi().RequireAuthorization();
app.MapScalarApiReference().RequireAuthorization();

app.UseForwardedHeaders();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGoogleSessionEndpoints();
app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .Add(endpoint =>
    {
        var dispatcher = endpoint.Metadata
            .OfType<Microsoft.AspNetCore.Http.Connections.HttpConnectionDispatcherOptions>()
            .FirstOrDefault();
        if (dispatcher is not null) dispatcher.CloseOnAuthenticationExpiration = true;
    });

app.Run();
