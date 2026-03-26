using System;
using System.Collections.Generic;

namespace RootNet
{
    public sealed class BinaryMessageRegistry
    {
        private readonly Dictionary<ushort, Func<ArraySegment<byte>, object>> _readers =
            new Dictionary<ushort, Func<ArraySegment<byte>, object>>();

        private readonly Dictionary<Type, Func<object, ArraySegment<byte>>> _writers =
            new Dictionary<Type, Func<object, ArraySegment<byte>>>();

        public void Register<T>(ushort messageId, IBinaryMessageCodec<T> codec)
            where T : IBinaryMessage
        {
            _readers[messageId] = (payload) =>
            {
                var reader = new NetReader(payload);
                return codec.Read(ref reader);
            };

            _writers[typeof(T)] = (obj) =>
            {
                var writer = new NetWriter(64);
                codec.Write(ref writer, (T)obj);
                return writer.ToArraySegment();
            };
        }

        public bool TryWrite(object message, out ArraySegment<byte> payload)
        {
            payload = default;

            if (message == null)
                return false;

            if (_writers.TryGetValue(message.GetType(), out var writer))
            {
                payload = writer(message);
                return true;
            }

            return false;
        }

        public bool TryRead(ushort messageId, ArraySegment<byte> payload, out object message)
        {
            message = null;

            if (_readers.TryGetValue(messageId, out var reader))
            {
                message = reader(payload);
                return true;
            }

            return false;
        }
    }
}