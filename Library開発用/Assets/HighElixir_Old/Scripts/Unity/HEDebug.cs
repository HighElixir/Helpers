using System.Runtime.CompilerServices;

namespace HighElixir.Unity
{
    public enum StringColor
    {
        red,
        green,
        blue,
        yellow,
        cyan,
        magenta,
        white,
    }

    public static class HEDebug
    {

        public static void Log(string message, StringColor col = StringColor.white, [CallerMemberName] string caller = "")
        {
            UnityEngine.Debug.Log($"[{caller.ColorText(col)}]{message}");
        }

        public static void Warn(string message, StringColor col = StringColor.white, [CallerMemberName] string caller = "")
        {
            UnityEngine.Debug.LogWarning($"[{caller.ColorText(col)}]{message}");
        }

        public static void Error(string message, StringColor col = StringColor.white, [CallerMemberName] string caller = "")
        {
            UnityEngine.Debug.LogError($"[{caller.ColorText(col)}]{message}");
        }

        // コンディションがfalseの場合にエラーログを出力し、falseを返す
        public static bool Assert(bool condition, string message, StringColor col = StringColor.white, [CallerMemberName] string caller = "")
        {
            if (!condition)
            {
                UnityEngine.Debug.LogError($"[{caller.ColorText(col)}]{message}");
                return false;
            }
            return true;
        }

        public static string ColorText(this string message, StringColor col)
        {
            return $"<color={col}>{message}</color>";
        }
    }
}