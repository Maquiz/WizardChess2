using UnityEngine;

/// <summary>
/// MonoBehaviour that updates the InputServiceLocator each frame.
/// Attach to a persistent GameObject in the scene (e.g., GameMaster or a dedicated InputManager).
/// Alternatively, GameMaster can call InputServiceLocator.UpdateInput() directly.
/// </summary>
public class InputServiceUpdater : MonoBehaviour
{
    private static InputServiceUpdater _instance;

    void Awake()
    {
        // Ensure only one instance exists
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    void Update()
    {
        InputServiceLocator.UpdateInput();
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
