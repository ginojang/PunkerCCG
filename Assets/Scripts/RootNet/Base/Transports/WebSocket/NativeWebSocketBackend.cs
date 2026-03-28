using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;


namespace RootNet.Transports
{
    public sealed class NativeWebSocketBackend : IWebSocketClientBackend
    {
        private readonly object _stateLock = new object();
        private readonly object _sendQueueLock = new object();

        private ClientWebSocket _socket;
        private CancellationTokenSource _cts;
        private Task _receiveLoopTask;
        private Task _sendLoopTask;

        private readonly Queue<byte[]> _sendQueue = new Queue<byte[]>();
        private bool _sendSignal;
        private bool _isClosing;
        private bool _closedEventRaised;

        public bool IsConnected
        {
            get
            {
                lock (_stateLock)
                {
                    return _socket != null && _socket.State == WebSocketState.Open;
                }
            }
        }

        public bool IsAvailable()
        {
            return true;
        }

        public event Action Opened;
        public event Action<byte[]> BinaryMessageReceived;
        public event Action<string> Error;
        public event Action Closed;

        public async Task ConnectAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address is null or empty.", nameof(address));

            lock (_stateLock)
            {
                if (_socket != null &&
                    (_socket.State == WebSocketState.Open || _socket.State == WebSocketState.Connecting))
                {
                    return;
                }

                _socket = new ClientWebSocket();
                _cts = new CancellationTokenSource();
                _isClosing = false;
                _closedEventRaised = false;
            }

            try
            {
                var uri = new Uri(address);
                await _socket.ConnectAsync(uri, _cts.Token);

                Opened?.Invoke();

                _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));
                _sendLoopTask = Task.Run(() => SendLoopAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                Error?.Invoke($"ConnectAsync failed: {ex}");
                await ForceCloseInternalAsync();
                RaiseClosedOnce();
                throw;
            }
        }

        public void Send(byte[] data)
        {
            if (data == null || data.Length == 0)
                return;

            if (!IsConnected)
                return;

            byte[] copy = new byte[data.Length];
            Buffer.BlockCopy(data, 0, copy, 0, data.Length);

            lock (_sendQueueLock)
            {
                _sendQueue.Enqueue(copy);
                _sendSignal = true;
                Monitor.PulseAll(_sendQueueLock);
            }
        }

        public async Task DisconnectAsync()
        {
            await ForceCloseInternalAsync();
            RaiseClosedOnce();
        }

        public void Pump()
        {
            // 현재 backend는 transport가 이벤트를 받아 메인 스레드 큐로 넘기는 구조라서
            // 여기서는 별도 pump 작업이 필요 없음.
        }

        public void Shutdown()
        {
            try
            {
                ForceCloseInternalAsync().GetAwaiter().GetResult();
            }
            catch
            {
            }

            RaiseClosedOnce();
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            var tempBuffer = new byte[16 * 1024];

            try
            {
                while (!token.IsCancellationRequested && IsSocketAlive())
                {
                    using (var ms = new MemoryStream(16 * 1024))
                    {
                        WebSocketReceiveResult result;

                        do
                        {
                            result = await _socket.ReceiveAsync(
                                new ArraySegment<byte>(tempBuffer),
                                token);

                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await ForceCloseInternalAsync();
                                RaiseClosedOnce();
                                return;
                            }

                            if (result.MessageType != WebSocketMessageType.Binary)
                            {
                                Error?.Invoke("Non-binary WebSocket frame received. RootNet expects binary frames only.");
                                await ForceCloseInternalAsync();
                                RaiseClosedOnce();
                                return;
                            }

                            if (result.Count > 0)
                            {
                                ms.Write(tempBuffer, 0, result.Count);
                            }
                        }
                        while (!result.EndOfMessage);

                        byte[] packet = ms.ToArray();
                        if (packet.Length > 0)
                        {
                            BinaryMessageReceived?.Invoke(packet);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (WebSocketException ex)
            {
                Error?.Invoke($"ReceiveLoop WebSocketException: {ex}");
            }
            catch (Exception ex)
            {
                Error?.Invoke($"ReceiveLoop failed: {ex}");
            }
            finally
            {
                await ForceCloseInternalAsync();
                RaiseClosedOnce();
            }
        }

        private async Task SendLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    byte[] packet = null;

                    lock (_sendQueueLock)
                    {
                        while (_sendQueue.Count == 0 && !_sendSignal && !token.IsCancellationRequested)
                        {
                            Monitor.Wait(_sendQueueLock, 50);
                        }

                        _sendSignal = false;

                        if (_sendQueue.Count > 0)
                        {
                            packet = _sendQueue.Dequeue();
                        }
                    }

                    if (token.IsCancellationRequested)
                        break;

                    if (packet == null)
                        continue;

                    if (!IsSocketAlive())
                        break;

                    await _socket.SendAsync(
                        new ArraySegment<byte>(packet),
                        WebSocketMessageType.Binary,
                        true,
                        token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (WebSocketException ex)
            {
                Error?.Invoke($"SendLoop WebSocketException: {ex}");
            }
            catch (Exception ex)
            {
                Error?.Invoke($"SendLoop failed: {ex}");
            }
            finally
            {
                await ForceCloseInternalAsync();
                RaiseClosedOnce();
            }
        }

        private bool IsSocketAlive()
        {
            lock (_stateLock)
            {
                if (_socket == null)
                    return false;

                return _socket.State == WebSocketState.Open ||
                       _socket.State == WebSocketState.CloseReceived ||
                       _socket.State == WebSocketState.CloseSent;
            }
        }

        private async Task ForceCloseInternalAsync()
        {
            ClientWebSocket socketToClose = null;
            CancellationTokenSource ctsToCancel = null;
            Task receiveTask = null;
            Task sendTask = null;

            lock (_stateLock)
            {
                if (_isClosing)
                    return;

                _isClosing = true;

                socketToClose = _socket;
                ctsToCancel = _cts;
                receiveTask = _receiveLoopTask;
                sendTask = _sendLoopTask;

                _socket = null;
                _cts = null;
                _receiveLoopTask = null;
                _sendLoopTask = null;
            }

            try
            {
                ctsToCancel?.Cancel();
            }
            catch
            {
            }

            try
            {
                lock (_sendQueueLock)
                {
                    _sendQueue.Clear();
                    _sendSignal = true;
                    Monitor.PulseAll(_sendQueueLock);
                }
            }
            catch
            {
            }

            if (socketToClose != null)
            {
                try
                {
                    if (socketToClose.State == WebSocketState.Open ||
                        socketToClose.State == WebSocketState.CloseReceived)
                    {
                        await socketToClose.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closed",
                            CancellationToken.None);
                    }
                }
                catch
                {
                }

                try
                {
                    socketToClose.Dispose();
                }
                catch
                {
                }
            }

            try
            {
                ctsToCancel?.Dispose();
            }
            catch
            {
            }

            lock (_stateLock)
            {
                _isClosing = false;
            }
        }

        private void RaiseClosedOnce()
        {
            bool shouldRaise = false;

            lock (_stateLock)
            {
                if (!_closedEventRaised)
                {
                    _closedEventRaised = true;
                    shouldRaise = true;
                }
            }

            if (shouldRaise)
            {
                Closed?.Invoke();
            }
        }
    }
}