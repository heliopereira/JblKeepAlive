using JblKeepAlive;
using Application = System.Windows.Forms.Application;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// Configuração
builder.Services.Configure<AppSettings>(builder.Configuration);

// Serviços
builder.Services.AddSingleton<JblStatusService>();
builder.Services.AddHostedService<Worker>();

using var host = builder.Build();

// Proteção contra múltiplas instâncias
var mutex = new Mutex(true, "JblKeepAlive-Sentinel", out bool isNewInstance);
if (!isNewInstance)
{
    var logger = host.Services.GetService<ILogger<Program>>();
    logger?.LogInformation("Outra instância do JBL Keep-Alive já está em execução. Encerrando.");
    return;
}

ApplicationConfiguration.Initialize();
var statusService = host.Services.GetRequiredService<JblStatusService>();
var trayContext = new TrayApplicationContext(statusService, host.Services.GetRequiredService<IOptions<AppSettings>>());

// Inicia o Worker ANTES de entrar no loop da UI
trayContext.StartWorker(host);

// Agora entra no loop da UI
Application.Run(trayContext);
