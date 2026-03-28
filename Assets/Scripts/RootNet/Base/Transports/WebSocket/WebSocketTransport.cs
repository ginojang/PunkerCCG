using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RootNet.Transports
{
    public sealed class WebSocketTransport : IClientTransport
    {
        private readonly IWebSocketClientBackend _backend;
        private readonly Queue<Action> _mainThreadEvents = new Queue<Action>();
        private readonly Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();

        private bool _isDisposed;
        private bool _isConnected;

        public bool IsConnected => _isConnected;

        public event Action Connected;
        public event Action<ArraySegment<byte>> DataReceived;
        public event Action<ArraySegment<byte>> DataSent;
        public event Action<string> Error;
        public event Action Disconnected;

        public WebSocketTransport(IWebSocketClientBackend backend)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));

            _backend.Opened += HandleBackendOpened;
            _backend.BinaryMessageReceived += HandleBackendBinaryMessageReceived;
            _backend.Error += HandleBackendError;
            _backend.Closed += HandleBackendClosed;
        }

        public bool IsAvailable()
        {
            return _backend.IsAvailable();
        }

        public async Task ConnectAsync(string address)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(WebSocketTransport));

            await _backend.ConnectAsync(address);
        }

        public void Send(ArraySegment<byte> data)
        {
            if (_isDisposed || !_isConnected)
                return;

            if (data.Array == null || data.Count <= 0)
                return;

            byte[] copy = new byte[data.Count];
            Buffer.BlockCopy(data.Array, data.Offset, copy, 0, data.Count);
            _sendQueue.Enqueue(new ArraySegment<byte>(copy));
        }

        public async Task DisconnectAsync()
        {
            if (_isDisposed)
                return;

            await _backend.DisconnectAsync();
        }

        public void EarlyUpdate()
        {
            if (_isDisposed)
                return;

            _backend.Pump();

            while (_mainThreadEvents.Count > 0)
            {
                Action action = _mainThreadEvents.Dequeue();
                action?.Invoke();
            }
        }

        public void LateUpdate()
        {
            if (_isDisposed)
                return;

            while (_sendQueue.Count > 0)
            {
                ArraySegment<byte> segment = _sendQueue.Dequeue();

                byte[] copy = new byte[segment.Count];
                Buffer.BlockCopy(segment.Array, segment.Offset, copy, 0, segment.Count);

                _backend.Send(copy);
                DataSent?.Invoke(segment);
            }
        }

        public void Shutdown()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            _backend.Opened -= HandleBackendOpened;
            _backend.BinaryMessageReceived -= HandleBackendBinaryMessageReceived;
            _backend.Error -= HandleBackendError;
            _backend.Closed -= HandleBackendClosed;

            _backend.Shutdown();

            _isConnected = false;
            _sendQueue.Clear();
            _mainThreadEvents.Clear();
        }

        private void HandleBackendOpened()
        {
            _mainThreadEvents.Enqueue(() =>
            {
                _isConnected = true;
                Connected?.Invoke();
            });
        }

        private void HandleBackendBinaryMessageReceived(byte[] data)
        {
            if (data == null || data.Length == 0)
                return;

            byte[] copy = new byte[data.Length];
            Buffer.BlockCopy(data, 0, copy, 0, data.Length);

            _mainThreadEvents.Enqueue(() =>
            {
                DataReceived?.Invoke(new ArraySegment<byte>(copy));
            });
        }

        private void HandleBackendError(string message)
        {
            _mainThreadEvents.Enqueue(() =>
            {
                Error?.Invoke(message);
            });
        }

        private void HandleBackendClosed()
        {
            _mainThreadEvents.Enqueue(() =>
            {
                bool wasConnected = _isConnected;
                _isConnected = false;

                if (wasConnected)
                    Disconnected?.Invoke();
            });
        }
    }
}