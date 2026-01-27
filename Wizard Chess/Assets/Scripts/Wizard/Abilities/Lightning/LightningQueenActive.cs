using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Tempest: Push enemies within range away. Pieces pushed off-board are captured.
/// </summary>
public class LightningQueenActive : IActiveAbility
{
    private readonly LtQueenActiveParams _params;

    public LightningQueenActive() { _params = new LtQueenActiveParams(); }
    public LightningQueenActive(LtQueenActiveParams p) { _params = p; }

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
        List<PieceMove> toPush = new List<PieceMove>();

        for (int dx = -_params.detectionRange; dx <= _params.detectionRange; dx++)
        {
            for (int dy = -_params.detectionRange; dy <= _params.detectionRange; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = piece.curx + dx;
                int ny = piece.cury + dy;
                if (!bs.IsInBounds(nx, ny)) continue;

                PieceMove enemy = bs.GetPieceAt(nx, ny);
                if (enemy != null && enemy.color != piece.color)
                {
                    toPush.Add(enemy);
                }
            }
        }

        foreach (PieceMove enemy in toPush)
        {
            int dirX = System.Math.Sign(enemy.curx - piece.curx);
            int dirY = System.Math.Sign(enemy.cury - piece.cury);

            if (dirX == 0 && dirY == 0) continue;

            int finalX = enemy.curx;
            int finalY = enemy.cury;

            for (int push = 1; push <= _params.pushDistance; push++)
            {
                int nextX = finalX + dirX;
                int nextY = finalY + dirY;

                if (!bs.IsInBounds(nextX, nextY))
                {
                    piece.gm.takePiece(enemy);
                    Debug.Log(enemy.printPieceName() + " was pushed off the board by Tempest!");
                    finalX = -1;
                    break;
                }

                if (!bs.IsSquareEmpty(nextX, nextY))
                {
                    break;
                }

                finalX = nextX;
                finalY = nextY;
            }

            if (finalX >= 0 && (finalX != enemy.curx || finalY != enemy.cury))
            {
                Square destSq = piece.getSquare(finalX, finalY);
                if (destSq != null)
                {
                    enemy.removePieceFromSquare();
                    bs.MovePiece(enemy.curx, enemy.cury, finalX, finalY);
                    enemy.setPieceLocation(finalX, finalY);
                    enemy.curSquare = destSq;
                    destSq.piece = enemy;
                    destSq.taken = true;
                    enemy.last = destSq.gameObject;

                    Transform t = enemy.gameObject.transform;
                    t.DOMove(new Vector3(destSq.transform.position.x, t.position.y, destSq.transform.position.z), 0.3f);
                }
            }
        }

        bs.RecalculateAttacks();
        return true;
    }
}
