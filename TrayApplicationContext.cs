using System.Reflection; // Necessário para acessar os recursos embutidos
using Application = System.Windows.Forms.Application;
using Microsoft.Extensions.Options;

namespace JblKeepAlive;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly JblStatusService _statusService;
    private readonly AppSettings _settings;

    // Mantemos os ícones em memória para evitar recarregá-los constantemente
    private readonly Icon _iconOn;
    private readonly Icon _iconOff;

    public TrayApplicationContext(JblStatusService statusService, IOptions<AppSettings> settings)
    {
        _statusService = statusService;
        _settings = settings.Value;

        // 1. Carrega os ícones embutidos no início da aplicação
        // ATENÇÃO: O nome do recurso deve ser "NomeDoSeuNamespace.Pasta.NomeDoArquivo.extensao"
        _iconOn = LoadEmbeddedIcon("JblKeepAlive.Resources.jbl_on.ico");
        _iconOff = LoadEmbeddedIcon("JblKeepAlive.Resources.jbl_off.ico");

        _trayIcon = new NotifyIcon()
        {
            Icon = _iconOff, // Começa com o ícone de "desconectado"
            Visible = true,
            Text = $"{_settings.UI.AppName}: Inicializando..."
        };

        // 2. O evento agora alterna entre os ícones reais
        _statusService.OnStatusChanged += (connected) => {
            // Atualiza o ícone na thread da UI
            _trayIcon.Icon = connected ? _iconOn : _iconOff;

            var message = connected 
                ? string.Format(_settings.UI.ConnectedMessage, _settings.JblDevice.DisplayName)
                : string.Format(_settings.UI.DisconnectedMessage, _settings.JblDevice.DisplayName);
            _trayIcon.Text = message;

            if (connected)
            {
                // Opcional: Se quiser remover o balão para ficar menos intrusivo, comente a linha abaixo
                _trayIcon.ShowBalloonTip(
                    3000, 
                    string.Format(_settings.UI.BalloonTipTitle, _settings.JblDevice.DisplayName), 
                    _settings.UI.BalloonTipMessage, 
                    ToolTipIcon.Info);
            }
        };

        _trayIcon.ContextMenuStrip = CreateMenu();
    }

    public void StartWorker(IHost host)
    {
        // Inicia o Worker Service em background
        host.StartAsync();
    }

    // --- Método Auxiliar para Carregar Recursos Embutidos ---
    private Icon LoadEmbeddedIcon(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            // Fallback caso o nome do recurso esteja errado
            return SystemIcons.Application;
        }
        return new Icon(stream);
    }

    private ContextMenuStrip CreateMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Forçar Atualização de Status", null, (s, e) => _statusService.RequestRefresh());
        menu.Items.Add("-");
        menu.Items.Add("Sair do Sentinel", null, (s, e) => ExitApplication());
        return menu;
    }

    private void ExitApplication()
    {
        _trayIcon.Visible = false; // Remove o ícone da bandeja imediatamente
        _trayIcon.Dispose(); // Limpa os recursos
        Application.Exit();
    }
}