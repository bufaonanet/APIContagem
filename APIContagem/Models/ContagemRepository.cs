using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using MongoDB.Driver;
using System.Diagnostics;
using System.Text.Json;

namespace APIContagem.Models;

public class ContagemRepository
{
    private readonly ILogger<ContagemRepository> _logger;
    private readonly IConfiguration _configuration;
    private readonly TelemetryConfiguration _telemetryConfig;

    public ContagemRepository(
        ILogger<ContagemRepository> logger,
        IConfiguration configuration,
        TelemetryConfiguration telemetryConfig)
    {
        _logger = logger;
        _configuration = configuration;
        _telemetryConfig = telemetryConfig;
    }

    public void Save(ResultadoContador resultado)
    {
        try
        {
            DateTimeOffset startTime = DateTime.Now;
            var watch = new Stopwatch();
            watch.Start();

            var collection = new MongoClient(_configuration.GetConnectionString("MongoDB"))
                .GetDatabase(_configuration["MongoDB:Database"])
                .GetCollection<ContagemDocument>(_configuration["MongoDB:Collection"]);
          
            collection.InsertOne(new()
            {
                ValorAtual = resultado.ValorAtual,
                Producer = resultado.Producer,
                Kernel = resultado.Kernel,
                Framework = resultado.Framework,
                Mensagem = resultado.Mensagem
            });

            watch.Stop();

            var resultadoJson = JsonSerializer.Serialize(resultado);

            var client = new TelemetryClient(_telemetryConfig);
            client.TrackDependency(
                "MongoDB", $"{nameof(ContagemDocument)} InsertOne",
                resultadoJson, startTime, watch.Elapsed, true);

            _logger.LogInformation(
                "MongoDB - Gerado documento para o valor {ValorAtual} | " +
                "{resultadoJson}", resultado.ValorAtual, resultadoJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na gravação do documento.");
            throw;
        }
    }
}
