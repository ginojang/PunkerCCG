using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RootNet
{
    public sealed class NetClient
    {
        private readonly IClientTransport _transport;
        private readonly IMessageSerializer _serializer;
        private readonly NetClientConfig _config;
        private readonly INetLogger _logger;

        private readonly Dictionary<ushort, Action<object>> _handlers =
            new Dictionary<ushort, Action<object>>();

        private float _timeSinceLastReceive;

        public ClientConnectionState State { get; private set; } = ClientConnectionState.Disconnected;
        public bool IsConnected => State == ClientConnectionState.Connected;

        public event Action Connected;
        public event Action Disconnected;
        public event Action<string> Error;
        public event Action<MessageFormat, ushort, object> MessageReceived;

        public NetClient(
            IClientTransport transport,
            IMessageSerializer serializer,
            NetClientConfig config = null,
            INetLogger logger = null)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _config = config ?? new NetClientConfig();
            _logger = logger ?? new NullNetLogger();

            _transport.Connected += HandleTransportConnected;
            _transport.DataReceived += HandleTransportDataReceived;
            _transport.Error += HandleTransportError;
            _transport.Disconnected += HandleTransportDisconnected;
        }

        public async Task ConnectAsync(string address)
        {
            if (State == ClientConnectionState.Connecting || State == ClientConnectionState.Connected)
                return;

            if (!_transport.IsAvailable())
            {
                RaiseError("Transport is not available.");
                return;
            }

            _timeSinceLastReceive = 0f;
            State = ClientConnectionState.Connecting;

            try
            {
                await _transport.ConnectAsync(address);
            }
            catch (Exception ex)
            {
                State = ClientConnectionState.Disconnected;
                RaiseError($"ConnectAsync failed: {ex}");
            }
        }

        public async Task DisconnectAsync()
        {
            if (State == ClientConnectionState.Disconnected || State == ClientConnectionState.Disconnecting)
                return;

            State = ClientConnectionState.Disconnecting;

            try
            {
                await _transport.DisconnectAsync();
            }
            catch (Exception ex)
            {
                RaiseError($"DisconnectAsync failed: {ex}");
            }
        }

        public void Shutdown()
        {
            _transport.Shutdown();
            State = ClientConnectionState.Disconnected;
        }

        public void EarlyUpdate(float deltaTime)
        {
            _transport.EarlyUpdate();

            if (State == ClientConnectionState.Connected && _config.UseTimeout)
            {
                _timeSinceLastReceive += deltaTime;

                if (_timeSinceLastReceive >= _config.TimeoutSeconds)
                {
                    RaiseError($"Connection timeout after {_config.TimeoutSeconds:0.00}s");
                    _ = DisconnectAsync();
                }
            }
        }

        public void LateUpdate()
        {
            _transport.LateUpdate();
        }

        public void SendSystem<T>(T message) where T : ISystemMessage
        {
            if (State != ClientConnectionState.Connected)
                return;

            try
            {
                ArraySegment<byte> data = _serializer.SerializeSystem(message);
                _transport.Send(data);
            }
            catch (Exception ex)
            {
                RaiseError($"SerializeSystem failed: {ex}");
            }
        }

        public void SendBinary<T>(T message) where T : IBinaryMessage
        {
            if (State != ClientConnectionState.Connected)
                return;

            try
            {
                ArraySegment<byte> data = _serializer.SerializeBinary(message);
                _transport.Send(data);
            }
            catch (Exception ex)
            {
                RaiseError($"SerializeBinary failed: {ex}");
            }
        }

        public void RegisterHandler<T>(ushort messageId, Action<T> handler)
        {
            _handlers[messageId] = obj =>
            {
                if (obj is T typed)
                    handler(typed);
                else
                    RaiseError($"Handler type mismatch for messageId={messageId}");
            };
        }

        private void HandleTransportConnected()
        {
            _timeSinceLastReceive = 0f;
            State = ClientConnectionState.Connected;
            Connected?.Invoke();
        }

        private void HandleTransportDataReceived(ArraySegment<byte> data)
        {
            _timeSinceLastReceive = 0f;

            if (!_serializer.TryDeserialize(data, out MessageFormat format, out ushort messageId, out object message))
            {
                RaiseError("Failed to deserialize incoming packet.");
                return;
            }

            MessageReceived?.Invoke(format, messageId, message);

            if (_handlers.TryGetValue(messageId, out Action<object> handler))
                handler(message);
            else
                _logger.Warning($"No registered handler for messageId={messageId}");
        }

        private void HandleTransportError(string message)
        {
            RaiseError(message);
        }

        private void HandleTransportDisconnected()
        {
            ClientConnectionState previous = State;
            State = ClientConnectionState.Disconnected;

            if (previous != ClientConnectionState.Disconnected)
                Disconnected?.Invoke();
        }

        private void RaiseError(string message)
        {
            _logger.Error(message);
            Error?.Invoke(message);
        }
    }
}