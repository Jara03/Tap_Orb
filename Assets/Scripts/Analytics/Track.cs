using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
#if FIREBASE_ANALYTICS
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
#endif
#if FIREBASE_CRASHLYTICS
using Firebase.Crashlytics;
#endif

public static class Track
{
    private const int MaxParameterCount = 10;
    private const int MaxStringLength = 100;

    private static readonly HashSet<string> BlockedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "email",
        "e_mail",
        "device_id",
        "deviceid",
        "idfa",
        "idfv",
        "advertising_id",
        "gaid",
        "gps_adid"
    };

    public static bool RuntimeEnabled { get; set; } = true;

    private static bool initialized;
    private static bool firebaseReady;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitOnLoad()
    {
        Init();
    }

    public static void Init()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        if (!IsTrackingEnabled)
        {
            DebugLog("Tracking disabled (TRACKING_ENABLED not set or RuntimeEnabled=false).");
            return;
        }

        WarnIfMissingConfigFiles();

#if FIREBASE_ANALYTICS
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result != DependencyStatus.Available)
            {
                Debug.LogWarning($"[Track] Firebase dependencies unresolved: {task.Result}. Analytics disabled.");
                return;
            }

            firebaseReady = true;
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
#if FIREBASE_CRASHLYTICS
            Crashlytics.IsCrashlyticsCollectionEnabled = true;
#endif
            DebugLog("Firebase Analytics ready.");
        });
#else
        DebugLog("Firebase Analytics SDK not present. Events will log to console only.");
#endif
    }

    public static void Event(string name, IDictionary<string, object> parameters = null)
    {
        if (!IsTrackingEnabled)
        {
            return;
        }

        Init();

        string eventName = NormalizeEventName(name);
        if (string.IsNullOrWhiteSpace(eventName))
        {
            return;
        }

        Dictionary<string, object> sanitized = SanitizeParameters(parameters);
        DebugLog(FormatLogLine(eventName, sanitized));

#if FIREBASE_ANALYTICS
        if (!firebaseReady)
        {
            return;
        }

        if (sanitized == null || sanitized.Count == 0)
        {
            FirebaseAnalytics.LogEvent(eventName);
            return;
        }

        FirebaseAnalytics.LogEvent(eventName, BuildFirebaseParameters(sanitized));
#endif
    }

    public static void LevelStart(int level)
    {
        Event("level_start", new Dictionary<string, object>
        {
            { "level", level }
        });
    }

    public static void LevelComplete(int level)
    {
        Event("level_complete", new Dictionary<string, object>
        {
            { "level", level }
        });
    }

    public static void LevelComplete(int level, float durationSeconds)
    {
        Event("level_complete", new Dictionary<string, object>
        {
            { "level", level },
            { "duration_s", durationSeconds }
        });
    }

    public static void LevelFail(int level, string reason)
    {
        Event("level_fail", BuildReasonParams(level, reason));
    }

    public static void LevelFail(int level, string reason, float durationSeconds)
    {
        Dictionary<string, object> parameters = BuildReasonParams(level, reason);
        parameters["duration_s"] = durationSeconds;
        Event("level_fail", parameters);
    }

    public static void TutorialStart()
    {
        Event("tutorial_start");
    }

    public static void TutorialComplete()
    {
        Event("tutorial_complete");
    }

    public static void AdImpression(string adType, string placement)
    {
        Event("ad_impression", BuildAdParams(adType, placement));
    }

    public static void AdImpression(string adType, string placement, int? level)
    {
        Dictionary<string, object> parameters = BuildAdParams(adType, placement);
        if (level.HasValue)
        {
            parameters["level"] = level.Value;
        }

        Event("ad_impression", parameters);
    }

    private static bool IsTrackingEnabled
    {
        get
        {
#if TRACKING_ENABLED
            return RuntimeEnabled;
#else
            return false;
#endif
        }
    }

    private static Dictionary<string, object> BuildReasonParams(int level, string reason)
    {
        var parameters = new Dictionary<string, object>
        {
            { "level", level }
        };

        if (!string.IsNullOrWhiteSpace(reason))
        {
            parameters["reason"] = NormalizeEventName(reason);
        }

        return parameters;
    }

    private static Dictionary<string, object> BuildAdParams(string adType, string placement)
    {
        var parameters = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(adType))
        {
            parameters["ad_type"] = NormalizeEventName(adType);
        }

        if (!string.IsNullOrWhiteSpace(placement))
        {
            parameters["placement"] = NormalizeEventName(placement);
        }

        return parameters;
    }

    private static Dictionary<string, object> SanitizeParameters(IDictionary<string, object> parameters)
    {
        if (parameters == null || parameters.Count == 0)
        {
            return null;
        }

        var sanitized = new Dictionary<string, object>(parameters.Count);

        foreach (KeyValuePair<string, object> kvp in parameters)
        {
            if (sanitized.Count >= MaxParameterCount)
            {
                DebugLog($"Parameter limit reached ({MaxParameterCount}). Extra params dropped.");
                break;
            }

            if (string.IsNullOrWhiteSpace(kvp.Key))
            {
                continue;
            }

            string key = NormalizeEventName(kvp.Key);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (BlockedKeys.Contains(key))
            {
                continue;
            }

            object sanitizedValue = SanitizeValue(kvp.Value);
            if (sanitizedValue == null)
            {
                continue;
            }

            sanitized[key] = sanitizedValue;
        }

        return sanitized.Count > 0 ? sanitized : null;
    }

    private static object SanitizeValue(object value)
    {
        if (value == null)
        {
            return null;
        }

        switch (value)
        {
            case bool boolValue:
                return boolValue ? 1L : 0L;
            case byte byteValue:
                return (long)byteValue;
            case short shortValue:
                return (long)shortValue;
            case int intValue:
                return (long)intValue;
            case long longValue:
                return longValue;
            case float floatValue:
                return (double)floatValue;
            case double doubleValue:
                return doubleValue;
            case decimal decimalValue:
                return (double)decimalValue;
            case string stringValue:
                return TrimString(stringValue);
        }

        return TrimString(value.ToString());
    }

    private static string TrimString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();
        if (value.Length <= MaxStringLength)
        {
            return value;
        }

        return value.Substring(0, MaxStringLength);
    }

