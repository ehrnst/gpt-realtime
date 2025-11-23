// REFERENCE IMPLEMENTATION - This file shows the key additions needed in Program.cs
// Add these sections to your actual Program.cs

using backend.Services;
using backend.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure OpenAI settings (EXISTING)
builder.Services.Configure<OpenAISettings>(
    builder.Configuration.GetSection("OpenAI"));

// ADDED: Configure Persona settings
builder.Services.Configure<PersonaSettings>(
    builder.Configuration.GetSection("Personas"));

// Register services
builder.Services.AddHttpClient();
builder.Services.AddScoped<SpeachService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
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

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
