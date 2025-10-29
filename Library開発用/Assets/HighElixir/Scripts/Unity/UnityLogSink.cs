using UnityEngine;

namespace HighElixir.Unity.Loggings
{
    // ===== Unity出力 =====
    public sealed class UnityLogger : HighElixir.Loggings.ILogger
    {
        public void Error(object message)
        {
            Debug.LogError(message);
        }

        public void Info(object message)
        {
            Debug.Log(message);
        }

        public void Warn(object message)
        {
            Debug.LogWarning(message);
        }
    }
}
