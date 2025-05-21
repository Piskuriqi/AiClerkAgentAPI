using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using AiClerkAgentAPI.Services;
using AiClerkAgentAPI.Plugins;
using AiClerkAgentAPI.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// ChatSettings laden
builder.Services.Configure<ChatSettings>(builder.Configuration.GetSection("ChatSettings"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<ChatSettings>>().Value);

// CORS aktivieren
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// OpenAI API-Key (Hinweis: In Produktion absichern!)
const string openAiApiKey = "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";

// Services registrieren
builder.Services.AddHttpClient<ProductService>();

builder.Services.AddSingleton<CartService>(sp =>
{
    var cache = sp.GetRequiredService<IMemoryCache>();
    var productService = sp.GetRequiredService<ProductService>();
    return new CartService(cache, productService);
});

builder.Services.AddSingleton<ProductService>();
builder.Services.AddSingleton<ShopPlugin>();

// Semantic Kernel konfigurieren
builder.Services.AddSingleton(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder()
        .AddOpenAIChatCompletion("gpt-4o", openAiApiKey);

    var shopPlugin = sp.GetRequiredService<ShopPlugin>();
    kernelBuilder.Plugins.AddFromObject(shopPlugin, "Shop");

    return kernelBuilder.Build();
});

// Chat Completion Service
builder.Services.AddScoped<IChatCompletionService>(sp =>
    sp.GetRequiredService<Kernel>().GetRequiredService<IChatCompletionService>()
);

builder.Services.AddMemoryCache();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger aktivieren
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowAll");
app.MapControllers();

app.Run();
