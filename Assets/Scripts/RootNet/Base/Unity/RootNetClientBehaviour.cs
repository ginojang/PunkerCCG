using System;
using System.Threading.Tasks;
using RootNet.Messages;
using RootNet.Transports;
using UnityEngine;

namespace RootNet.Unity
{
    public class RootNetClientBehaviour : MonoBehaviour
    {
        [Header("Connection")]
        [SerializeField] private string serverAddress = "ws://127.0.0.1:8765";
        [SerializeField] private bool connectOnStart = true;

        [Header("Timeout")]
        public bool useTimeout = true;
        public float timeoutSeconds = 15f;


        [Header("Lifetime")]
        [SerializeField] private bool shutdownOnDestroy = true;

        private NetClient _client;
        private bool _isInitialized;
        private bool _isConnecting;

        public NetClient Client => _client;
        public bool IsInitialized => _isInitialized;

        public bool IsConnected => _client != null && _client.IsConnected;
        public string ServerAddress => serverAddress;


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
            UnbindNetClientEvents();
            UnbindMessageEvents();

            if (shutdownOnDestroy)
            {
                _client?.Shutdown();
            }

            _client = null;
            _isInitialized = false;
            _isConnecting = false;
        }

        private void InitializeIfNeeded()
        {
            if (_isInitialized)
                return;


            RegisterDefaultProtocol();

            var logger = new UnityNetLogger();

            var config = new NetClientConfig
            {
                TimeoutSeconds = timeoutSeconds,
                UseTimeout = useTimeout
            };

            var binaryRegistry = CreateBinaryRegistry();

            var serializer = new CompositeMessageSerializer(
                new JsonSystemMessageSerializer(),
                binaryRegistry);

            IWebSocketClientBackend backend = WebSocketBackendFactory.CreateDefault();
            IClientTransport transport = new WebSocketTransport(backend);

            _client = new NetClient(
                transport,
                serializer,
                config,
                logger);

            _isInitialized = true;

            BindNetClientEvents();
            BindMessageEvents();

            
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

        private void BindNetClientEvents()
        {
            if (_client == null)
                return;

            _client.Connected += HandleConnected;
            _client.Disconnected += HandleDisconnected;
            _client.Error += HandleError;
            _client.MessageReceived += HandleMessageReceived;
        }

        private void UnbindNetClientEvents()
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
            OnConnected();
        }

        private void HandleDisconnected()
        {
            OnDisconnected();
        }

        private void HandleError(string message)
        {
            OnError(message);
        }
        private void HandleMessageReceived(MessageFormat format, ushort messageId, object message)
        {
            OnMessageReceived(format, messageId, message);
        }


        
        protected virtual void RegisterDefaultProtocol() { }

        protected virtual BinaryMessageRegistry CreateBinaryRegistry() { return new BinaryMessageRegistry(); }

        protected virtual void OnConnected() { }
        protected virtual void OnDisconnected() { }
        protected virtual void OnError(string message) { }

        protected virtual void OnMessageReceived(MessageFormat format, ushort messageId, object message) { }



        protected virtual void BindMessageEvents() { }
        protected virtual void UnbindMessageEvents() { } 
    }
}