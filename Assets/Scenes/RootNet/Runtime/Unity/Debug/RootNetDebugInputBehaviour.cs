using RootNet.Messages;
using UnityEngine;

namespace RootNet.Unity.Debugging
{
    public sealed class RootNetDebugInputBehaviour : MonoBehaviour
    {
        [SerializeField] private RootNetClientBehaviour clientBehaviour;
        [SerializeField] private KeyCode sendMoveKey = KeyCode.Space;

        private ushort _tick = 0;

        private void Update()
        {
            if (clientBehaviour == null || !clientBehaviour.IsConnected)
                return;

            if (Input.GetKeyDown(sendMoveKey))
            {
                MoveInputMessage msg;
                msg.Tick = _tick++;
                msg.MoveX = 1.0f;
                msg.MoveY = 0.0f;
                msg.Buttons = 1;

                clientBehaviour.SendBinary(msg);
            }
        }
    }
}