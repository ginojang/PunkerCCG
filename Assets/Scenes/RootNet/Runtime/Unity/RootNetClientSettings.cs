using UnityEngine;

namespace RootNet.Unity
{
    [CreateAssetMenu(
        fileName = "RootNetClientSettings",
        menuName = "RootNet/Client Settings")]
    public sealed class RootNetClientSettings : ScriptableObject
    {
        [Header("Connection")]
        public string serverAddress = "ws://127.0.0.1:8765";

        [Header("Timeout")]
        public bool useTimeout = true;
        public float timeoutSeconds = 15f;

        [Header("System")]
        public bool sendHelloOnConnected = true;
        public string clientVersion = "0.1.0";
        public string guestToken = "guest-token";
    }
}