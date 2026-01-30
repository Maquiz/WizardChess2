using UnityEngine;

/// <summary>
/// Service locator for the input system. Provides a single access point for input services
/// and handles platform detection to select the correct implementation.
/// </summary>
public static class InputServiceLocator
{
    private static IInputService _current;
    private static bool _initialized;

    /// <summary>
    /// The current input service instance. Automatically initializes on first access.
    /// </summary>
    public static IInputService Current
    {
        get
        {
            if (!_initialized)
            {
                Initialize();
            }
            return _current;
        }
    }

    /// <summary>
    /// Initialize the input service based on current platform.
    /// Called automatically on first access, but can be called manually for explicit control.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        // Determine which input service to use based on platform
        if (PlatformDetector.IsMobile)
        {
            _current = new TouchInputService();
            Debug.Log("[InputServiceLocator] Initialized TouchInputService for mobile platform");
        }
        else
        {
            _current = new DesktopInputService();
            Debug.Log("[InputServiceLocator] Initialized DesktopInputService for desktop platform");
        }

        _current.Initialize();
        _initialized = true;
    }

    /// <summary>
    /// Force a specific input service (useful for testing or manual override).
    /// </summary>
    public static void SetService(IInputService service)
    {
        if (_current != null)
        {
            _current.Dispose();
        }

        _current = service;
        _current.Initialize();
        _initialized = true;
        Debug.Log("[InputServiceLocator] Input service manually set to: " + service.GetType().Name);
    }

    /// <summary>
    /// Reset the input service (useful for testing or scene changes).
    /// </summary>
    public static void Reset()
    {
        if (_current != null)
        {
            _current.Dispose();
            _current = null;
        }
        _initialized = false;
    }

    /// <summary>
    /// Update the input service. Should be called every frame from a MonoBehaviour.
    /// </summary>
    public static void UpdateInput()
    {
        if (_initialized && _current != null)
        {
            _current.Update();
        }
    }
}
