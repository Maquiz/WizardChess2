using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// Desktop input service implementation using mouse and keyboard.
/// Uses Unity's New Input System for cross-platform compatibility.
/// </summary>
public class DesktopInputService : IInputService
{
    // ========== Events ==========
    public event Action OnPrimaryAction;
    public event Action OnSecondaryAction;
    public event Action OnAbilityActivate;
    public event Action<int> OnCameraViewRequested;
    public event Action OnMenuToggle;

    // ========== Properties ==========
    public Vector3 PointerPosition => Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
    public bool IsPointerDown => Mouse.current != null && Mouse.current.leftButton.isPressed;
    public bool PrimaryActionThisFrame { get; private set; }
    public bool SecondaryActionThisFrame { get; private set; }

    // ========== Methods ==========

    public void Initialize()
    {
        // New Input System is automatically initialized
    }

    public void Update()
    {
        // Safety check for available devices
        if (Mouse.current == null || Keyboard.current == null) return;

        // Track frame-specific actions
        PrimaryActionThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
        SecondaryActionThisFrame = Mouse.current.rightButton.wasPressedThisFrame;

        // Fire events for this frame's input
        if (PrimaryActionThisFrame)
        {
            OnPrimaryAction?.Invoke();
        }

        if (SecondaryActionThisFrame)
        {
            OnSecondaryAction?.Invoke();
        }

        // Ability activation (Q key)
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            OnAbilityActivate?.Invoke();
        }

        // Camera view keys (1, 2, 3)
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            OnCameraViewRequested?.Invoke(1);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            OnCameraViewRequested?.Invoke(2);
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            OnCameraViewRequested?.Invoke(3);
        }

        // Menu toggle (Escape)
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OnMenuToggle?.Invoke();
        }
    }

    public void Dispose()
    {
        // Clear all event subscriptions
        OnPrimaryAction = null;
        OnSecondaryAction = null;
        OnAbilityActivate = null;
        OnCameraViewRequested = null;
        OnMenuToggle = null;
    }
}
