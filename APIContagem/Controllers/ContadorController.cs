using APIContagem.Logging;
using APIContagem.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Diagnostics;

namespace APIContagem.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ContadorController : ControllerBase
{
    private static readonly Contador _CONTADOR = new Contador();

    private readonly ILogger<ContadorController> _logger;
    private readonly TelemetryConfiguration _telemetryConfig;
    private readonly IDatabase _redisDb;
    private readonly IConfiguration _configuration;
    private readonly ContagemRepository _repository;

    public ContadorController(
        ILogger<ContadorController> logger,
        TelemetryConfiguration telemetryConfig,
        IDatabase redisDb,
        IConfiguration configuration,
        ContagemRepository repository)
    {
        _logger = logger;
        _telemetryConfig = telemetryConfig;
        _redisDb = redisDb;
        _configuration = configuration;
        _repository = repository;
    }

    [HttpGet]
    [Route("contador-redir")]
    public async Task<ActionResult<ResultadoContador>> GetContadorRedir()
    {
        DateTimeOffset inicio = DateTime.Now;
        var watch = new Stopwatch();
        watch.Start();

        var valorAtualContador = await RetornaValorContadorCache();

        watch.Stop();

        var telemetryClient = new TelemetryClient(_telemetryConfig);

        try
        {
            //if (valorAtualContador % 4 == 0)
            //    throw new Exception("Simulacao de Falha");

            //Adicionando custom event do application insights
            _logger.LogInformation("Gerando Custom Event do Application Insights...");
            telemetryClient.TrackEvent("ContagemAcessos", GetDictionaryValorAtual(valorAtualContador));
            _logger.LogValorAtual(valorAtualContador);

            //Adicionando track de dependência do redis no application insights
            telemetryClient.TrackDependency(
                "Redis", "INCR", $"{nameof(valorAtualContador)} = {valorAtualContador}",
                inicio, watch.Elapsed, true);

            return Ok(new ResultadoContador
            {
                ValorAtual = valorAtualContador,
                Producer = ContagemInfo.Local,
                Kernel = ContagemInfo.Kernel,
                Framework = ContagemInfo.Framework,
                Enviroment = ContagemInfo.Env,
                Mensagem = ContagemInfo.Mensagem
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Excecao - Mensagem: {ex.Message}");
            _logger.LogWarning("Registrando Exception com o Application Insights...");
            telemetryClient.TrackException(ex, GetDictionaryValorAtual(valorAtualContador));
            return BadRequest();
        }
    }


    [HttpGet]
    [Route("contador-mongo")]
    public ResultadoContador GetContadorMongo()
    {
        _logger.LogInformation("Gerando valor...");

        int valorAtualContador;
        lock (_CONTADOR)
        {
            _CONTADOR.Incrementar();
            valorAtualContador = _CONTADOR.ValorAtual;
        }

        var resultado = new ResultadoContador()
        {
            ValorAtual = valorAtualContador,
            Producer = _CONTADOR.Local,
            Kernel = _CONTADOR.Kernel,
            Framework = _CONTADOR.Framework,
            Mensagem = _configuration["MensagemVariavel"]
        };

        _logger.LogInformation("Persistindo documento...");
        _repository.Save(resultado);

        return resultado;
    }

    private async Task<int> RetornaValorContadorCache()
    {
        var valorContador = await _redisDb
            .StringIncrementAsync("APIContagem");
        return (int)valorContador;
    }

    private Dictionary<string, string> GetDictionaryValorAtual(int valorAtualContador)
    {
        return new Dictionary<string, string>
        {
             { "Horario", DateTime.Now.ToString("HH:mm:ss") },
             { "ValorAtual", valorAtualContador.ToString() }
        };
    }
}
