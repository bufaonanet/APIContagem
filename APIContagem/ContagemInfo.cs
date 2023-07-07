using System.Runtime.InteropServices;

namespace APIContagem;

public class ContagemInfo
{
    public static string Local { get; }
    public static string Kernel { get; }
    public static string Framework { get; }
    public static string Mensagem { get; }
    public static string Env { get; set; }

    static ContagemInfo()
    {
        Local = "APIContagemRedis";
        Kernel = Environment.OSVersion.VersionString;
        Framework = RuntimeInformation.FrameworkDescription;
        Env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Sem informação";
        Mensagem = "Testes com Redis + Application Insights";
    }
}