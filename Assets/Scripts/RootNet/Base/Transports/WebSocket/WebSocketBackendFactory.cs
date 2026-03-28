namespace RootNet.Transports
{
    public static class WebSocketBackendFactory
    {
        public static IWebSocketClientBackend CreateDefault()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new WebGLWebSocketBackend();
#elif UNITY_2021_3_OR_NEWER || UNITY_EDITOR || UNITY_STANDALONE
            return new NativeWebSocketBackend();
#else
            return new UnsupportedWebSocketBackend();
#endif
        }
    }
}