using UnityEngine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Handles camera movement between different viewpoints.
/// Input: Keyboard 1/2/3 on desktop, on-screen buttons on mobile.
/// Camera switching is disabled in online matches (each player sees their own perspective).
/// Adjusts camera position for portrait vs landscape aspect ratios.
/// </summary>
public class CameraMove : MonoBehaviour
{
    public Transform C; // Camera transform

    private IInputService inputService;
    private Camera cam;
    private int currentView = 1; // Track current view for aspect ratio changes

    // Portrait mode requires camera further back to fit the board
    private bool IsPortrait => Screen.height > Screen.width;
    private float lastAspect;

    void Start()
    {
        // Initialize DOTween (needs to be done only once).
        DOTween.Init(false, true, LogBehaviour.ErrorsOnly);

        cam = C.GetComponent<Camera>();
        lastAspect = (float)Screen.width / Screen.height;

        // Subscribe to camera view events from input service
        inputService = InputServiceLocator.Current;
        if (inputService != null)
        {
            inputService.OnCameraViewRequested += OnCameraViewRequested;
        }

        // Auto-switch to top-down view on portrait devices for better board visibility
        if (IsPortrait && !MatchConfig.isOnlineMatch)
        {
            currentView = 3;
            SetTopPosition(false);
        }
    }

    void Update()
    {
        // Check for aspect ratio changes (e.g., device rotation)
        float currentAspect = (float)Screen.width / Screen.height;
        if (Mathf.Abs(currentAspect - lastAspect) > 0.1f)
        {
            lastAspect = currentAspect;
            RefreshCameraPosition();
        }
    }

    /// <summary>
    /// Refresh camera position for current aspect ratio without animation.
    /// </summary>
    private void RefreshCameraPosition()
    {
        switch (currentView)
        {
            case 1: SetPlayer1Position(false); break;
            case 2: SetPlayer2Position(false); break;
            case 3: SetTopPosition(false); break;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from input events
        if (inputService != null)
        {
            inputService.OnCameraViewRequested -= OnCameraViewRequested;
        }
    }

    /// <summary>
    /// Handle camera view request from input service.
    /// </summary>
    private void OnCameraViewRequested(int viewIndex)
    {
        // Block camera switching in online matches
        if (MatchConfig.isOnlineMatch) return;

        switch (viewIndex)
        {
            case 1:
                Player1Move();
                break;
            case 2:
                Player2Move();
                break;
            case 3:
                TopMove();
                break;
        }
    }

    /// <summary>
    /// Move camera to White player's perspective.
    /// </summary>
    public void Player1Move()
    {
        if (MatchConfig.isOnlineMatch) return;
        currentView = 1;
        SetPlayer1Position(true);
    }

    private void SetPlayer1Position(bool animate)
    {
        // Portrait mode: move camera much further back and up to fit the board width
        Vector3 targetPos = IsPortrait
            ? new Vector3(-3.5f, 10, -14)  // Much further back for portrait
            : new Vector3(-4, 4, -8);       // Original landscape position

        if (animate)
        {
            C.DOMove(targetPos, 1);
        }
        else
        {
            C.position = targetPos;
        }
        C.transform.localEulerAngles = new Vector3(IsPortrait ? 25f : 0f, 0, 0);

        // Ensure perspective mode for this view
        if (cam != null)
        {
            cam.orthographic = false;
            cam.fieldOfView = IsPortrait ? 70f : 60f;
        }
    }

    /// <summary>
    /// Move camera to Black player's perspective.
    /// </summary>
    public void Player2Move()
    {
        if (MatchConfig.isOnlineMatch) return;
        currentView = 2;
        SetPlayer2Position(true);
    }

    private void SetPlayer2Position(bool animate)
    {
        // Portrait mode: move camera much further back and up to fit the board width
        Vector3 targetPos = IsPortrait
            ? new Vector3(-3.5f, 10, 21)  // Much further back for portrait
            : new Vector3(-6, 7, 25);      // Original landscape position

        if (animate)
        {
            C.DOMove(targetPos, 1);
        }
        else
        {
            C.position = targetPos;
        }
        C.transform.localEulerAngles = new Vector3(IsPortrait ? 25f : 0f, 180, 0);

        // Ensure perspective mode for this view
        if (cam != null)
        {
            cam.orthographic = false;
            cam.fieldOfView = IsPortrait ? 70f : 60f;
        }
    }

    /// <summary>
    /// Move camera to top-down perspective.
    /// </summary>
    public void TopMove()
    {
        if (MatchConfig.isOnlineMatch) return;
        currentView = 3;
        SetTopPosition(true);
    }

    private void SetTopPosition(bool animate)
    {
        // Portrait mode: use orthographic camera with size 7, centered on board
        Vector3 targetPos = IsPortrait
            ? new Vector3(-3.5f, 15, 3.5f)  // Centered on board for portrait
            : new Vector3(-7, 12, 7);        // Original landscape position

        if (animate)
        {
            C.DOMove(targetPos, 1);
        }
        else
        {
            C.position = targetPos;
        }
        C.transform.localEulerAngles = new Vector3(90, 0, 0);

        // Use orthographic camera in portrait mode for clean top-down view
        if (cam != null)
        {
            if (IsPortrait)
            {
                cam.orthographic = true;
                cam.orthographicSize = 7f;
            }
            else
            {
                cam.orthographic = false;
                cam.fieldOfView = 60f;
            }
        }
    }
}
