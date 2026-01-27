using System.Collections.Generic;

/// <summary>
/// Backdraft (CD: 8): All Fire Squares on the board capture enemy pieces (not kings)
/// adjacent to them, then all Fire Squares are removed.
/// </summary>
public class FireKingActive : IActiveAbility
{
    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        // Need at least one fire square on the board
        return sem.GetAllEffectsOfType(SquareEffectType.Fire).Count > 0;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        // Self-cast
        List<Square> targets = new List<Square>();
        targets.Add(piece.curSquare);
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        List<SquareEffect> fireEffects = sem.GetAllEffectsOfType(SquareEffectType.Fire);
        List<PieceMove> toCaptureList = new List<PieceMove>();

        // Find all enemy pieces adjacent to fire squares
        foreach (var fire in fireEffects)
        {
            Square sq = piece.getSquare(fire.gameObject.GetComponent<Square>().x,
                                         fire.gameObject.GetComponent<Square>().y);
            if (sq == null) continue;

            foreach (var dir in ChessConstants.KingDirections)
            {
                int nx = sq.x + dir.x;
                int ny = sq.y + dir.y;
                if (!bs.IsInBounds(nx, ny)) continue;

                PieceMove adj = bs.GetPieceAt(nx, ny);
                if (adj != null && adj.color != piece.color && adj.piece != ChessConstants.KING)
                {
                    if (!toCaptureList.Contains(adj))
                        toCaptureList.Add(adj);
                }
            }
        }

        // Capture all found enemies — via TryCapture for passive hooks
        foreach (var p in toCaptureList)
        {
            piece.gm.TryCapture(piece, p);
        }

        // Remove all fire squares
        sem.RemoveAllEffectsOfType(SquareEffectType.Fire);

        return true;
    }
}
