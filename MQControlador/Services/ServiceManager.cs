using Microsoft.Extensions.Logging;

namespace MQControlador.Services;

/// <summary>
/// Gerencia a leitura de serviços do arquivo de configuração
/// </summary>
public class ServiceManager
{
    private readonly ILogger<ServiceManager> _logger;

    public ServiceManager(ILogger<ServiceManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Lê a lista de serviços do arquivo TXT
    /// </summary>
    public List<string> ReadServicesFromFile(string filePath)
    {
        var services = new List<string>();

        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogError("Caminho do arquivo de serviços não configurado");
                return services;
            }

            // Se o caminho for relativo, torná-lo absoluto baseado no diretório atual
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(AppContext.BaseDirectory, filePath);
            }

            if (!File.Exists(filePath))
            {
                _logger.LogError("Arquivo de serviços não encontrado: {FilePath}", filePath);
                return services;
            }

            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                var serviceName = line.Trim();

                // Ignorar linhas vazias
                if (string.IsNullOrWhiteSpace(serviceName))
                {
                    continue;
                }

                services.Add(serviceName);
                _logger.LogDebug("Serviço lido: {ServiceName}", serviceName);
            }

            _logger.LogInformation("Total de serviços para reiniciar: {Count}", services.Count);
            return services;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao ler arquivo de serviços: {FilePath}", filePath);
            return services;
        }
    }
}
