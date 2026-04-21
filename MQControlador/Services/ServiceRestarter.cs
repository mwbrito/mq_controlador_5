using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Management;
using System.ServiceProcess;

namespace MQControlador.Services;

/// <summary>
/// Responsável por reiniciar serviços do Windows
/// </summary>
public class ServiceRestarter
{
    private readonly ILogger<ServiceRestarter> _logger;
    private TimeSpan _stopTimeout;

    public ServiceRestarter(ILogger<ServiceRestarter> logger, IConfiguration configuration)
    {
        var seconds = configuration.GetValue<double?>("StopTimeoutSeconds") ?? 30;
        _stopTimeout = TimeSpan.FromSeconds(seconds);
        _logger = logger;
    }

    /// <summary>
    /// Reinicia um serviço (Stop seguido de Start)
    /// </summary>
    public async Task<RestartResult> RestartServiceAsync(string serviceName)
    {
        var result = new RestartResult { ServiceName = serviceName, StartTime = DateTime.Now };

        try
        {
            // Verificar se o serviço existe
            if (!ServiceExists(serviceName))
            {
                result.Success = false;
                result.Error = $"Serviço '{serviceName}' não encontrado no sistema";
                _logger.LogWarning("Serviço não encontrado: {ServiceName}", serviceName);
                return result;
            }

            // Parar o serviço
            _logger.LogInformation("Parando serviço: {ServiceName}", serviceName);
            var stopSuccess = await StopServiceAsync(serviceName);

            if (!stopSuccess)
            {
                result.Success = false;
                result.Error = "Falha ao parar o serviço";
                return result;
            }

            // Iniciar o serviço
            _logger.LogInformation("Iniciando serviço: {ServiceName}", serviceName);
            await Task.Delay(500); // Pequena pausa entre stop e start

            var startSuccess = StartService(serviceName);

            result.Success = startSuccess;
            if (!startSuccess)
            {
                result.Error = "Falha ao iniciar o serviço";
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao reiniciar serviço: {ServiceName}", serviceName);
            result.Success = false;
            result.Error = $"Exceção: {ex.Message}";
            return result;
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Para um serviço com timeout de 1 minuto
    /// </summary>
    private async Task<bool> StopServiceAsync(string serviceName)
    {
        try
        {
            using var sc = new ServiceController(serviceName);

            if (sc.Status == ServiceControllerStatus.Stopped)
                return true;

            sc.Stop();

            var timeout = _stopTimeout;
            var start = DateTime.Now;

            while (sc.Status != ServiceControllerStatus.Stopped)
            {
                if (DateTime.Now - start > timeout)
                {
                    _logger.LogWarning("Timeout ao parar serviço {ServiceName}", serviceName);
                    break;
                }

                await Task.Delay(1000);
                sc.Refresh();
            }

            if (sc.Status == ServiceControllerStatus.Stopped)
                return true;

            // fallback → kill
            return KillServiceProcess(serviceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao parar serviço {ServiceName}", serviceName);
            return false;
        }
    }

    private bool KillServiceProcess(string serviceName)
    {
        try
        {
            var query = new SelectQuery($"SELECT ProcessId FROM Win32_Service WHERE Name = '{serviceName}'");
            using var searcher = new ManagementObjectSearcher(query);

            foreach (ManagementObject obj in searcher.Get())
            {
                var pid = (uint)obj["ProcessId"];

                if (pid == 0)
                {
                    _logger.LogWarning("Serviço {ServiceName} não possui PID ativo", serviceName);
                    return true; // já está parado
                }

                var process = Process.GetProcessById((int)pid);

                _logger.LogWarning("Matando processo {Pid} do serviço {ServiceName}", pid, serviceName);

                process.Kill(true); // kill árvore inteira
                process.WaitForExit(5000);

                return true;
            }

            _logger.LogWarning("Não foi possível encontrar PID do serviço {ServiceName}", serviceName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao matar processo do serviço {ServiceName}", serviceName);
            return false;
        }
    }

    /// <summary>
    /// Inicia um serviço
    /// </summary>
    private bool StartService(string serviceName)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "net",
                    Arguments = $"start \"{serviceName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(30000); // Timeout de 30 segundos

            bool success = process.ExitCode == 0 || process.ExitCode == 2; // 0 = sucesso, 2 = já iniciado
            if (!success)
            {
                string error = process.StandardError.ReadToEnd();
                _logger.LogWarning("Erro ao iniciar serviço {ServiceName}: {Error}", serviceName, error);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção ao iniciar serviço {ServiceName}", serviceName);
            return false;
        }
    }

    /// <summary>
    /// Verifica se um serviço existe no sistema
    /// </summary>
    private bool ServiceExists(string serviceName)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = $"query \"{serviceName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(5000);

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Resultado da operação de restart de um serviço
/// </summary>
public class RestartResult
{
    public string ServiceName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public TimeSpan Duration => EndTime - StartTime;
}
