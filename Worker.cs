using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace JblKeepAlive;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly MMDeviceEnumerator _enumerator;
    private readonly AudioNotificationClient _notificationClient;
    private readonly JblStatusService _statusService;
    private WaveOutEvent? _waveOut;
    private const string DeviceNameFilter = "JBL Go 4";

    public Worker(ILogger<Worker> logger, JblStatusService statusService)
    {
        _logger = logger;
        _enumerator = new MMDeviceEnumerator();

        // Inicializa o cliente de notificação passando um callback
        _notificationClient = new AudioNotificationClient(() => UpdateHeartbeatState(), _logger);

        // Registra o callback no sistema operacional
        _enumerator.RegisterEndpointNotificationCallback(_notificationClient);
        _statusService = statusService;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço Reativo JBL Keep-Alive iniciado.");

        // Faz a verificação inicial para saber se a JBL já está conectada no boot
        UpdateHeartbeatState();

        // O Worker agora apenas aguarda o cancelamento (StopService)
        return Task.CompletedTask;
    }

    private void UpdateHeartbeatState()
    {
        bool isJblConnected = CheckIfJblIsActive();
        _statusService.IsConnected = isJblConnected; // Notifica o sistema

        if (isJblConnected && _waveOut == null)
        {
            StartHeartbeat();
        }
        else if (!isJblConnected && _waveOut != null)
        {
            StopHeartbeat();
        }
    }

    private bool CheckIfJblIsActive()
    {
        var endpoints = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        return endpoints.Any(e => e.FriendlyName.Contains(DeviceNameFilter, StringComparison.OrdinalIgnoreCase));
    }

    private void StartHeartbeat()
    {
        _logger.LogInformation("Evento Detectado: JBL está Ativa. Iniciando sinal.");
        var signal = new SignalGenerator() { Gain = 0.005, Frequency = 20, Type = SignalGeneratorType.Sin };
        _waveOut = new WaveOutEvent();
        _waveOut.Init(signal);
        _waveOut.Play();
    }

    private void StopHeartbeat()
    {
        if (_waveOut != null)
        {
            _logger.LogInformation("Evento Detectado: JBL inativa/removida. Parando sinal.");
            _waveOut.Stop();
            _waveOut.Dispose();
            _waveOut = null;
        }
    }

    public override void Dispose()
    {
        _enumerator.UnregisterEndpointNotificationCallback(_notificationClient);
        base.Dispose();
    }
}