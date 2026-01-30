using UnityEngine;

/// <summary>
/// Handles safe area adjustments for devices with notches, rounded corners, etc.
/// Attach to any UI RectTransform that should respect the safe area.
/// Typically attached to the root canvas or main UI container.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaHandler : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private ScreenOrientation lastOrientation;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void Update()
    {
        // Check if safe area has changed (orientation change, etc.)
        if (lastSafeArea != Screen.safeArea ||
            lastScreenSize.x != Screen.width ||
            lastScreenSize.y != Screen.height ||
            lastOrientation != Screen.orientation)
        {
            ApplySafeArea();
        }
    }

    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;

        // Cache current values
        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        // On desktop or if safe area matches screen, no adjustment needed
        if (!PlatformDetector.IsMobile)
        {
            return;
        }

        if (safeArea.width <= 0 || safeArea.height <= 0)
        {
            return;
        }

        // Convert safe area from screen space to anchor coordinates
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // Apply to RectTransform
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        Debug.Log("[SafeAreaHandler] Applied safe area: " + safeArea +
                  " (anchors: " + anchorMin + " to " + anchorMax + ")");
    }

    /// <summary>
    /// Force a safe area recalculation. Call after orientation changes.
    /// </summary>
    public void Refresh()
    {
        ApplySafeArea();
    }

    /// <summary>
    /// Get the current safe area rect.
    /// </summary>
    public static Rect GetSafeArea()
    {
        return Screen.safeArea;
    }

    /// <summary>
    /// Check if the current device has a notch or non-standard safe area.
    /// </summary>
    public static bool HasNotch()
    {
        Rect safeArea = Screen.safeArea;
        return safeArea.x > 0 ||
               safeArea.y > 0 ||
               safeArea.width < Screen.width ||
               safeArea.height < Screen.height;
    }
}