#if FIREBASE_ANALYTICS
    private static Parameter[] BuildFirebaseParameters(Dictionary<string, object> parameters)
    {
        var list = new List<Parameter>(parameters.Count);

        foreach (KeyValuePair<string, object> kvp in parameters)
        {
            switch (kvp.Value)
            {
                case long longValue:
                    list.Add(new Parameter(kvp.Key, longValue));
                    break;
                case double doubleValue:
                    list.Add(new Parameter(kvp.Key, doubleValue));
                    break;
                default:
                    list.Add(new Parameter(kvp.Key, kvp.Value.ToString()));
                    break;
            }
        }

        return list.ToArray();
    }
#endif

    private static string NormalizeEventName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(name.Length + 8);
        bool prevUnderscore = false;
        char previous = '\0';

        foreach (char ch in name.Trim())
        {
            if (!char.IsLetterOrDigit(ch))
            {
                if (!prevUnderscore && builder.Length > 0)
                {
                    builder.Append('_');
                    prevUnderscore = true;
                }

                previous = ch;
                continue;
            }

            if (char.IsUpper(ch) && builder.Length > 0 && char.IsLower(previous))
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(ch));
            prevUnderscore = false;
            previous = ch;
        }

        return builder.ToString().Trim('_');
    }

    private static string FormatLogLine(string eventName, Dictionary<string, object> props)
    {
        if (props == null || props.Count == 0)
        {
            return $"[Track] {eventName}";
        }

        var builder = new StringBuilder(128);
        builder.Append("[Track] ").Append(eventName);

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

    private static void WarnIfMissingConfigFiles()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        string assetsPath = Application.dataPath;
        string androidConfig = Path.Combine(assetsPath, "google-services.json");
        string iosConfig = Path.Combine(assetsPath, "GoogleService-Info.plist");

        if (!File.Exists(androidConfig))
        {
            Debug.LogWarning("[Track] TODO: Add Firebase Android config at Assets/google-services.json.");
        }

        if (!File.Exists(iosConfig))
        {
            Debug.LogWarning("[Track] TODO: Add Firebase iOS config at Assets/GoogleService-Info.plist.");
        }
#endif
    }

    private static void DebugLog(string message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Track] {message}");
#endif
    }
}
