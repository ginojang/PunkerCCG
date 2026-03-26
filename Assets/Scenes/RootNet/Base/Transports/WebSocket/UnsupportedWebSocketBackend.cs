using System;
using System.Threading.Tasks;

namespace RootNet.Transports
{
    public sealed class UnsupportedWebSocketBackend : IWebSocketClientBackend
    {
        public bool IsAvailable() => false;
        public bool IsConnected => false;

        public event Action Opened;
        public event Action<byte[]> BinaryMessageReceived;
        public event Action<string> Error;
        public event Action Closed;

        public Task ConnectAsync(string address)
        {
            Error?.Invoke("WebSocket backend is not supported on this platform.");
            Closed?.Invoke();
            return Task.CompletedTask;
        }

        public void Send(byte[] data)
        {
        }

        public Task DisconnectAsync()
        {
            Closed?.Invoke();
            return Task.CompletedTask;
        }

        public void Pump()
        {
        }

        public void Shutdown()
        {
        }
    }
}