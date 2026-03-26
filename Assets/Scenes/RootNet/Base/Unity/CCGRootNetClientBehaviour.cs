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
                Send(new HelloRequest
                {
                    ClientVersion = clientVersion,
                    Token = guestToken
                });
            }
        }

        protected override void OnDisconnected() 
        { 
        }
    }


} 