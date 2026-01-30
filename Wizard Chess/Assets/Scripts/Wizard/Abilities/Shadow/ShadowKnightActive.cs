using System.Collections.Generic;

/// <summary>
/// Doppelganger: Create a Shadow Decoy (fake piece) on any valid knight-move square.
/// </summary>
public class ShadowKnightActive : IActiveAbility
{
    private readonly ShadowKnightActiveParams _params;

    public ShadowKnightActive() { _params = new ShadowKnightActiveParams(); }
    public ShadowKnightActive(ShadowKnightActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return GetTargetSquares(piece, bs).Count > 0;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();

        // Can place decoy on any empty knight-move square
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
        // Create a ShadowDecoy on the target square
        // The decoy looks like the knight that created it
        sem.CreateEffect(target.x, target.y, SquareEffectType.ShadowDecoy, _params.decoyDuration, piece.color);

        return true;
    }
}
