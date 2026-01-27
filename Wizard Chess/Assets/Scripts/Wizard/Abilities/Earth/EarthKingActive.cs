using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sanctuary: Adjacent squares become Stone Walls. King and adjacent friendlies immobilized.
/// </summary>
public class EarthKingActive : IActiveAbility
{
    private readonly EarthKingActiveParams _params;

    public EarthKingActive() { _params = new EarthKingActiveParams(); }
    public EarthKingActive(EarthKingActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return true;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();
        targets.Add(piece.curSquare);
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        foreach (var dir in ChessConstants.KingDirections)
        {
            int nx = piece.curx + dir.x;
            int ny = piece.cury + dir.y;
            if (!bs.IsInBounds(nx, ny)) continue;

            sem.CreateEffect(nx, ny, SquareEffectType.StoneWall, _params.wallDuration, piece.color, _params.wallHP);
        }

        if (piece.elementalPiece != null)
        {
            piece.elementalPiece.AddStatusEffect(StatusEffectType.Stunned, _params.selfStunDuration);
        }

        foreach (var dir in ChessConstants.KingDirections)
        {
            int nx = piece.curx + dir.x;
            int ny = piece.cury + dir.y;
            if (!bs.IsInBounds(nx, ny)) continue;

            PieceMove adj = bs.GetPieceAt(nx, ny);
            if (adj != null && adj.color == piece.color)
            {
                if (adj.elementalPiece == null)
                {
                    var ep = adj.gameObject.AddComponent<ElementalPiece>();
                    ep.Init(ChessConstants.ELEMENT_NONE, null, null, 0);
                }
                adj.elementalPiece.AddStatusEffect(StatusEffectType.Stunned, _params.allyStunDuration);
            }
        }

        Debug.Log("Sanctuary activated! King and adjacent allies are protected but immobilized.");
        return true;
    }
}
