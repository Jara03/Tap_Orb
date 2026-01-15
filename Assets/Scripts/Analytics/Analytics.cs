using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public static class Analytics
{
    public static bool Enabled { get; set; } = Debug.isDebugBuild;

    public static string SessionId
    {
        get
        {
            EnsureInitialized();
            return sessionId;
        }
    }

    public static int RunIndexInSession
    {
        get
        {
            EnsureInitialized();
            return runIndexInSession;
        }
    }

    private static bool initialized;
    private static string sessionId;
    private static int runIndexInSession;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnLoad()
    {
        EnsureInitialized();
        Track("session_start");
    }

    public static void NotifyRunStarted()
    {
        EnsureInitialized();
        runIndexInSession++;
    }

    public static void Track(string eventName, Dictionary<string, object> props = null)
    {
        if (!Enabled)
        {
            return;
        }

        EnsureInitialized();

        Dictionary<string, object> mergedProps = props != null
            ? new Dictionary<string, object>(props)
            : new Dictionary<string, object>();

        mergedProps["session_id"] = sessionId;
        mergedProps["run_index"] = runIndexInSession;

        Debug.Log(FormatLogLine(eventName, mergedProps));

        // TODO: SendToProvider(eventName, mergedProps);
    }

    private static void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        sessionId = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{UnityEngine.Random.Range(100000, 999999)}";
        runIndexInSession = 0;
        initialized = true;
    }

    private static string FormatLogLine(string eventName, Dictionary<string, object> props)
    {
        var builder = new StringBuilder(128);
        builder.Append("[ANALYTICS] ").Append(eventName);

        foreach (KeyValuePair<string, object> kvp in props)
        {
            builder.Append(' ');
            builder.Append(kvp.Key);
            builder.Append('=');
            builder.Append(FormatValue(kvp.Value));
        }

        return builder.ToString();
    }

    private static string FormatValue(object value)
    {
        if (value == null)
        {
            return "null";
        }

        return value switch
        {
            float f => f.ToString("0.###", CultureInfo.InvariantCulture),
            double d => d.ToString("0.###", CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }
}
