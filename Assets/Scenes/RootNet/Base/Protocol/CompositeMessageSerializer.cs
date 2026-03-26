using System;

namespace RootNet
{
    public sealed class CompositeMessageSerializer : IMessageSerializer
    {
        private readonly ISystemMessageSerializer _systemSerializer;
        private readonly BinaryMessageRegistry _binaryRegistry;

        public CompositeMessageSerializer(
            ISystemMessageSerializer systemSerializer,
            BinaryMessageRegistry binaryRegistry)
        {
            _systemSerializer = systemSerializer;
            _binaryRegistry = binaryRegistry;
        }

        public ArraySegment<byte> SerializeSystem<T>(T message) where T : ISystemMessage
        {
            ArraySegment<byte> payload = _systemSerializer.Serialize(message);
            return Wrap(MessageFormat.Json, message.MessageId, payload);
        }

        public ArraySegment<byte> SerializeBinary<T>(T message) where T : IBinaryMessage
        {
            if (!_binaryRegistry.TryWrite(message, out ArraySegment<byte> payload))
                throw new InvalidOperationException($"No binary writer registered for {typeof(T).Name}");

            return Wrap(MessageFormat.Binary, message.MessageId, payload);
        }

        public bool TryDeserialize(
            ArraySegment<byte> data,
            out MessageFormat format,
            out ushort messageId,
            out object message)
        {
            format = 0;
            messageId = 0;
            message = null;

            if (data.Array == null || data.Count < 3)
                return false;

            byte[] array = data.Array;
            int offset = data.Offset;

            format = (MessageFormat)array[offset];
            messageId = (ushort)(array[offset + 1] | (array[offset + 2] << 8));

            ArraySegment<byte> payload = new ArraySegment<byte>(array, offset + 3, data.Count - 3);

            switch (format)
            {
                case MessageFormat.Json:
                    return _systemSerializer.TryDeserialize(messageId, payload, out message);

                case MessageFormat.Binary:
                    return _binaryRegistry.TryRead(messageId, payload, out message);

                default:
                    return false;
            }
        }

        private static ArraySegment<byte> Wrap(MessageFormat format, ushort messageId, ArraySegment<byte> payload)
        {
            byte[] buffer = new byte[3 + payload.Count];
            buffer[0] = (byte)format;
            buffer[1] = (byte)(messageId & 0xFF);
            buffer[2] = (byte)((messageId >> 8) & 0xFF);

            if (payload.Count > 0)
            {
                Buffer.BlockCopy(payload.Array, payload.Offset, buffer, 3, payload.Count);
            }

            return new ArraySegment<byte>(buffer);
        }
    }
}