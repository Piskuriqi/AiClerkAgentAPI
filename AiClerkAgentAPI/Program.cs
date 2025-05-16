using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using AiClerkAgentAPI.Services;
using AiClerkAgentAPI.Plugins;

var builder = WebApplication.CreateBuilder(args);

// Hier wird der OpenAI-API-Schlüssel direkt im Code definiert.
// Aus Sicherheitsgründen sollte man ihn später in einen Secret-Manager oder Umgebungsvariablen auslagern.
const string openAiApiKey = "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";

// Registrierung des ProductService mithilfe der HttpClient-Fabrik.
builder.Services.AddHttpClient<ProductService>();
builder.Services.AddHttpClient<CartService>();
builder.Services.AddSingleton<ShopPlugin>();


// Konfiguration des Semantic Kernel und Registrierung des ChatCompletionService.
builder.Services.AddSingleton(sp =>
{
    // 1) Kernel-Builder erzeugen
    var kernelBuilder = Kernel.CreateBuilder()
        .AddOpenAIChatCompletion("gpt-4o", openAiApiKey);

    // 2) Plugin aus dem Container holen (jetzt registriert!)  
    var shopPlugin = sp.GetRequiredService<ShopPlugin>();
    kernelBuilder.Plugins.AddFromObject(shopPlugin, "Shop");

    // 3) Kernel fertigbauen
    return kernelBuilder.Build();
});

// Der ChatCompletionService wird aus dem Kernel extrahiert und als Scoped-Service registriert.
builder.Services.AddScoped<IChatCompletionService>(sp =>
    sp.GetRequiredService<Kernel>().GetRequiredService<IChatCompletionService>()
);

// Aktivierung des MemoryCache für die Speicherung der Conversation History.
builder.Services.AddMemoryCache();

// Registrierung der Controller für die HTTP-Endpunkte.
builder.Services.AddControllers();

// Aktivierung von Swagger zur Dokumentation der API.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.MapControllers();

app.Run();
