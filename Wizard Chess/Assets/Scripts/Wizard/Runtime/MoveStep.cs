using System;

/// <summary>
/// Types of steps in a multi-step move sequence.
/// </summary>
public enum MoveStepType
{
    MoveTo,     // Move piece to destination square
    Capture,    // Capture a piece (victim already known)
    Custom      // Execute a custom action (fire effects, push, etc.)
}

/// <summary>
/// Represents a single step in a multi-step move sequence.
/// Used by MultiStepMoveController to orchestrate sequential animations.
/// </summary>
public class MoveStep
{
    public MoveStepType Type { get; private set; }
    public PieceMove Piece { get; private set; }
    public Square Destination { get; private set; }
    public PieceMove CaptureTarget { get; private set; }
    public Action CustomAction { get; private set; }

    /// <summary>
    /// Optional: intermediate square for multi-leg moves (e.g., knight double-jump)
    /// </summary>
    public Square IntermediateSquare { get; set; }

    /// <summary>
    /// If true, this step should record a move in history.
    /// Default false - only final step typically records.
    /// </summary>
    public bool RecordInHistory { get; set; }

    /// <summary>
    /// Animation duration override. If <= 0, uses controller default.
    /// </summary>
    public float Duration { get; set; }

    private MoveStep() { }

    /// <summary>
    /// Create a MoveTo step - animates piece to destination and updates board state.
    /// </summary>
    public static MoveStep MoveTo(PieceMove piece, Square destination, bool recordInHistory = false)
    {
        return new MoveStep
        {
            Type = MoveStepType.MoveTo,
            Piece = piece,
            Destination = destination,
            RecordInHistory = recordInHistory,
            Duration = 0f
        };
    }

    /// <summary>
    /// Create a MoveTo step with a specific animation duration.
    /// </summary>
    public static MoveStep MoveTo(PieceMove piece, Square destination, float duration, bool recordInHistory = false)
    {
        return new MoveStep
        {
            Type = MoveStepType.MoveTo,
            Piece = piece,
            Destination = destination,
            RecordInHistory = recordInHistory,
            Duration = duration
        };
    }

    /// <summary>
    /// Create a Capture step - captures the victim piece (removes from board).
    /// The attacker should already be at the capture location or moving there.
    /// </summary>
    public static MoveStep Capture(PieceMove attacker, PieceMove victim)
    {
        return new MoveStep
        {
            Type = MoveStepType.Capture,
            Piece = attacker,
            CaptureTarget = victim,
            Duration = 0f
        };
    }

    /// <summary>
    /// Create a Custom step - executes arbitrary action (for effects, pushes, etc.)
    /// </summary>
    public static MoveStep Custom(Action action)
    {
        return new MoveStep
        {
            Type = MoveStepType.Custom,
            CustomAction = action,
            Duration = 0f
        };
    }

    /// <summary>
    /// Create a Custom step with a specific duration to wait after action.
    /// </summary>
    public static MoveStep Custom(Action action, float waitDuration)
    {
        return new MoveStep
        {
            Type = MoveStepType.Custom,
            CustomAction = action,
            Duration = waitDuration
        };
    }

    public override string ToString()
    {
        switch (Type)
        {
            case MoveStepType.MoveTo:
                return $"MoveTo({Piece?.printPieceName()} -> {Destination?.x},{Destination?.y})";
            case MoveStepType.Capture:
                return $"Capture({Piece?.printPieceName()} x {CaptureTarget?.printPieceName()})";
            case MoveStepType.Custom:
                return "Custom(action)";
            default:
                return "Unknown";
        }
    }
}
