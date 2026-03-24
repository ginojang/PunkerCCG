using RootNet.Messages;
using UnityEngine;

namespace RootNet.Unity.Debugging
{
    public sealed class RootNetDebugLogBehaviour : MonoBehaviour
    {
        [SerializeField] private RootNetClientBehaviour clientBehaviour;

        private void Awake()
        {
            if (clientBehaviour == null)
                return;

            clientBehaviour.Connected += OnConnected;
            clientBehaviour.Disconnected += OnDisconnected;
            clientBehaviour.Error += OnError;

            clientBehaviour.RegisterHandler<HelloResponse>(2, OnHelloResponse);
            clientBehaviour.RegisterHandler<PongMessage>(4, OnPong);
        }

        private void OnDestroy()
        {
            if (clientBehaviour == null)
                return;

            clientBehaviour.Connected -= OnConnected;
            clientBehaviour.Disconnected -= OnDisconnected;
            clientBehaviour.Error -= OnError;
        }

        private void OnConnected()
        {
            Debug.Log("[RootNet] Connected");
        }

        private void OnDisconnected()
        {
            Debug.Log("[RootNet] Disconnected");
        }

        private void OnError(string msg)
        {
            Debug.LogError($"[RootNet] {msg}");
        }

        private void OnHelloResponse(HelloResponse msg)
        {
            Debug.Log($"[RootNet] HelloResponse success={msg.Success}, message={msg.Message}");
        }

        private void OnPong(PongMessage msg)
        {
            Debug.Log($"[RootNet] Pong {msg.ServerTimeMs}");
        }
    }
}