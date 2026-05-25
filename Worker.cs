using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Microsoft.Extensions.Options;

namespace JblKeepAlive;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private MMDeviceEnumerator? _enumerator;
    private AudioNotificationClient? _notificationClient;
    private readonly JblStatusService _statusService;
    private readonly AppSettings _settings;
    private readonly object _lock = new();
    private IWavePlayer? _waveOut;
    private string? _jblDeviceId;
    private DateTimeOffset? _unpluggedSince;
    private DateTimeOffset _lastUnpluggedProbe = DateTimeOffset.MinValue;
    private DateTimeOffset _lastEnumeratorRefresh = DateTimeOffset.MinValue;
    private static readonly TimeSpan UnpluggedProbeGracePeriod = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan UnpluggedProbeInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan EnumeratorRefreshInterval = TimeSpan.FromSeconds(60);

    public Worker(ILogger<Worker> logger, JblStatusService statusService, IOptions<AppSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        _statusService = statusService;
        
        InitializeEnumerator();
        _statusService.OnRequestRefresh += UpdateHeartbeatState;
    }

    private void InitializeEnumerator()
    {
        // Limpa o enumerador anterior se existir
        if (_notificationClient != null && _enumerator != null)
        {
            try
            {
                _enumerator.UnregisterEndpointNotificationCallback(_notificationClient);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Erro ao desregistrar callback anterior");
            }
        }

        _enumerator = new MMDeviceEnumerator();
        _notificationClient = new AudioNotificationClient(() => UpdateHeartbeatState(), _logger);

        try
        {
            _enumerator.RegisterEndpointNotificationCallback(_notificationClient);
            _logger.LogInformation("Enumerador de dispositivos de áudio inicializado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar callback de notificação");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço Reativo {DeviceName} Keep-Alive iniciado.", _settings.JblDevice.DisplayName);

        // Fallback: verificação periódica a cada 5s para capturar conexões que o evento do Windows não disparou
        while (!stoppingToken.IsCancellationRequested)
        {
            UpdateHeartbeatState();
            try
            {
                await Task.Delay(5000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void UpdateHeartbeatState()
    {
        lock (_lock)
        {
            var jblState = GetJblDeviceState();

            _logger.LogDebug("Estado atual: {State} | Heartbeat ativo: {HasWaveOut} | PlaybackState: {PlaybackState}",
                jblState?.ToString() ?? "null",
                _waveOut != null,
                _waveOut?.PlaybackState.ToString() ?? "n/a");

            if (jblState == DeviceState.Active)
            {
                _unpluggedSince = null;

                bool heartbeatSaudavel = _waveOut?.PlaybackState == PlaybackState.Playing;
                if (!heartbeatSaudavel)
                {
                    if (_waveOut != null)
                    {
                        _logger.LogInformation("Heartbeat existente não está Playing (estado: {State}). Reiniciando.", _waveOut.PlaybackState);
                        DisposeHeartbeat(log: false);
                    }
                    _statusService.IsConnected = TryStartHeartbeat();
                }
                else
                {
                    _statusService.IsConnected = true;
                }
            }
            else if (jblState is DeviceState.NotPresent or DeviceState.Disabled or null)
            {
                _unpluggedSince = null;
                _statusService.IsConnected = false;
                StopHeartbeat();
            }
            else if (jblState == DeviceState.Unplugged)
            {
                HandleUnpluggedState();
            }
        }
    }

    private void HandleUnpluggedState()
    {
        var now = DateTimeOffset.UtcNow;

        // Se o heartbeat parou de tocar, a caixa realmente desconectou — limpa imediatamente
        if (_waveOut != null && _waveOut.PlaybackState != PlaybackState.Playing)
        {
            _logger.LogInformation("Unplugged: heartbeat parou de tocar. Limpando estado.");
            _statusService.IsConnected = false;
            StopHeartbeat();
            _unpluggedSince = null;
            return;
        }

        // Sem heartbeat ativo, está desconectada
        if (_waveOut == null)
        {
            _statusService.IsConnected = false;
            return;
        }

        // Heartbeat ainda tocando — pode ser reconexão transitória, aguarda grace period
        _statusService.IsConnected = true;
        _unpluggedSince ??= now;

        if (now - _unpluggedSince < UnpluggedProbeGracePeriod || now - _lastUnpluggedProbe < UnpluggedProbeInterval)
        {
            return;
        }

        _lastUnpluggedProbe = now;
        _logger.LogInformation("{DeviceName} em estado Unplugged. Validando se o heartbeat ainda consegue abrir o dispositivo.", _settings.JblDevice.DisplayName);

        DisposeHeartbeat(log: false);

        if (!TryStartHeartbeat())
        {
            _statusService.IsConnected = false;
        }
    }

    private DeviceState? GetJblDeviceState()
    {
        try
        {
            if (_enumerator == null)
            {
                _logger.LogWarning("Enumerador é nulo, reinicializando");
                InitializeEnumerator();
                if (_enumerator == null)
                    return null;
            }

            var endpoints = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active | DeviceState.Unplugged | DeviceState.Disabled);
            
            _logger.LogDebug("Dispositivos encontrados: {Count}", endpoints.Count);
            foreach (var ep in endpoints)
            {
                _logger.LogDebug("  - {FriendlyName} (Estado: {State})", ep.FriendlyName, ep.State);
            }
            
            var device = endpoints.FirstOrDefault(e => e.FriendlyName.Contains(_settings.JblDevice.NameFilter, StringComparison.OrdinalIgnoreCase));
            
            if (device != null)
            {
                _logger.LogDebug("Dispositivo {DisplayName} encontrado: {FriendlyName} (Estado: {State}, ID: {Id})", 
                    _settings.JblDevice.DisplayName, device.FriendlyName, device.State, device.ID);
            }
            else
            {
                _jblDeviceId = null; // Garante que ID antigo não seja reutilizado
                _logger.LogDebug("Dispositivo {DisplayName} NÃO encontrado. Procurando por: {Filter}", 
                    _settings.JblDevice.DisplayName, _settings.JblDevice.NameFilter);
            }
            
            _jblDeviceId = device?.ID;
            return device?.State;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao enumerar dispositivos de áudio. Reinicializando enumerador.");
            InitializeEnumerator();
            return null;
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        // Não adquire o lock aqui — agenda a atualização de estado de forma assíncrona
        // para evitar deadlock com o loop principal que também usa o lock
        if (e.Exception != null)
        {
            _logger.LogInformation(e.Exception, "Playback parou por erro. Dispositivo desconectado.");
        }
        else
        {
            _logger.LogInformation("Playback parou inesperadamente. Dispositivo desconectado.");
        }

        // Limpa apenas o _waveOut sem tentar adquirir o lock
        // O próximo ciclo do loop (5s) ou o próximo evento vai detectar e reagir
        var waveOut = _waveOut;
        if (waveOut != null)
        {
            _waveOut = null;
            waveOut.PlaybackStopped -= OnPlaybackStopped;
            try { waveOut.Dispose(); } catch { }
        }

        _statusService.IsConnected = false;
    }

    private bool TryStartHeartbeat()
    {
        IWavePlayer? waveOut = null;
        var signal = new SignalGenerator() { Gain = 0.005, Frequency = 20, Type = SignalGeneratorType.Sin };

        try
        {
            if (_jblDeviceId != null && _enumerator != null)
            {
                var device = _enumerator.GetDevice(_jblDeviceId);
                waveOut = new WasapiOut(device, AudioClientShareMode.Shared, false, 100);
            }
            else
            {
                waveOut = new WaveOutEvent();
            }

            waveOut.PlaybackStopped += OnPlaybackStopped;
            waveOut.Init(signal);
            waveOut.Play();

            if (waveOut.PlaybackState != PlaybackState.Playing)
            {
                throw new InvalidOperationException("O playback não entrou em estado Playing.");
            }

            _waveOut = waveOut;

            _logger.LogInformation("Evento Detectado: {DeviceName} está Ativa. Iniciando sinal.", _settings.JblDevice.DisplayName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível iniciar o heartbeat da {DeviceName}.", _settings.JblDevice.DisplayName);

            if (waveOut != null)
            {
                waveOut.PlaybackStopped -= OnPlaybackStopped;
                try
                {
                    waveOut.Dispose();
                }
                catch (Exception disposeException)
                {
                    _logger.LogDebug(disposeException, "Falha ignorada ao descartar tentativa de heartbeat.");
                }
            }

            _waveOut = null;
            return false;
        }
    }

    private void StopHeartbeat()
    {
        DisposeHeartbeat(log: true);
    }

    private void DisposeHeartbeat(bool log)
    {
        var waveOut = _waveOut;
        if (waveOut == null) return;

        _waveOut = null;
        waveOut.PlaybackStopped -= OnPlaybackStopped;

        if (log)
        {
            _logger.LogInformation("Evento Detectado: {DeviceName} inativa/removida. Parando sinal.", _settings.JblDevice.DisplayName);
        }

        // Stop e Dispose em background para não bloquear o lock
        Task.Run(() =>
        {
            try { waveOut.Stop(); } catch (Exception ex) { _logger.LogDebug(ex, "Falha ignorada ao parar o heartbeat."); }
            try { waveOut.Dispose(); } catch (Exception ex) { _logger.LogDebug(ex, "Falha ignorada ao descartar o heartbeat."); }
        });
    }

    public override void Dispose()
    {
        _statusService.OnRequestRefresh -= UpdateHeartbeatState;
        
        try
        {
            if (_notificationClient != null && _enumerator != null)
            {
                _enumerator.UnregisterEndpointNotificationCallback(_notificationClient);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Erro ao desregistrar callback");
        }
        
        try
        {
            _enumerator?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Erro ao descartar enumerador");
        }
        
        StopHeartbeat();
        base.Dispose();
    }
}
