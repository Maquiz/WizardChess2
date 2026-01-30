using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Utility class for detecting the current platform type.
/// Used to decide which input service and UI elements to use.
/// </summary>
public static class PlatformDetector
{
    private static bool? _isMobileCached;

    /// <summary>
    /// True if running on a mobile platform (Android, iOS).
    /// </summary>
    public static bool IsMobile
    {
        get
        {
            if (!_isMobileCached.HasValue)
            {
                _isMobileCached = CheckIsMobile();
            }
            return _isMobileCached.Value;
        }
    }

    /// <summary>
    /// True if running on a desktop platform (Windows, Mac, Linux).
    /// </summary>
    public static bool IsDesktop => !IsMobile;

    /// <summary>
    /// True if running in the Unity Editor.
    /// </summary>
    public static bool IsEditor => Application.isEditor;

    /// <summary>
    /// True if the device has a touchscreen.
    /// Note: Some desktop devices also have touchscreens.
    /// Uses New Input System's Touchscreen.current for detection.
    /// </summary>
    public static bool HasTouchscreen => Touchscreen.current != null;

    private static bool CheckIsMobile()
    {
#if UNITY_ANDROID || UNITY_IOS
        return true;
#elif UNITY_EDITOR
        // In Editor, can simulate mobile via EditorUserBuildSettings or a manual flag
        // For now, default to desktop in Editor unless explicitly overridden
        return _forceMobileInEditor;
#else
        return Application.platform == RuntimePlatform.Android ||
               Application.platform == RuntimePlatform.IPhonePlayer;
#endif
    }

    // ========== Editor Testing Support ==========

    private static bool _forceMobileInEditor = false;

    /// <summary>
    /// Force mobile mode in the Editor for testing touch controls.
    /// Only affects Editor builds; ignored in standalone builds.
    /// </summary>
    public static void SetMobileInEditor(bool isMobile)
    {
#if UNITY_EDITOR
        _forceMobileInEditor = isMobile;
        _isMobileCached = null; // Clear cache to re-evaluate
        Debug.Log("[PlatformDetector] Editor mobile mode set to: " + isMobile);
#endif
    }

    /// <summary>
    /// Reset the cached platform detection.
    /// Useful after changing platform simulation settings.
    /// </summary>
    public static void ResetCache()
    {
        _isMobileCached = null;
    }
}
