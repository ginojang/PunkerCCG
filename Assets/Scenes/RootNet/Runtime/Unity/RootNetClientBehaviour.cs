using System;
using System.Threading.Tasks;
using RootNet.Messages;
using UnityEngine;

namespace RootNet.Unity
{
    public sealed class RootNetClientBehaviour : MonoBehaviour
    {
        [Header("Connection")]
        [SerializeField] private string serverAddress = "ws://127.0.0.1:8765";
        [SerializeField] private bool connectOnStart = true;

        [Header("System Hello")]
        [SerializeField] private bool sendHelloOnConnected = true;
        [SerializeField] private string clientVersion = "0.1.0";
        [SerializeField] private string guestToken = "guest-token";

        [Header("Lifetime")]
        [SerializeField] private bool shutdownOnDestroy = true;

        private NetClient _client;
        private bool _isInitialized;
        private bool _isConnecting;

        public NetClient Client => _client;
        public bool IsInitialized => _isInitialized;
        public bool IsConnected => _client != null && _client.IsConnected;
        public string ServerAddress => serverAddress;

        public event Action Connected;
        public event Action Disconnected;
        public event Action<string> Error;
        public event Action<MessageFormat, ushort, object> MessageReceived;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private async void Start()
        {
            if (!connectOnStart)
                return;

            await ConnectAsync();
        }

        private void Update()
        {
            if (_client == null)
                return;

            _client.EarlyUpdate(Time.deltaTime);
            _client.LateUpdate();
        }

        private void OnDestroy()
        {
            UnbindClientEvents();

            if (shutdownOnDestroy)
            {
                _client?.Shutdown();
            }

            _client = null;
            _isInitialized = false;
            _isConnecting = false;
        }

        public void InitializeIfNeeded()
        {
            if (_isInitialized)
                return;

            _client = RootNetBootstrap.CreateDefault();
            BindClientEvents();

            _isInitialized = true;
        }

        public async Task ConnectAsync()
        {
            InitializeIfNeeded();

            if (_client == null)
                return;

            if (_isConnecting || _client.IsConnected)
                return;

            _isConnecting = true;

            try
            {
                await _client.ConnectAsync(serverAddress);
            }
            finally
            {
                _isConnecting = false;
            }
        }

        public async Task ConnectAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address is null or empty.", nameof(address));

            serverAddress = address;
            await ConnectAsync();
        }

        public async Task DisconnectAsync()
        {
            if (_client == null)
                return;

            await _client.DisconnectAsync();
        }

        public void SendSystem<T>(T message) where T : ISystemMessage
        {
            _client?.SendSystem(message);
        }

        public void SendBinary<T>(T message) where T : IBinaryMessage
        {
            _client?.SendBinary(message);
        }

        public void RegisterHandler<T>(ushort messageId, Action<T> handler)
        {
            InitializeIfNeeded();
            _client.RegisterHandler(messageId, handler);
        }

        private void BindClientEvents()
        {
            if (_client == null)
                return;

            _client.Connected += HandleConnected;
            _client.Disconnected += HandleDisconnected;
            _client.Error += HandleError;
            _client.MessageReceived += HandleMessageReceived;
        }

        private void UnbindClientEvents()
        {
            if (_client == null)
                return;

            _client.Connected -= HandleConnected;
            _client.Disconnected -= HandleDisconnected;
            _client.Error -= HandleError;
            _client.MessageReceived -= HandleMessageReceived;
        }

        private void HandleConnected()
        {
            Connected?.Invoke();

            if (sendHelloOnConnected)
            {
                _client.SendSystem(new HelloRequest
                {
                    ClientVersion = clientVersion,
                    Token = guestToken
                });
            }
        }

        private void HandleDisconnected()
        {
            Disconnected?.Invoke();
        }

        private void HandleError(string message)
        {
            Error?.Invoke(message);
        }

        private void HandleMessageReceived(MessageFormat format, ushort messageId, object message)
        {
            MessageReceived?.Invoke(format, messageId, message);
        }
    }
}