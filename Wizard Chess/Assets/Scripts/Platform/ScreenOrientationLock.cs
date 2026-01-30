using UnityEngine;

/// <summary>
/// Locks screen orientation to landscape on mobile devices.
/// Attach to a persistent GameObject or call Initialize() manually.
/// </summary>
public class ScreenOrientationLock : MonoBehaviour
{
    private static bool _initialized;

    void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// Lock orientation to landscape. Safe to call multiple times.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        if (PlatformDetector.IsMobile)
        {
            // Allow both landscape orientations, but no portrait
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.orientation = ScreenOrientation.AutoRotation;

            Debug.Log("[ScreenOrientationLock] Locked to landscape orientations");
        }
    }

    /// <summary>
    /// Force a specific landscape orientation.
    /// </summary>
    public static void ForceLandscapeLeft()
    {
        if (PlatformDetector.IsMobile)
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }
    }

    /// <summary>
    /// Force a specific landscape orientation.
    /// </summary>
    public static void ForceLandscapeRight()
    {
        if (PlatformDetector.IsMobile)
        {
            Screen.orientation = ScreenOrientation.LandscapeRight;
        }
    }

    /// <summary>
    /// Return to auto-rotation (landscape only).
    /// </summary>
    public static void AllowAutoRotation()
    {
        if (PlatformDetector.IsMobile)
        {
            Screen.orientation = ScreenOrientation.AutoRotation;
        }
    }
}
