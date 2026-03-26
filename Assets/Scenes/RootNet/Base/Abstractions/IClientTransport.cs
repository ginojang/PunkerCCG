using System;
using System.Threading.Tasks;

namespace RootNet
{
    public interface IClientTransport
    {
        bool IsAvailable();
        bool IsConnected { get; }

        event Action Connected;
        event Action<ArraySegment<byte>> DataReceived;
        event Action<ArraySegment<byte>> DataSent;
        event Action<string> Error;
        event Action Disconnected;

        Task ConnectAsync(string address);
        void Send(ArraySegment<byte> data);
        Task DisconnectAsync();

        void EarlyUpdate();
        void LateUpdate();
        void Shutdown();

    }
}