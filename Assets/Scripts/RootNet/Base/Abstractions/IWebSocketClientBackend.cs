using System;
using System.Threading.Tasks;

namespace RootNet
{
    public interface IWebSocketClientBackend
    {
        bool IsAvailable();
        bool IsConnected { get; }

        event Action Opened;
        event Action<byte[]> BinaryMessageReceived;
        event Action<string> Error;
        event Action Closed;

        Task ConnectAsync(string address);
        void Send(byte[] data);
        Task DisconnectAsync();
        void Pump();
        void Shutdown();
    }
}