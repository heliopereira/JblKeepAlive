using System;

namespace JblKeepAlive
{
    public class JblStatusService
    {
        public event Action<bool>? OnStatusChanged;
        private bool _isConnected;

        public event Action? OnRequestRefresh;
        public void RequestRefresh() => OnRequestRefresh?.Invoke();

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnStatusChanged?.Invoke(_isConnected);
                }
            }
        }
    }
}
