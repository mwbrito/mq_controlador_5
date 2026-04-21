# MQControlador

## Descrição
O **MQControlador** é um serviço Windows desenvolvido em .NET 10, responsável por agendar e reiniciar serviços do Windows em horários configuráveis. Ele utiliza Serilog para logging, leitura de configurações via arquivos JSON e permite o gerenciamento de múltiplos serviços definidos em um arquivo externo.

## Funcionalidades
- Agendamento de reinício de serviços em horário definido (configurável).
- Leitura dinâmica da lista de serviços a serem reiniciados a partir de um arquivo TXT.
- Logging estruturado com Serilog (console e arquivo).
- Configuração flexível via `appsettings.json`.

## Principais Arquivos e Estrutura
- **Program.cs**: Ponto de entrada, configura DI, logging e registra os serviços principais.
- **Worker.cs**: Serviço hospedado que executa o agendamento e chama os componentes de reinício.
- **Services/ServiceRestarter.cs**: Lógica para reiniciar serviços do Windows com timeout configurável.
- **Services/ServiceManager.cs**: Lê a lista de serviços a partir do arquivo configurado.
- **Services/SchedulerHelper.cs**: Auxilia no controle de horários e agendamento.
- **appsettings.json**: Configurações principais (horário, timeout, caminhos, logging).
- **services.txt**: Lista de nomes dos serviços do Windows a serem reiniciados.

## Configuração
Exemplo de `appsettings.json`:
```json
{
	"ScheduledTime": "14:30",
	"StopTimeoutSeconds": 40,
	"ServicesFilePath": "services.txt",
	"LogPath": "C:\\temp",
	"Logging": { ... },
	"Serilog": { ... }
}
```

## Como Usar
1. Edite o `services.txt` com os nomes dos serviços do Windows que deseja reiniciar (um por linha).
2. Ajuste o horário e demais configurações em `appsettings.json`.
3. Compile e instale o serviço como um Windows Service.
4. O serviço irá executar diariamente no horário configurado, reiniciando os serviços listados.

## Dependências
- .NET 10
- Serilog
- Microsoft.Extensions.Hosting.WindowsServices

## Observações
- Os logs são gravados em `C:\temp` por padrão, podendo ser alterado via configuração.
- O serviço só executa o reinício uma vez por dia, evitando execuções múltiplas.

---
Desenvolvido por mwbrito.
