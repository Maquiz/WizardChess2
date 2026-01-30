using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// Touch input service implementation for mobile devices.
/// Handles tap-to-select, tap-to-move, and long-press for ability activation.
/// Camera view and menu buttons are handled via UI (MobileCameraControls, InGameMenuUI).
/// Uses Unity's New Input System with EnhancedTouch for cross-platform compatibility.
/// </summary>
public class TouchInputService : IInputService
{
    // ========== Events ==========
    public event Action OnPrimaryAction;
    public event Action OnSecondaryAction;
    public event Action OnAbilityActivate;
    public event Action<int> OnCameraViewRequested;
    public event Action OnMenuToggle;

    // ========== Properties ==========
    public Vector3 PointerPosition { get; private set; }
    public bool IsPointerDown { get; private set; }
    public bool PrimaryActionThisFrame { get; private set; }
    public bool SecondaryActionThisFrame { get; private set; }

    // ========== Long Press Detection ==========
    private bool _touchStarted;
    private float _touchStartTime;
    private Vector2 _touchStartPosition;
    private bool _longPressTriggered;
    private const float LONG_PRESS_DURATION = 0.5f; // 500ms
    private const float LONG_PRESS_MAX_MOVE = 30f;  // Max pixels to move before cancelling long press

    // ========== Methods ==========

    public void Initialize()
    {
        PointerPosition = Vector3.zero;
        IsPointerDown = false;
        _touchStarted = false;
        _longPressTriggered = false;

        // Enable EnhancedTouch support for the New Input System
        if (!EnhancedTouchSupport.enabled)
        {
            EnhancedTouchSupport.Enable();
        }
    }

    public void Update()
    {
        // Reset frame-specific flags
        PrimaryActionThisFrame = false;
        SecondaryActionThisFrame = false;

        // Handle mouse input for Editor testing (simulates touch)
        if (Application.isEditor && !IsTouchActive())
        {
            UpdateMouseInput();
            return;
        }

        // Handle touch input
        UpdateTouchInput();
    }

    private bool IsTouchActive()
    {
        return Touch.activeTouches.Count > 0;
    }

    private void UpdateMouseInput()
    {
        // Safety check for mouse availability
        if (Mouse.current == null) return;

        // Use mouse for Editor testing
        PointerPosition = Mouse.current.position.ReadValue();
        IsPointerDown = Mouse.current.leftButton.isPressed;
        PrimaryActionThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
        SecondaryActionThisFrame = Mouse.current.rightButton.wasPressedThisFrame;

        // Simulate long press with mouse hold
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            _touchStarted = true;
            _touchStartTime = Time.time;
            _touchStartPosition = Mouse.current.position.ReadValue();
            _longPressTriggered = false;
        }
        else if (Mouse.current.leftButton.isPressed && _touchStarted && !_longPressTriggered)
        {
            float heldTime = Time.time - _touchStartTime;
            float moveDistance = Vector2.Distance(Mouse.current.position.ReadValue(), _touchStartPosition);

            if (heldTime >= LONG_PRESS_DURATION && moveDistance < LONG_PRESS_MAX_MOVE)
            {
                _longPressTriggered = true;
                OnAbilityActivate?.Invoke();
                HapticFeedback.Vibrate();
            }
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            // Only trigger tap if it wasn't a long press
            if (_touchStarted && !_longPressTriggered)
            {
                OnPrimaryAction?.Invoke();
            }
            _touchStarted = false;
            _longPressTriggered = false;
        }

        // Right click still cancels selection
        if (SecondaryActionThisFrame)
        {
            OnSecondaryAction?.Invoke();
        }

        // Escape for menu (Editor only)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OnMenuToggle?.Invoke();
        }
    }

    private void UpdateTouchInput()
    {
        if (Touch.activeTouches.Count == 0)
        {
            IsPointerDown = false;
            return;
        }

        Touch touch = Touch.activeTouches[0];
        PointerPosition = touch.screenPosition;
        IsPointerDown = touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved;

        switch (touch.phase)
        {
            case TouchPhase.Began:
                _touchStarted = true;
                _touchStartTime = Time.time;
                _touchStartPosition = touch.screenPosition;
                _longPressTriggered = false;
                break;

            case TouchPhase.Stationary:
            case TouchPhase.Moved:
                if (_touchStarted && !_longPressTriggered)
                {
                    float heldTime = Time.time - _touchStartTime;
                    float moveDistance = Vector2.Distance(touch.screenPosition, _touchStartPosition);

                    // Check for long press
                    if (heldTime >= LONG_PRESS_DURATION && moveDistance < LONG_PRESS_MAX_MOVE)
                    {
                        _longPressTriggered = true;
                        OnAbilityActivate?.Invoke();
                        HapticFeedback.Vibrate();
                    }
                }
                break;

            case TouchPhase.Ended:
                // Only trigger tap if it wasn't a long press
                if (_touchStarted && !_longPressTriggered)
                {
                    PrimaryActionThisFrame = true;
                    OnPrimaryAction?.Invoke();
                }
                _touchStarted = false;
                _longPressTriggered = false;
                break;

            case TouchPhase.Canceled:
                _touchStarted = false;
                _longPressTriggered = false;
                break;
        }
    }

    /// <summary>
    /// Trigger a camera view change. Called by MobileCameraControls UI buttons.
    /// </summary>
    public void TriggerCameraView(int viewIndex)
    {
        OnCameraViewRequested?.Invoke(viewIndex);
    }

    /// <summary>
    /// Trigger menu toggle. Called by mobile menu button.
    /// </summary>
    public void TriggerMenuToggle()
    {
        OnMenuToggle?.Invoke();
    }

    /// <summary>
    /// Trigger ability activation. Called by mobile ability button.
    /// </summary>
    public void TriggerAbilityActivate()
    {
        OnAbilityActivate?.Invoke();
        HapticFeedback.Vibrate();
    }

    public void Dispose()
    {
        OnPrimaryAction = null;
        OnSecondaryAction = null;
        OnAbilityActivate = null;
        OnCameraViewRequested = null;
        OnMenuToggle = null;

        // Note: EnhancedTouchSupport is typically left enabled for app lifetime
        // as it may be used by other systems. Only disable if this is the sole user.
    }
}
