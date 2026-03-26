using System;
using System.Text;
using UnityEngine;

namespace RootNet
{
    public sealed class JsonSystemMessageSerializer : ISystemMessageSerializer
    {
        public ArraySegment<byte> Serialize<T>(T message) where T : ISystemMessage
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            string json = JsonUtility.ToJson(message);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            return new ArraySegment<byte>(jsonBytes);
        }

        public bool TryDeserialize(ushort messageId, ArraySegment<byte> payload, out object message)
        {
            message = null;

            Type targetType = SystemMessageRegistry.Get(messageId);
            if (targetType == null)
                return false;

            try
            {
                string json = Encoding.UTF8.GetString(payload.Array, payload.Offset, payload.Count);
                message = JsonUtility.FromJson(json, targetType);
                return message != null;
            }
            catch
            {
                return false;
            }
        }
    }
}