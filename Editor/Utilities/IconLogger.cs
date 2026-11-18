using UnityEngine;

namespace AndroidIconGenerator
{
    public enum Severity { Success = 0, Warning = 1, Error = 2 }

    /// <summary>Console logging with a consistent prefix and the three severity levels.</summary>
    public static class IconLogger
    {
        private const string Prefix = "[Android Icon Generator] ";

        public static void Success(string message) { Debug.Log(Prefix + message); }
        public static void Warning(string message) { Debug.LogWarning(Prefix + message); }
        public static void Error(string message) { Debug.LogError(Prefix + message); }

        public static void Log(Severity severity, string message)
        {
            switch (severity)
            {
                case Severity.Error: Error(message); break;
                case Severity.Warning: Warning(message); break;
                default: Success(message); break;
            }
        }
    }
}
