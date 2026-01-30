using UnityEngine;

/// <summary>
/// Provides haptic feedback (vibration) for mobile devices.
/// Respects user preference setting to enable/disable vibration.
/// </summary>
public static class HapticFeedback
{
    private const string PREFS_KEY = "HapticFeedbackEnabled";
    private static bool? _enabledCached;

    /// <summary>
    /// Whether haptic feedback is enabled. Persists across sessions.
    /// </summary>
    public static bool Enabled
    {
        get
        {
            if (!_enabledCached.HasValue)
            {
                _enabledCached = PlayerPrefs.GetInt(PREFS_KEY, 1) == 1;
            }
            return _enabledCached.Value;
        }
        set
        {
            _enabledCached = value;
            PlayerPrefs.SetInt(PREFS_KEY, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Trigger a short vibration. Only works on mobile and when enabled.
    /// </summary>
    public static void Vibrate()
    {
        if (!Enabled) return;
        if (!PlatformDetector.IsMobile) return;

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// Trigger a light vibration for UI feedback.
    /// Uses the same Handheld.Vibrate() - for more control, use a native plugin.
    /// </summary>
    public static void LightVibrate()
    {
        // Unity's Handheld.Vibrate() is a fixed duration.
        // For different vibration patterns, you'd need a native Android/iOS plugin.
        // For now, use the same vibration.
        Vibrate();
    }

    /// <summary>
    /// Trigger vibration for a selection event.
    /// </summary>
    public static void SelectionVibrate()
    {
        Vibrate();
    }

    /// <summary>
    /// Trigger vibration for a successful action.
    /// </summary>
    public static void SuccessVibrate()
    {
        Vibrate();
    }

    /// <summary>
    /// Trigger vibration for an error or invalid action.
    /// </summary>
    public static void ErrorVibrate()
    {
        Vibrate();
    }

    /// <summary>
    /// Reset cached preference (useful for testing).
    /// </summary>
    public static void ResetCache()
    {
        _enabledCached = null;
    }
}
