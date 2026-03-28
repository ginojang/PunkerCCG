using System;
using System.Collections.Generic;

namespace RootNet
{
    public static class SystemMessageRegistry
    {
        private static readonly Dictionary<ushort, Type> _types = new Dictionary<ushort, Type>();

        public static void Register<T>(ushort messageId) where T : ISystemMessage
        {
            _types[messageId] = typeof(T);
        }

        public static Type Get(ushort messageId)
        {
            _types.TryGetValue(messageId, out Type type);
            return type;
        }

        public static void Clear()
        {
            _types.Clear();
        }
    }
}