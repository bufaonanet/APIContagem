using APIContagem.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Redis Configuration
var redisConnectionString = builder.Configuration.GetConnectionString("cachedb");
using var redis = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddSingleton(redis.GetDatabase());

// ApplicationInsights
var applicationInsights = builder.Configuration.GetConnectionString("ApplicationInsights");
if (!string.IsNullOrWhiteSpace(applicationInsights))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = applicationInsights;
    });
}

builder.Services.AddScoped<ContagemRepository>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

// Ativando o middlweare de Health Check
app.UseHealthChecks("/status");

app.Run();