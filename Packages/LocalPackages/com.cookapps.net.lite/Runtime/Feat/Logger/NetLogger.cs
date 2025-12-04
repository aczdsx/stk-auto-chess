/*
* Copyright (c) CookApps.
*/

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming
// ReSharper disable ArrangeAttributes

namespace CookApps.NetLite.Feat.Logger
{
    /// 로그 출력
    internal class NetLogger
    {
        private readonly ILogHandler logHandler = new LogHandler();
        private static readonly NetLogger _instance = new();

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReloadDomain()
        {
            _instance.logEnabled = true;
        }
#endif

        private bool logEnabled { get; set; } = true;
        private LogType filterLogType { get; set; } = LogType.Log;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsLogTypeAllowed(LogType logType)
        {
            if (logEnabled)
            {
                if (logType == LogType.Exception)
                    return true;
                if (filterLogType != LogType.Exception)
                    return logType <= filterLogType;
            }

            return false;
        }

        private static string GetString(object message)
        {
            if (message == null)
                return "Null";
            return message is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : message.ToString();
        }

        public static void SetLogEnabled(bool enabled)
        {
            _instance.logEnabled = enabled;
        }

        public static void SetFilterLogType(LogType logType)
        {
            _instance.filterLogType = logType;
        }

        public static TaggedLogger WithTag(string tag)
        {
            return new TaggedLogger(tag);
        }

        public static TaggedLogger For<T>()
        {
            return WithTag(typeof(T).Name);
        }

        public static TaggedLogger For(object owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            return WithTag(owner.GetType().Name);
        }

