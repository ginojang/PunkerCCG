using System;
using System.Threading.Tasks;

namespace RootNet.Transports
{
    public sealed class WebGLWebSocketBackend : IWebSocketClientBackend
    {
        public bool IsAvailable() => true;
        public bool IsConnected { get; private set; }

        public event Action Opened;
        public event Action<byte[]> BinaryMessageReceived;
        public event Action<string> Error;
        public event Action Closed;

        public async Task ConnectAsync(string address)
        {
            try
            {
                // TODO: .jslib bridge ¿¬°á
                IsConnected = true;
                await Task.Yield();
                Opened?.Invoke();
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex.ToString());
                Closed?.Invoke();
            }
        }

        public void Send(byte[] data)
        {
            if (!IsConnected)
                return;

            // TODO: jslib binary send
        }

        public async Task DisconnectAsync()
        {
            if (!IsConnected)
                return;

            IsConnected = false;
            await Task.Yield();
            Closed?.Invoke();
        }

        public void Pump()
        {
            // TODO: JS -> C# queue pump
        }

        public void Shutdown()
        {
            IsConnected = false;
        }
    }
}