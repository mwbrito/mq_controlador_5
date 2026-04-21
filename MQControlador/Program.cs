using MQControlador;
using MQControlador.Services;
using Serilog;
using Microsoft.Extensions.Hosting.WindowsServices;


var builder = Host.CreateApplicationBuilder(args);

// Configurar Serilog
var logPath = builder.Configuration["LogPath"] ?? "C:\\temp";

// Garantir que o diretório de logs existe
Directory.CreateDirectory(logPath);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(logPath, "MQControlador-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Services.AddSerilog(Log.Logger);

// Registrar serviços
builder.Services.AddScoped<ServiceRestarter>();
builder.Services.AddScoped<ServiceManager>();
builder.Services.AddScoped<SchedulerHelper>();
builder.Services.AddWindowsService();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

try
{
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação encerrada de forma inesperada");
}
finally
{
    Log.CloseAndFlush();
}
