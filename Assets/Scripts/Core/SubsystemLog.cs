using UnityEngine;

namespace VLTK.Core
{
    public static class SubsystemLog
    {
        public static void Info(string subsystem, string message)
        {
            Debug.Log($"[{subsystem}] {message}");
        }

        public static void Warn(string subsystem, string message)
        {
            Debug.LogWarning($"[{subsystem}] {message}");
        }

        public static void Error(string subsystem, string message)
        {
            Debug.LogError($"[{subsystem}] {message}");
        }
    }
}
