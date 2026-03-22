using RootNet.Messages;
using RootNet.Transports;

namespace RootNet
{
    public static class RootNetBootstrap
    {
        public static NetClient CreateDefault(INetLogger logger = null)
        {
            logger ??= new UnityNetLogger();

            SystemMessageRegistry.Clear();
            SystemMessageRegistry.Register<HelloRequest>(1);
            SystemMessageRegistry.Register<HelloResponse>(2);
            SystemMessageRegistry.Register<PingMessage>(3);
            SystemMessageRegistry.Register<PongMessage>(4);

            var binaryRegistry = new BinaryMessageRegistry();
            binaryRegistry.Register(1001, new MoveInputMessageCodec());

            var serializer = new CompositeMessageSerializer(
                new JsonSystemMessageSerializer(),
                binaryRegistry);

            IWebSocketClientBackend backend = WebSocketBackendFactory.CreateDefault();
            IClientTransport transport = new WebSocketTransport(backend);

            var client = new NetClient(
                transport,
                serializer,
                new NetClientConfig
                {
                    TimeoutSeconds = 15f,
                    UseTimeout = true
                },
                logger);

            return client;
        }
    }
}