        // --------------------
        // Static 래퍼 메서드들
        // --------------------

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        public static void Log(LogType logType, object message)
        {
            _instance._Log(logType, message);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        public static void Log(LogType logType, object message, Object context)
        {
            _instance._Log(logType, message, context);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        public static void Log(LogType logType, string tag, object message)
        {
            _instance._Log(logType, tag, message);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        public static void Log(LogType logType, string tag, object message, Object context)
        {
            _instance._Log(logType, tag, message, context);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        public static void Log(object message)
        {
            _instance._Log(message);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        public static void Log(string tag, object message)
        {
            _instance._Log(tag, message);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        public static void Log(string tag, object message, Object context)
        {
            _instance._Log(tag, message, context);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        public static void LogWarning(string tag, object message)
        {
            _instance._LogWarning(tag, message);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        public static void LogWarning(string tag, object message, Object context)
        {
            _instance._LogWarning(tag, message, context);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        public static void LogError(string tag, object message)
        {
#if UNITY_INCLUDE_TESTS // 테스트 환경에서는 에러 로그를 경고로 변경
            LogWarning(tag, message);
#else
            _instance._LogError(tag, message);
#endif
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        public static void LogError(string tag, object message, Object context)
        {
#if UNITY_INCLUDE_TESTS // 테스트 환경에서는 에러 로그를 경고로 변경
            LogWarning(tag, message, context);
#else
            _instance._LogError(tag, message, context);
#endif
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        public static void LogException(Exception exception)
        {
            _instance._LogException(exception);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        public static void LogException(Exception exception, Object context)
        {
            _instance._LogException(exception, context);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        public static void LogFormat(LogType logType, string format, params object[] args)
        {
            _instance._LogFormat(logType, format, args);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        public static void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            _instance._LogFormat(logType, context, format, args);
        }

        // --------------------
        // 인스턴스 메서드 (접두사 _)
        // --------------------

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        private void _Log(LogType logType, object message)
        {
            if (!IsLogTypeAllowed(logType))
                return;
            logHandler.LogFormat(logType, null, "{0}", GetString(message));
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        private void _Log(LogType logType, object message, Object context)
        {
            if (!IsLogTypeAllowed(logType))
                return;
            logHandler.LogFormat(logType, context, "{0}", GetString(message));
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        private void _Log(LogType logType, string tag, object message)
        {
            if (!IsLogTypeAllowed(logType))
                return;
            logHandler.LogFormat(logType, null, "{0}: {1}", tag, GetString(message));
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        private void _Log(LogType logType, string tag, object message, Object context)
        {
            if (!IsLogTypeAllowed(logType))
                return;
            logHandler.LogFormat(logType, context, "{0}: {1}", tag, GetString(message));
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        private void _Log(object message)
        {
            if (!IsLogTypeAllowed(LogType.Log))
                return;
            logHandler.LogFormat(LogType.Log, null, "{0}", GetString(message));
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        private void _Log(string tag, object message)
        {
            if (!IsLogTypeAllowed(LogType.Log))
                return;
            logHandler.LogFormat(LogType.Log, null, "{0}: {1}", tag, GetString(message));
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        private void _Log(string tag, object message, Object context)
        {
            if (!IsLogTypeAllowed(LogType.Log))
                return;
            logHandler.LogFormat(LogType.Log, context, "{0}: {1}", tag, GetString(message));
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        private void _LogWarning(string tag, object message)
        {
            if (!IsLogTypeAllowed(LogType.Warning))
                return;
            logHandler.LogFormat(LogType.Warning, null, "{0}: {1}", tag, GetString(message));
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        private void _LogWarning(string tag, object message, Object context)
        {
            if (!IsLogTypeAllowed(LogType.Warning))
                return;
            logHandler.LogFormat(LogType.Warning, context, "{0}: {1}", tag, GetString(message));
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        private void _LogError(string tag, object message)
        {
            if (!IsLogTypeAllowed(LogType.Error))
                return;
            logHandler.LogFormat(LogType.Error, null, "{0}: {1}", tag, GetString(message));
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        private void _LogError(string tag, object message, Object context)
        {
            if (!IsLogTypeAllowed(LogType.Error))
                return;
            logHandler.LogFormat(LogType.Error, context, "{0}: {1}", tag, GetString(message));
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        private void _LogException(Exception exception)
        {
            if (!logEnabled)
                return;
            logHandler.LogException(exception, null);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        private void _LogException(Exception exception, Object context)
        {
            if (!logEnabled)
                return;
            logHandler.LogException(exception, context);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        private void _LogFormat(LogType logType, string format, params object[] args)
        {
            if (!IsLogTypeAllowed(logType))
                return;
            logHandler.LogFormat(logType, null, format, args);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
        private void _LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            if (!IsLogTypeAllowed(logType))
                return;
            logHandler.LogFormat(logType, context, format, args);
        }

        internal readonly struct TaggedLogger
        {
            private readonly string _tag;

            internal TaggedLogger(string tag)
            {
                _tag = string.IsNullOrEmpty(tag) ? string.Empty : tag;
            }

            [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
            public void Log(object message)
            {
                NetLogger.Log(_tag, message);
            }

            [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
            public void Log(object message, Object context)
            {
                NetLogger.Log(_tag, message, context);
            }

            [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
            public void LogWarning(object message)
            {
                NetLogger.LogWarning(_tag, message);
            }

            [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
            public void LogWarning(object message, Object context)
            {
                NetLogger.LogWarning(_tag, message, context);
            }

            [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
            public void LogError(object message)
            {
                NetLogger.LogError(_tag, message);
            }

            [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
            public void LogError(object message, Object context)
            {
                NetLogger.LogError(_tag, message, context);
            }

            [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("__DEV")]
            public void LogException(Exception exception)
            {
                NetLogger.LogError(_tag, exception);
            }
        }
    }

    internal sealed class LogHandler : ILogHandler
    {
        private const string Prefix = "[Net]";
        private readonly ILogHandler _defaultLogHandler = Debug.unityLogger.logHandler;
        private readonly StringBuilder _stringBuilder = new(256);
        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append(Prefix);
            if (args.Length > 0)
            {
                _stringBuilder.AppendFormat(format, args);
            }
            else
            {
                _stringBuilder.Append(format);
            }

            var message = _stringBuilder.ToString();
            _defaultLogHandler.LogFormat(logType, context, "{0}", message);
        }

        public void LogException(Exception exception, Object context)
        {
            _defaultLogHandler.LogException(exception, context);
        }
    }
}
