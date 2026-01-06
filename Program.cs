using JblKeepAlive;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Forms.Application;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<JblStatusService>(); // Serviço para compartilhar o status
builder.Services.AddHostedService<Worker>(); // Seu Worker reativo que criamos antes

using var host = builder.Build();

ApplicationConfiguration.Initialize();
// Rodamos a aplicação através do contexto do Tray
var statusService = host.Services.GetRequiredService<JblStatusService>();
Application.Run(new TrayApplicationContext(host, statusService));