using System.Collections.Generic;

/// <summary>
/// Meteor Strike: Target any square within movement range; create a fire zone.
/// Captures the first enemy piece in the zone.
/// </summary>
public class FireQueenActive : IActiveAbility
{
    private readonly FireQueenActiveParams _params;

    public FireQueenActive() { _params = new FireQueenActiveParams(); }
    public FireQueenActive(FireQueenActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return true;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();
        piece.moves.Clear();
        piece.createPieceMoves(piece.piece);
        foreach (Square move in piece.moves)
        {
            targets.Add(move);
        }
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        int captureCount = 0;

        for (int dx = -_params.aoeRadius; dx <= _params.aoeRadius; dx++)
        {
            for (int dy = -_params.aoeRadius; dy <= _params.aoeRadius; dy++)
            {
                int nx = target.x + dx;
                int ny = target.y + dy;
                if (!bs.IsInBounds(nx, ny)) continue;

                if (captureCount < _params.maxCaptures)
                {
                    PieceMove targetPiece = bs.GetPieceAt(nx, ny);
                    if (targetPiece != null && targetPiece.color != piece.color
                        && targetPiece.piece != ChessConstants.KING)
                    {
                        if (piece.gm.TryCapture(piece, targetPiece))
                            captureCount++;
                    }
                }

                sem.CreateEffect(nx, ny, SquareEffectType.Fire, _params.fireDuration, piece.color);
            }
        }
        return true;
    }
}
