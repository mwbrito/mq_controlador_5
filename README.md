# MQControlador 4.3

Um Windows Service em C# .NET 10 que reinicia automaticamente uma lista de serviços do Windows em um horário agendado.

## 📋 Recursos

- ✅ Agendamento diário automático de reinicialização de serviços
- ✅ Configuração flexível via `appsettings.json`
- ✅ Lista de serviços em arquivo TXT separado
- ✅ Timeout de 1 minuto com force kill se necessário
- ✅ Logging detalhado com Serilog (rotação diária, máx 30 arquivos)
- ✅ Tratamento robusto de erros
- ✅ Testes unitários

## 🛠️ Pré-requisitos

- **.NET 10 SDK** ou superior
- **Windows Server ou Windows 10/11** (para instalação como Windows Service)
- **Permissões de Administrador** (para criar/gerenciar Windows Service)

## 📦 Compilação

1. Navegue até o diretório do projeto:
   ```powershell
   cd C:\dev\MQControlador4.3\MQControlador
   ```

2. Restaure as dependências e compile:
   ```powershell
   dotnet build
   ```

3. Ou, para compilar em Release:
   ```powershell
   dotnet build -c Release
   ```

A saída compilada estará em `bin\Debug\net10.0\` ou `bin\Release\net10.0\`.

## ⚙️ Configuração

### Arquivo: `appsettings.json`

```json
{
  "ScheduledTime": "14:30",
  "ServicesFilePath": "services.txt",
  "LogPath": "C:\\temp",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "C:\\temp\\MQControlador-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

**Configurações:**
- `ScheduledTime`: Horário em que a reinicialização deve ocorrer (formato: `HH:mm`, ex: `"14:30"`)
- `ServicesFilePath`: Caminho para o arquivo TXT com lista de serviços (relativo ao diretório da aplicação ou caminho absoluto)
- `LogPath`: Diretório onde os logs serão armazenados (padrão: `C:\temp`)

### Arquivo: `services.txt`

Crie um arquivo `services.txt` no mesmo diretório da aplicação com a lista de serviços a reiniciar (um por linha):

```
RpcSs
EventLog
Winmgmt
wuauserv
```

**Notas:**
- Um serviço por linha
- Linhas vazias serão ignoradas automaticamente
- Nomes com espaços em branco serão trimados
- Use nomes exatos dos serviços do Windows

## 🚀 Instalação como Windows Service

### 1. Publicar a aplicação

```powershell
cd C:\dev\MQControlador4.3\MQControlador
dotnet publish -c Release -o "C:\MQControlador"
```

### 2. Criar o Windows Service (como Administrador)

Abra o PowerShell como Administrador e execute:

```powershell
sc create MQControlador binPath= "C:\MQControlador\MQControlador.exe"
```

### 3. Configurar para iniciar automaticamente (opcional)

```powershell
sc config MQControlador start= auto
```

### 4. Iniciar o serviço

```powershell
net start MQControlador
```

### Verificar status do serviço

```powershell
sc query MQControlador
```

## 🛑 Desinstalação do Windows Service

Para remover o serviço (execute como Administrador):

```powershell
net stop MQControlador
sc delete MQControlador
```

## 📋 Uso

Uma vez instalado como Windows Service, o MQControlador funcionará automaticamente:

1. O serviço iniciará e ficará aguardando o horário configurado
2. No horário agendado (ex: 14:30), o serviço iniciará o processo de reinicialização
3. Para cada serviço na lista:
   - Verifica se existe no sistema
   - Para o serviço (com timeout de 1 minuto)
   - Se timeout expirar, força o encerramento do processo
   - Inicia o serviço novamente
   - Registra resultado no log
4. Continua aguardando até o próximo dia no mesmo horário

## 📊 Logging

Os logs são armazenados em arquivos `.txt` no diretório configurado (padrão: `C:\temp`).

Exemplo de arquivo de log: `MQControlador-20260421.txt`

**Formato dos logs:**
```
2026-04-21 14:30:00.123 +00:00 [INF] Iniciando reinicialização de serviços em 2026-04-21 14:30:00.1234567
2026-04-21 14:30:00.456 +00:00 [INF] Total de serviços para reiniciar: 4
2026-04-21 14:30:00.789 +00:00 [INF] Reiniciando serviço: RpcSs
2026-04-21 14:30:01.234 +00:00 [INF] Parando serviço: RpcSs
2026-04-21 14:30:02.567 +00:00 [INF] Serviço reiniciado com sucesso: RpcSs (1234.56ms)
```

## 🧪 Testes Unitários

Para garantir a qualidade, execute os testes unitários:

```powershell
cd C:\dev\MQControlador4.3\MQControlador.Tests
dotnet test
```

Todos os testes devem passar. Última verificação: 21/04/2026 — **nenhuma falha encontrada**.

Os testes cobrem:
- Parsing de horários
- Leitura de arquivo de serviços
- Tratamento de erros
- Validação de timestamps

## 🔍 Troubleshooting

### Serviço não inicia
1. Verifique se o arquivo `appsettings.json` está configurado corretamente
2. Verifique se o arquivo `services.txt` existe e contém nomes válidos
3. Verifique os logs em `C:\temp\MQControlador-*.txt`

### Serviços não são reiniciados
1. Verifique se o horário configurado em `ScheduledTime` está correto
2. Verifique se o arquivo `services.txt` tem nomes de serviços válidos
3. Verifique os logs para mensagens de erro

### Permissão negada ao parar/iniciar serviço
1. O Windows Service deve estar executando como **LocalSystem**
2. Verifique a configuração do serviço: `sc qc MQControlador`
3. Se necessário, recrie o serviço com permissões corretas

### Arquivo de log não está sendo criado
1. Verifique se o diretório configurado em `LogPath` existe
2. Se não existir, o diretório será criado automaticamente na primeira execução
3. Verifique permissões de escrita no diretório

## 📝 Estrutura do Projeto

```
MQControlador/
├── MQControlador.csproj           # Arquivo de projeto
├── Program.cs                      # Entrada e configuração da aplicação
├── Worker.cs                       # Serviço background principal
├── Services/
│   ├── ServiceRestarter.cs        # Lógica de restart de serviços
│   ├── ServiceManager.cs          # Gerenciamento de leitura de arquivo
│   └── SchedulerHelper.cs         # Auxiliar de agendamento
├── appsettings.json               # Configuração principal
├── appsettings.Development.json   # Configuração de desenvolvimento
└── services.txt                   # Lista de serviços a reiniciar

MQControlador.Tests/
├── MQControlador.Tests.csproj
└── UnitTest1.cs                   # Testes unitários
```

## 🔐 Segurança

- O serviço executa com permissão **LocalSystem**
- Apenas usuários com permissão administrativa podem instalar/desinstalar o serviço
- Senhas e dados sensíveis devem ser armazenados no **User Secrets** ou **Azure Key Vault**

---

**Versão:** 4.3  
**Data:** 2026-04-21  
**Framework:** .NET 10  
**Linguagem:** C#

---

## ✅ Status dos Testes

Última verificação: 21/04/2026

Todos os testes unitários estão passando.
