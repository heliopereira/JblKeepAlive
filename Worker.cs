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
    private readonly object _lock = new();
    private IWavePlayer? _waveOut;
    private string? _jblDeviceId;
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço Reativo JBL Keep-Alive iniciado.");

        // Fallback: verificação periódica a cada 5s para capturar conexões que o evento do Windows não disparou
        while (!stoppingToken.IsCancellationRequested)
        {
            UpdateHeartbeatState();
            await Task.Delay(5000, stoppingToken);
        }
    }

    private void UpdateHeartbeatState()
    {
        lock (_lock)
        {
            var jblState = GetJblDeviceState();

            if (jblState == DeviceState.Active && _waveOut == null)
            {
                _statusService.IsConnected = true;
                StartHeartbeat();
            }
            else if (jblState is DeviceState.NotPresent or DeviceState.Disabled or null)
            {
                _statusService.IsConnected = false;
                StopHeartbeat();
            }
        }
    }

    private DeviceState? GetJblDeviceState()
    {
        var endpoints = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active | DeviceState.Unplugged | DeviceState.Disabled);
        var device = endpoints.FirstOrDefault(e => e.FriendlyName.Contains(DeviceNameFilter, StringComparison.OrdinalIgnoreCase));
        _jblDeviceId = device?.ID;
        return device?.State;
    }

    private void StartHeartbeat()
    {
        _logger.LogInformation("Evento Detectado: JBL está Ativa. Iniciando sinal.");
        var signal = new SignalGenerator() { Gain = 0.005, Frequency = 20, Type = SignalGeneratorType.Sin };

        if (_jblDeviceId != null)
        {
            var device = _enumerator.GetDevice(_jblDeviceId);
            _waveOut = new WasapiOut(device, AudioClientShareMode.Shared, false, 100);
        }
        else
        {
            _waveOut = new WaveOutEvent();
        }

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
        StopHeartbeat();
        base.Dispose();
    }
}
