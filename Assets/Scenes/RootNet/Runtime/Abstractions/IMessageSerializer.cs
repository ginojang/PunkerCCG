using System;

namespace RootNet
{
    public interface IMessageSerializer
    {
        ArraySegment<byte> SerializeSystem<T>(T message) where T : ISystemMessage;
        ArraySegment<byte> SerializeBinary<T>(T message) where T : IBinaryMessage;

        bool TryDeserialize(
            ArraySegment<byte> data,
            out MessageFormat format,
            out ushort messageId,
            out object message);
    }
}