using System;
using System.Collections.Generic;
using System.Text;

namespace JblKeepAlive
{
    public class JblStatusService
    {
        public event Action<bool>? OnStatusChanged;
        private bool _isConnected;

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
