using UnityEngine;
using System;
using System.Threading.Tasks;
using RootNet;
using RootNet.Messages;
using RootNet.Transports;
using RootNet.Unity;




namespace CCGF
{

    #region Messages
    [Serializable]
    public class HelloRequest : ISystemMessage
    {
        public ushort MessageId => 1;

        public string ClientVersion;
        public string Token;
    }

    [Serializable]
    public class HelloResponse : ISystemMessage
    {
        public ushort MessageId => 2;

        public bool Success;
        public string Message;
    }

    [Serializable]
    public class PingMessage : ISystemMessage
    {
        public ushort MessageId => 3;

        public long ClientTimeMs;
    }

    [Serializable]
    public class PongMessage : ISystemMessage
    {
        public ushort MessageId => 4;

        public long ClientTimeMs;
        public long ServerTimeMs;
    }
    #endregion



    public class CCGRootNetClientBehaviour : RootNetClientBehaviour
    {
        [Header("System Hello")]
        [SerializeField] private bool sendHelloOnConnected = true;
        [SerializeField] private string clientVersion = "0.1.0";
        [SerializeField] private string guestToken = "guest-token";

        private bool _handlersRegistered;

        protected override void RegisterDefaultProtocol()
        {
            SystemMessageRegistry.Clear();

            SystemMessageRegistry.Register<HelloRequest>(1);
            SystemMessageRegistry.Register<HelloResponse>(2);
            SystemMessageRegistry.Register<PingMessage>(3);
            SystemMessageRegistry.Register<PongMessage>(4);
        }
        
        protected override BinaryMessageRegistry CreateBinaryRegistry()
        {
            var binaryRegistry = new BinaryMessageRegistry();
            binaryRegistry.Register(1001, new MoveInputMessageCodec());
            return binaryRegistry;
        }

        protected override void OnConnected() 
        {
            if (sendHelloOnConnected)
            {
                SendSystem(new HelloRequest
                {
                    ClientVersion = clientVersion,
                    Token = guestToken
                });
            }
        }

        protected override void OnDisconnected() 
        { 
        }

        protected override void OnError(string message) 
        { 
        }
        protected override void OnMessageReceived(MessageFormat format, ushort messageId, object message) 
        {
        }


        protected override void BindMessageEvents() 
        {
            if (!_handlersRegistered)
            {
                RegisterHandler<HelloResponse>(2, OnHelloResponse);
                RegisterHandler<PongMessage>(4, OnPong);
                _handlersRegistered = true;
            }
        }

        protected override void UnbindMessageEvents() 
        { 
        }

        void OnHelloResponse(HelloResponse msg)
        {
            Debug.Log($"[RootNetMessage] HelloResponse success={msg.Success}, message={msg.Message}");
        }

        void OnPong(PongMessage msg)
        {
            Debug.Log($"[RootNetMessage] Pong clientTime={msg.ClientTimeMs}, serverTime={msg.ServerTimeMs}");
        }

    }


} 