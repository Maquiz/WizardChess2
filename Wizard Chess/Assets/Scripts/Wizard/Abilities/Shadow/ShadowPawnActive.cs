using System.Collections.Generic;

/// <summary>
/// Shadow Lunge: Capture an enemy 1-2 squares directly forward.
/// </summary>
public class ShadowPawnActive : IActiveAbility
{
    private readonly ShadowPawnActiveParams _params;

    public ShadowPawnActive() { _params = new ShadowPawnActiveParams(); }
    public ShadowPawnActive(ShadowPawnActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return GetTargetSquares(piece, bs).Count > 0;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();
        int direction = piece.color == ChessConstants.WHITE ? -1 : 1;

        for (int i = 1; i <= _params.maxForwardRange; i++)
        {
            int ny = piece.cury + (direction * i);

            if (!bs.IsInBounds(piece.curx, ny)) break;

            PieceMove target = bs.GetPieceAt(piece.curx, ny);
            if (target != null && target.color != piece.color)
            {
                targets.Add(piece.getSquare(piece.curx, ny));
            }
        }
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        PieceMove targetPiece = bs.GetPieceAt(target.x, target.y);
        if (targetPiece == null || targetPiece.color == piece.color) return false;

        // Capture the target using TryCapture
        bool captureSuccess = piece.gm.TryCapture(piece, targetPiece);
        if (!captureSuccess) return false;

        // Move to the target square
        piece.movePiece(target.x, target.y, target);

        return true;
    }
}
