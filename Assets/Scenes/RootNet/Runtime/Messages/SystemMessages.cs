using System;

namespace RootNet.Messages
{
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
}