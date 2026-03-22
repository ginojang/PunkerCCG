using UnityEngine;

namespace RootNet
{
    public sealed class NetClientBehaviour : MonoBehaviour
    {
        public NetClient Client { get; private set; }

        public void Initialize(NetClient client)
        {
            Client = client;
        }

        private void Update()
        {
            if (Client == null)
                return;

            Client.EarlyUpdate(Time.deltaTime);
            Client.LateUpdate();
        }

        private void OnDestroy()
        {
            Client?.Shutdown();
        }
    }
}