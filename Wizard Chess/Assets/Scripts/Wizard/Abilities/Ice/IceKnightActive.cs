using System.Collections.Generic;

/// <summary>
/// Blizzard Leap: Perform a knight move and create Ice on all adjacent squares around landing.
/// </summary>
public class IceKnightActive : IActiveAbility
{
    private readonly IceKnightActiveParams _params;

    public IceKnightActive() { _params = new IceKnightActiveParams(); }
    public IceKnightActive(IceKnightActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return GetTargetSquares(piece, bs).Count > 0;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();

        // Standard knight moves to empty squares
        foreach (var dir in ChessConstants.KnightDirections)
        {
            int nx = piece.curx + dir.x;
            int ny = piece.cury + dir.y;

            if (!bs.IsInBounds(nx, ny)) continue;
            if (bs.IsSquareEmpty(nx, ny))
            {
                targets.Add(piece.getSquare(nx, ny));
            }
        }
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        // Move the knight
        piece.movePiece(target.x, target.y, target);

        // Create Ice on all adjacent squares
        foreach (var dir in ChessConstants.KingDirections)
        {
            int nx = target.x + dir.x;
            int ny = target.y + dir.y;

            if (!bs.IsInBounds(nx, ny)) continue;
            if (bs.IsSquareEmpty(nx, ny))
            {
                sem.CreateEffect(nx, ny, SquareEffectType.Ice, _params.iceDuration, piece.color);
            }
        }

        return true;
    }
}
