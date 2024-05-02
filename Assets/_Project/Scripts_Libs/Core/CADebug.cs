using UnityEngine;
using System.Diagnostics;

namespace CookApps.TeamBattle
{
public static class CADebug
{
	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void DrawLine(Vector3 start, Vector3 end, Color color)
	{
		UnityEngine.Debug.DrawLine(start, end, color);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void Log(object message)
	{
		UnityEngine.Debug.Log(message);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void Log(object message, Object context)
	{
		UnityEngine.Debug.Log(message, context);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void LogFormat(string format, params object[] args)
	{
		UnityEngine.Debug.LogFormat(format, args);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void LogFormat(Object context, string format, params object[] args)
	{
		UnityEngine.Debug.LogFormat(context, format, args);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void LogWarning(object message)
	{
		UnityEngine.Debug.LogWarning(message);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void LogWarning(object message, Object context)
	{
		UnityEngine.Debug.LogWarning(message, context);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void LogWarningFormat(string format, params object[] args)
	{
		UnityEngine.Debug.LogWarningFormat(format, args);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void LogWarningFormat(Object context, string format, params object[] args)
	{
		UnityEngine.Debug.LogWarningFormat(context, format, args);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void LogError(object message)
	{
		UnityEngine.Debug.LogError(message);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void LogError(object message, Object context)
	{
		UnityEngine.Debug.LogError(message, context);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void LogErrorFormat(string format, params object[] args)
	{
		UnityEngine.Debug.LogErrorFormat(format, args);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void LogErrorFormat(Object context, string format, params object[] args)
	{
		UnityEngine.Debug.LogErrorFormat(context, format, args);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void LogException(System.Exception exception)
	{
		UnityEngine.Debug.LogException(exception);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void LogException(System.Exception exception, Object context)
	{
		UnityEngine.Debug.LogException(exception, context);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void Assert(bool condition, object message)
	{
		if (condition)
			return;
		UnityEngine.Debug.LogAssertion(message);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void LogAssertion(object message)
	{
		UnityEngine.Debug.LogAssertion(message);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void LogAssertion(object message, Object context)
	{
		UnityEngine.Debug.LogAssertion(message, context);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void LogAssertionFormat(string format, params object[] args)
	{
		UnityEngine.Debug.LogAssertionFormat(format, args);
	}

	[Conditional("ENABLE_LOG"), Conditional("UNITY_EDITOR")]
	public static void LogAssertionFormat(Object context, string format, params object[] args)
	{
		UnityEngine.Debug.LogAssertionFormat(context, format, args);
	}

	public static bool isDebugBuild { get { return UnityEngine.Debug.isDebugBuild; } }
}
}
