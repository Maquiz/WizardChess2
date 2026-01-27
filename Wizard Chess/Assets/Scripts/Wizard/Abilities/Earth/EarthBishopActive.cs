using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Petrify: Target one enemy piece within diagonal range.
/// That piece becomes a Stone Wall, stunned.
/// </summary>
public class EarthBishopActive : IActiveAbility
{
    private readonly EarthBishopActiveParams _params;

    public EarthBishopActive() { _params = new EarthBishopActiveParams(); }
    public EarthBishopActive(EarthBishopActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return GetTargetSquares(piece, bs).Count > 0;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();

        foreach (var dir in ChessConstants.BishopDirections)
        {
            int nx = piece.curx + dir.x;
            int ny = piece.cury + dir.y;
            while (bs.IsInBounds(nx, ny))
            {
                PieceMove targetPiece = bs.GetPieceAt(nx, ny);
                if (targetPiece != null)
                {
                    if (targetPiece.color != piece.color && targetPiece.piece != ChessConstants.KING)
                    {
                        targets.Add(piece.getSquare(nx, ny));
                    }
                    break;
                }
                nx += dir.x;
                ny += dir.y;
            }
        }
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        PieceMove targetPiece = bs.GetPieceAt(target.x, target.y);
        if (targetPiece == null) return false;

        if (targetPiece.elementalPiece == null)
        {
            var ep = targetPiece.gameObject.AddComponent<ElementalPiece>();
            ep.Init(ChessConstants.ELEMENT_NONE, null, null, 0);
        }
        targetPiece.elementalPiece.AddStatusEffect(StatusEffectType.Stunned, _params.stunDuration);
        sem.CreateEffect(target.x, target.y, SquareEffectType.StoneWall, _params.wallDuration, piece.color, _params.wallHP);

        Debug.Log(targetPiece.printPieceName() + " has been petrified!");
        return true;
    }
}
