using HighElixir.Loggings;
using System;

namespace HighElixir.StateMachines
{
    [Flags]
    public enum LogLevel : uint
    {
        None = 0,
        // 一般的な情報
        Info = 1 << 0, // その他一般的な情報
        Register = 1 << 1, // ステート,遷移登録関連
        TransitionResult = 1 << 2, // 遷移結果関連
        Enter = 1 << 3, // ステート進入
        StateUpdate = 1 << 4, // ステート更新
        Exit = 1 << 5, // ステート退出
        MachineLifeCycle = 1 << 6, // ステートマシンのライフサイクル関連
        LazyResult = 1 << 7, // Lazy評価関連

        StateLifeCycle = Enter | StateUpdate | Exit,
        LifeCycle = StateLifeCycle | MachineLifeCycle | LazyResult,
        INFO = Info | Register | TransitionResult | LifeCycle,
        // 警告
        Warning = 1 << 21,
        OverrideWarning = 1 << 22, // ステート上書き警告

        WARN = Warning | OverrideWarning,

        // 重大なエラー
        Error = 1 << 41,
        Fatal = 1 << 42,

        ERROR = Error | Fatal,

        // 組み合わせ
        ALL = INFO | WARN | ERROR,
    }
    public static class LogLevelExtension
    {
        // Non-extension wrappers for the corrected type name.
        public static bool HasFlagFast(uint value, LogLevel flag) => LogLevelExtention.HasFlagFast(value, flag);
        public static bool HasAnyFlagFast(LogLevel value, LogLevel flag) => LogLevelExtention.HasAnyFlagFast(value, flag);
        public static bool HasFlagFast(LogLevel value, LogLevel flag) => LogLevelExtention.HasFlagFast(value, flag);
        public static bool HasAnyFlagFast(uint value, LogLevel flag) => LogLevelExtention.HasAnyFlagFast(value, flag);
        public static void Log(ILoggable logger, LogLevel level, string message) => LogLevelExtention.Log(logger, level, message);
        public static void Throw(ILoggable logger, LogLevel level, Exception exception) => LogLevelExtention.Throw(logger, level, exception);
    }

    [Obsolete("Use LogLevelExtension instead.")]
    public static class LogLevelExtention
    {
        public static bool HasFlagFast(this uint value, LogLevel flag)
        {
            return (value & (uint)flag) == (uint)flag;
        }

        public static bool HasAnyFlagFast(this LogLevel value, LogLevel flag)
        {
            return (value & flag) != 0;
        }
        public static bool HasFlagFast(this LogLevel value, LogLevel flag)
        {
            return (value & flag) == flag;
        }

        public static bool HasAnyFlagFast(this uint value, LogLevel flag)
        {
            return (value & (uint)flag) != 0;
        }

        public static void Log(this ILoggable logger, LogLevel level, string message)
        {
            if (logger.Logger == null || !logger.RequiredLoggerLevel.HasAnyFlagFast(level))
                return;

            if (level.HasAnyFlagFast(LogLevel.ERROR))
                logger.Logger.Error($"[StateMachine]{message}");
            else if (level.HasAnyFlagFast(LogLevel.WARN))
                logger.Logger.Warn($"[StateMachine]{message}");
            else if (level.HasAnyFlagFast(LogLevel.INFO))
                logger.Logger.Info($"[StateMachine]{message}");
        }


        public static void Throw(this ILoggable logger, LogLevel level, Exception exception)
        {
            if (logger.Logger == null || !logger.RequiredLoggerLevel.HasAnyFlagFast(level)) return;
            if (logger.RequiredLoggerLevel.HasAnyFlagFast(LogLevel.ERROR))
            {
                logger.Logger.Error(exception);
            }
        }

    }
}
