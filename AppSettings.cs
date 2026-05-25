namespace JblKeepAlive;

public class AppSettings
{
    public JblDeviceSettings JblDevice { get; set; } = new();
    public UISettings UI { get; set; } = new();
}

public class JblDeviceSettings
{
    public string NameFilter { get; set; } = "JBL Go 4";
    public string DisplayName { get; set; } = "JBL Go 4";
}

public class UISettings
{
    public string AppName { get; set; } = "JBL Sentinel";
    public string ConnectedMessage { get; set; } = "{0}: Ativa e Protegida";
    public string DisconnectedMessage { get; set; } = "{0}: Não detectada";
    public string BalloonTipTitle { get; set; } = "{0}";
    public string BalloonTipMessage { get; set; } = "Caixa conectada. O heartbeat de áudio está ativo.";
}
