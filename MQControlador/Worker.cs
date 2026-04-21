using MQControlador.Services;
using Microsoft.Extensions.Configuration;

namespace MQControlador;

public class Worker(
    ILogger<Worker> logger,
    IConfiguration configuration,
    ServiceRestarter serviceRestarter,
    ServiceManager serviceManager,
    SchedulerHelper schedulerHelper) : BackgroundService
{
    private TimeSpan? _scheduledTime;
    private DateTime _lastExecutionDate = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MQControlador iniciado");

        // Fazer parse do horário agendado
        var scheduledTimeString = configuration["ScheduledTime"] ?? "14:30";
        _scheduledTime = schedulerHelper.ParseScheduledTime(scheduledTimeString);

        if (_scheduledTime == null)
        {
            logger.LogError("Não foi possível configurar o horário agendado. Serviço será parado.");
            return;
        }

        logger.LogInformation("Agendamento configurado para: {ScheduledTime}", scheduledTimeString);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_scheduledTime.HasValue && schedulerHelper.IsTimeToExecute(_scheduledTime.Value))
                {
                    // Evitar múltiplas execuções no mesmo dia
                    if (_lastExecutionDate.Date != DateTime.Now.Date)
                    {
                        _lastExecutionDate = DateTime.Now;
                        await ExecuteServiceRestartAsync(stoppingToken);
                    }
                }

                // Aguardar 10 segundos antes de verificar novamente
                await Task.Delay(10000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Operação cancelada");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no worker");
                await Task.Delay(10000, stoppingToken);
            }
        }

        logger.LogInformation("MQControlador encerrado");
    }

    /// <summary>
    /// Executa o restart de todos os serviços
    /// </summary>
    private async Task ExecuteServiceRestartAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Iniciando reinicialização de serviços em {Time}", DateTime.Now);

        var servicesFilePath = configuration["ServicesFilePath"] ?? "services.txt";
        var services = serviceManager.ReadServicesFromFile(servicesFilePath);

        if (services.Count == 0)
        {
            logger.LogError("Nenhum serviço configurado para reiniciar");
            return;
        }

        logger.LogInformation("Total de serviços para reiniciar: {Count}", services.Count);

        var results = new List<string>();
        var successCount = 0;
        var failureCount = 0;

        foreach (var serviceName in services)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Cancelamento solicitado, parando restart dos serviços");
                break;
            }

            try
            {
                logger.LogInformation("Reiniciando serviço: {ServiceName}", serviceName);
                var result = await serviceRestarter.RestartServiceAsync(serviceName);

                if (result.Success)
                {
                    successCount++;
                    results.Add($"✓ {serviceName} - Sucesso ({result.Duration.TotalSeconds:F2}s)");
                    logger.LogInformation("Serviço reiniciado com sucesso: {ServiceName} ({Duration}ms)", 
                        serviceName, result.Duration.TotalMilliseconds);
                }
                else
                {
                    failureCount++;
                    results.Add($"✗ {serviceName} - Erro: {result.Error}");
                    logger.LogError("Falha ao reiniciar serviço {ServiceName}: {Error}", 
                        serviceName, result.Error);
                }

                // Pequena pausa entre serviços
                await Task.Delay(1000, stoppingToken);
            }
            catch (Exception ex)
            {
                failureCount++;
                results.Add($"✗ {serviceName} - Exceção: {ex.Message}");
                logger.LogError(ex, "Exceção ao reiniciar serviço {ServiceName}", serviceName);
            }
        }

        // Log resumido
        logger.LogInformation(
            "Reinicialização concluída - Sucesso: {SuccessCount}, Falhas: {FailureCount}, Total: {TotalCount}",
            successCount, failureCount, services.Count);

        foreach (var result in results)
        {
            logger.LogInformation("{Result}", result);
        }

        logger.LogInformation("Próxima execução agendada para amanhã às {ScheduledTime}",
            configuration["ScheduledTime"] ?? "14:30");
    }
}

