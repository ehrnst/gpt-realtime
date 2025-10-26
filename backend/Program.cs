using GptRealtime.Api.Models;
using GptRealtime.Api.Services;
using OpenAI;
using System.ClientModel;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.Configure<OpenAISettings>(builder.Configuration.GetSection("OpenAI"));

// Register OpenAI client for Azure OpenAI
builder.Services.AddSingleton<OpenAIClient>(serviceProvider =>
{
    var openAISettings = serviceProvider.GetRequiredService<IOptions<OpenAISettings>>().Value;
    
    if (string.IsNullOrWhiteSpace(openAISettings.ApiKey))
    {
        throw new InvalidOperationException("OpenAI API key is required.");
    }
    
    var clientOptions = new OpenAIClientOptions();
    if (!string.IsNullOrWhiteSpace(openAISettings.BaseUrl))
    {
        // For Azure OpenAI, use the base URL as-is, not with /openai/v1/ suffix
        clientOptions.Endpoint = new Uri(openAISettings.BaseUrl.TrimEnd('/'));
    }
    
    return new OpenAIClient(new ApiKeyCredential(openAISettings.ApiKey), clientOptions);
});

// Register TokenService with HttpClient dependency
// HttpClient is needed for session token creation (realtime preview feature)
builder.Services.AddHttpClient<TokenService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "GptRealtime.Api/1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<ITokenService, TokenService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:4200",
            "https://localhost:4200"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // Only use HTTPS redirection in production
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();
