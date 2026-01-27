using System.Collections.Generic;

/// <summary>
/// Interface for passive elemental abilities that modify chess behavior.
/// All methods have default no-op behavior so implementations only override what they need.
/// </summary>
public interface IPassiveAbility
{
    /// <summary>
    /// Modify the generated moves list. Called after base move generation, before filterIllegalMoves.
    /// Return the modified list.
    /// </summary>
    List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs);

    /// <summary>
    /// Called before a capture is executed. Return false to prevent the capture.
    /// attacker is the piece doing the capturing, defender is being captured.
    /// </summary>
    bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs);

    /// <summary>
    /// Called after a capture is executed.
    /// </summary>
    void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs);

    /// <summary>
    /// Called after this piece moves (not captures).
    /// </summary>
    void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs);

    /// <summary>
    /// Called when THIS piece is captured by another piece.
    /// </summary>
    void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs);

    /// <summary>
    /// Called at the start of each turn.
    /// </summary>
    void OnTurnStart(int currentTurnColor);
}
