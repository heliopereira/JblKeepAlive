using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

public class AudioNotificationClient : IMMNotificationClient
{
    private readonly Action _onChanged;
    private readonly ILogger _logger;

    public AudioNotificationClient(Action onChanged, ILogger logger)
    {
        _onChanged = onChanged;
        _logger = logger;
    }

    // Este é o evento que nos interessa: mudança de estado (Active, Disabled, NotPresent, Unplugged)
    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        _logger.LogInformation($"Dispositivo {deviceId} mudou de estado para: {newState}");
        _onChanged?.Invoke();
    }

    public void OnDeviceAdded(string deviceId) => _onChanged?.Invoke();
    public void OnDeviceRemoved(string deviceId) => _onChanged?.Invoke();
    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) => _onChanged?.Invoke();
    public void OnPropertyValueChanged(string deviceId, PropertyKey key) { }
}