using System;

namespace RootNet
{
    public interface ISystemMessageSerializer
    {
        ArraySegment<byte> Serialize<T>(T message) where T : ISystemMessage;
        bool TryDeserialize(ushort messageId, ArraySegment<byte> payload, out object message);
    }
}