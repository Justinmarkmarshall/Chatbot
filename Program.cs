using AIPlatform.Api.Options;
using AIPlatform.Api.Services;
using Chatbot.Components;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddScoped(serviceProvider => new HttpClient
{
    BaseAddress = new Uri(
        serviceProvider.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().BaseUri)
});
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();