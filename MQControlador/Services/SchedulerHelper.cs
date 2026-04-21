using Microsoft.Extensions.Logging;

namespace MQControlador.Services;

/// <summary>
/// Auxiliar para gerenciar o agendamento das execuções
/// </summary>
public class SchedulerHelper
{
    private readonly ILogger<SchedulerHelper> _logger;

    public SchedulerHelper(ILogger<SchedulerHelper> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Pareia um horário no formato "HH:mm"
    /// </summary>
    public TimeSpan? ParseScheduledTime(string timeString)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(timeString))
            {
                _logger.LogError("Horário agendado não configurado");
                return null;
            }

            if (TimeSpan.TryParse(timeString, out var timeSpan))
            {
                _logger.LogInformation("Horário agendado configurado: {Time}", timeString);
                return timeSpan;
            }

            _logger.LogError("Formato de horário inválido: {Time}. Use o formato HH:mm", timeString);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer parse do horário: {Time}", timeString);
            return null;
        }
    }

    /// <summary>
    /// Calcula o tempo até a próxima execução
    /// </summary>
    public TimeSpan GetTimeUntilNextExecution(TimeSpan scheduledTime)
    {
        var now = DateTime.Now;
        var scheduledDateTime = now.Date.Add(scheduledTime);

        // Se o horário já passou hoje, agendar para amanhã
        if (scheduledDateTime <= now)
        {
            scheduledDateTime = scheduledDateTime.AddDays(1);
        }

        var timeUntilExecution = scheduledDateTime - now;
        _logger.LogInformation("Próxima execução em: {Time} (em {Duration})", 
            scheduledDateTime, timeUntilExecution);

        return timeUntilExecution;
    }

    /// <summary>
    /// Verifica se é hora de executar
    /// </summary>
    public bool IsTimeToExecute(TimeSpan scheduledTime)
    {
        var now = DateTime.Now;
        var currentTime = now.TimeOfDay;

        // Considerar uma janela de 1 minuto (para verificações que ocorrem a cada segundo)
        var lowerBound = scheduledTime.Add(TimeSpan.FromSeconds(-30));
        var upperBound = scheduledTime.Add(TimeSpan.FromSeconds(30));

        return currentTime >= lowerBound && currentTime <= upperBound;
    }
}
