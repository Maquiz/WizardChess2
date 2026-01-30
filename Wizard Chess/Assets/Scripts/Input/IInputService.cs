using UnityEngine;
using System;

/// <summary>
/// Platform-agnostic input service interface.
/// Provides events for all input actions that game code can subscribe to.
/// Implementations: DesktopInputService (mouse/keyboard), TouchInputService (touch/buttons).
/// </summary>
public interface IInputService
{
    // ========== Events ==========

    /// <summary>
    /// Fired when primary action is triggered (left mouse click / tap).
    /// </summary>
    event Action OnPrimaryAction;

    /// <summary>
    /// Fired when secondary action is triggered (right mouse click / none on mobile).
    /// </summary>
    event Action OnSecondaryAction;

    /// <summary>
    /// Fired when ability activation is requested (Q key / long-press on mobile).
    /// </summary>
    event Action OnAbilityActivate;

    /// <summary>
    /// Fired when camera view change is requested. Parameter is view index (1=White, 2=Black, 3=Top).
    /// </summary>
    event Action<int> OnCameraViewRequested;

    /// <summary>
    /// Fired when menu toggle is requested (Escape key / menu button on mobile).
    /// </summary>
    event Action OnMenuToggle;

    // ========== Properties ==========

    /// <summary>
    /// Current pointer position in screen coordinates.
    /// </summary>
    Vector3 PointerPosition { get; }

    /// <summary>
    /// Whether the pointer is currently down (mouse button held / finger touching).
    /// </summary>
    bool IsPointerDown { get; }

    /// <summary>
    /// Whether this frame had a primary action start (GetMouseButtonDown equivalent).
    /// </summary>
    bool PrimaryActionThisFrame { get; }

    /// <summary>
    /// Whether this frame had a secondary action start (GetMouseButtonDown(1) equivalent).
    /// </summary>
    bool SecondaryActionThisFrame { get; }

    // ========== Methods ==========

    /// <summary>
    /// Initialize the input service. Called once at startup.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Update the input state. Should be called every frame.
    /// </summary>
    void Update();

    /// <summary>
    /// Clean up resources. Called on shutdown.
    /// </summary>
    void Dispose();
}
