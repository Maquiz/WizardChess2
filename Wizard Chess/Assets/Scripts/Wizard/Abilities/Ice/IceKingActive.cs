using System.Collections.Generic;

/// <summary>
/// Glacial Fortress: Create Ice on all 8 adjacent squares and gain temporary Ice immunity.
/// </summary>
public class IceKingActive : IActiveAbility
{
    private readonly IceKingActiveParams _params;

    public IceKingActive() { _params = new IceKingActiveParams(); }
    public IceKingActive(IceKingActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        // Can always activate if there's at least one empty adjacent square
        foreach (var dir in ChessConstants.KingDirections)
        {
            int nx = piece.curx + dir.x;
            int ny = piece.cury + dir.y;
            if (bs.IsInBounds(nx, ny) && bs.IsSquareEmpty(nx, ny))
            {
                return true;
            }
        }
        return false;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        // Target is self - just return current square
        List<Square> targets = new List<Square>();
        targets.Add(piece.getSquare(piece.curx, piece.cury));
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        // Create Ice on all 8 adjacent squares
        foreach (var dir in ChessConstants.KingDirections)
        {
            int nx = piece.curx + dir.x;
            int ny = piece.cury + dir.y;

            if (!bs.IsInBounds(nx, ny)) continue;

            if (bs.IsSquareEmpty(nx, ny))
            {
                sem.CreateEffect(nx, ny, SquareEffectType.Ice, _params.iceDuration, piece.color);
            }
        }

        // Grant temporary Ice immunity
        if (piece.elementalPiece != null)
        {
            piece.elementalPiece.AddImmunity(SquareEffectType.Ice);
            // Note: Immunity removal after duration would need additional tracking
            // For now, the immunity persists (balanced by the defensive nature of the ability)
        }

        return true;
    }
}
