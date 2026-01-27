using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Arc Flash (CD: 5): Swap positions with any friendly piece on the board.
/// Neither piece counts as having "moved" for firstMove purposes.
/// </summary>
public class LightningBishopActive : IActiveAbility
{
    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return GetTargetSquares(piece, bs).Count > 0;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();
        List<PieceMove> friendlies = bs.GetAllPieces(piece.color);

        foreach (PieceMove friendly in friendlies)
        {
            if (friendly == piece) continue; // Can't swap with self
            Square sq = piece.getSquare(friendly.curx, friendly.cury);
            if (sq != null) targets.Add(sq);
        }
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        PieceMove swapTarget = bs.GetPieceAt(target.x, target.y);
        if (swapTarget == null || swapTarget.color != piece.color) return false;

        // Save positions
        int ax = piece.curx, ay = piece.cury;
        int bx = swapTarget.curx, by = swapTarget.cury;
        Square sqA = piece.curSquare;
        Square sqB = swapTarget.curSquare;
        bool aFirstMove = piece.firstMove;
        bool bFirstMove = swapTarget.firstMove;

        // Remove both from squares
        piece.removePieceFromSquare();
        swapTarget.removePieceFromSquare();

        // Swap in board state
        bs.RemovePiece(ax, ay);
        bs.RemovePiece(bx, by);

        // Move piece A to B's position
        piece.setPieceLocation(bx, by);
        piece.curSquare = sqB;
        sqB.piece = piece;
        sqB.taken = true;
        piece.last = sqB.gameObject;
        bs.SetPieceAt(bx, by, piece);

        // Move piece B to A's position
        swapTarget.setPieceLocation(ax, ay);
        swapTarget.curSquare = sqA;
        sqA.piece = swapTarget;
        sqA.taken = true;
        swapTarget.last = sqA.gameObject;
        bs.SetPieceAt(ax, ay, swapTarget);

        // Preserve firstMove status
        piece.firstMove = aFirstMove;
        swapTarget.firstMove = bFirstMove;

        // Animate both to new positions
        Transform tA = piece.gameObject.transform;
        Transform tB = swapTarget.gameObject.transform;
        tA.DOMove(new Vector3(sqB.transform.position.x, tA.position.y, sqB.transform.position.z), 0.5f);
        tB.DOMove(new Vector3(sqA.transform.position.x, tB.position.y, sqA.transform.position.z), 0.5f);

        bs.RecalculateAttacks();

        Debug.Log("Arc Flash! " + piece.printPieceName() + " swaps with " + swapTarget.printPieceName());
        return true;
    }
}
