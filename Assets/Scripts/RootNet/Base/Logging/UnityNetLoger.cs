using UnityEngine;

namespace RootNet
{
    public sealed class UnityNetLogger : INetLogger
    {
        public void Info(string message) => Debug.Log($"[RootNet] {message}");
        public void Warning(string message) => Debug.LogWarning($"[RootNet] {message}");
        public void Error(string message) => Debug.LogError($"[RootNet] {message}");
    }
}