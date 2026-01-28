using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Static utility class for chess AI evaluation.
/// All evaluation is from the AI's perspective (positive = good for AI).
/// </summary>
public static class AIEvaluation
{
    // ========== Material Values (centipawns) ==========

    private static readonly int[] PieceValues = new int[]
    {
        0,      // 0 = unused
        100,    // 1 = PAWN
        500,    // 2 = ROOK
        320,    // 3 = KNIGHT
        330,    // 4 = BISHOP
        900,    // 5 = QUEEN
        10000   // 6 = KING
    };

    public static int GetPieceValue(int pieceType)
    {
        if (pieceType < 0 || pieceType >= PieceValues.Length) return 0;
        return PieceValues[pieceType];
    }

    // ========== Piece-Square Tables (from Black's perspective, row 0 = Black's back rank) ==========

    private static readonly float[,] PawnTable = new float[,]
    {
        {  0,  0,  0,  0,  0,  0,  0,  0 },
        { 50, 50, 50, 50, 50, 50, 50, 50 },
        { 10, 10, 20, 30, 30, 20, 10, 10 },
        {  5,  5, 10, 25, 25, 10,  5,  5 },
        {  0,  0,  0, 20, 20,  0,  0,  0 },
        {  5, -5,-10,  0,  0,-10, -5,  5 },
        {  5, 10, 10,-20,-20, 10, 10,  5 },
        {  0,  0,  0,  0,  0,  0,  0,  0 }
    };

    private static readonly float[,] KnightTable = new float[,]
    {
        {-50,-40,-30,-30,-30,-30,-40,-50 },
        {-40,-20,  0,  0,  0,  0,-20,-40 },
        {-30,  0, 10, 15, 15, 10,  0,-30 },
        {-30,  5, 15, 20, 20, 15,  5,-30 },
        {-30,  0, 15, 20, 20, 15,  0,-30 },
        {-30,  5, 10, 15, 15, 10,  5,-30 },
        {-40,-20,  0,  5,  5,  0,-20,-40 },
        {-50,-40,-30,-30,-30,-30,-40,-50 }
    };

    private static readonly float[,] BishopTable = new float[,]
    {
        {-20,-10,-10,-10,-10,-10,-10,-20 },
        {-10,  0,  0,  0,  0,  0,  0,-10 },
        {-10,  0, 10, 10, 10, 10,  0,-10 },
        {-10,  5,  5, 10, 10,  5,  5,-10 },
        {-10,  0, 10, 10, 10, 10,  0,-10 },
        {-10, 10, 10, 10, 10, 10, 10,-10 },
        {-10,  5,  0,  0,  0,  0,  5,-10 },
        {-20,-10,-10,-10,-10,-10,-10,-20 }
    };

    private static readonly float[,] RookTable = new float[,]
    {
        {  0,  0,  0,  0,  0,  0,  0,  0 },
        {  5, 10, 10, 10, 10, 10, 10,  5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        {  0,  0,  0,  5,  5,  0,  0,  0 }
    };

    private static readonly float[,] QueenTable = new float[,]
    {
        {-20,-10,-10, -5, -5,-10,-10,-20 },
        {-10,  0,  0,  0,  0,  0,  0,-10 },
        {-10,  0,  5,  5,  5,  5,  0,-10 },
        { -5,  0,  5,  5,  5,  5,  0, -5 },
        {  0,  0,  5,  5,  5,  5,  0, -5 },
        {-10,  5,  5,  5,  5,  5,  0,-10 },
        {-10,  0,  5,  0,  0,  0,  0,-10 },
        {-20,-10,-10, -5, -5,-10,-10,-20 }
    };

    private static readonly float[,] KingTable = new float[,]
    {
        {-30,-40,-40,-50,-50,-40,-40,-30 },
        {-30,-40,-40,-50,-50,-40,-40,-30 },
        {-30,-40,-40,-50,-50,-40,-40,-30 },
        {-30,-40,-40,-50,-50,-40,-40,-30 },
        {-20,-30,-30,-40,-40,-30,-30,-20 },
        {-10,-20,-20,-20,-20,-20,-20,-10 },
        { 20, 20,  0,  0,  0,  0, 20, 20 },
        { 20, 30, 10,  0,  0, 10, 30, 20 }
    };

    /// <summary>
    /// Get positional bonus for a piece at (x, y).
    /// Tables are from Black's perspective; for White, flip y-axis.
    /// </summary>
    public static float GetPositionalBonus(int pieceType, int x, int y, int color)
    {
        int row = (color == ChessConstants.WHITE) ? (7 - y) : y;
        int col = x;

        if (row < 0 || row > 7 || col < 0 || col > 7) return 0f;

        switch (pieceType)
        {
            case ChessConstants.PAWN:   return PawnTable[row, col];
            case ChessConstants.KNIGHT: return KnightTable[row, col];
            case ChessConstants.BISHOP: return BishopTable[row, col];
            case ChessConstants.ROOK:   return RookTable[row, col];
            case ChessConstants.QUEEN:  return QueenTable[row, col];
            case ChessConstants.KING:   return KingTable[row, col];
            default: return 0f;
        }
    }

    // ========== Move Scoring ==========

    /// <summary>
    /// Score a single move for the AI. Higher = better.
    /// </summary>
    public static float ScoreMove(PieceMove piece, Square target, BoardState bs,
                                   SquareEffectManager sem, int turnNumber)
    {
        float score = 0f;

        // 1. Capture value
        PieceMove victim = bs.GetPieceAt(target.x, target.y);
        bool isCapture = victim != null && victim.color != piece.color;

        if (isCapture)
        {
            int victimValue = GetPieceValue(victim.piece);
            int attackerValue = GetPieceValue(piece.piece);
            score += victimValue;

            // MVV-LVA bonus (prefer capturing high-value pieces with low-value attackers)
            score += (victimValue - attackerValue) * 0.1f;

            // Singed bonus: singed pieces are free captures
            if (victim.elementalPiece != null && victim.elementalPiece.IsSinged())
            {
                score += 50f;
            }
        }

        // 2. Positional delta
        float oldPositional = GetPositionalBonus(piece.piece, piece.curx, piece.cury, piece.color);
        float newPositional = GetPositionalBonus(piece.piece, target.x, target.y, piece.color);
        score += (newPositional - oldPositional) * 0.5f;

        // 3. Center control bonus
        if (target.x >= 2 && target.x <= 5 && target.y >= 2 && target.y <= 5)
        {
            score += 10f;
        }

        // 4. Check bonus (simulate move to see if it gives check)
        int opponentColor = (piece.color == ChessConstants.WHITE) ? ChessConstants.BLACK : ChessConstants.WHITE;
        if (WouldMoveGiveCheck(piece, target, bs, opponentColor))
        {
            score += 40f;
        }

        // 5. Development bonus (move non-pawn off back rank in early game)
        if (turnNumber < 20 && piece.piece != ChessConstants.PAWN)
        {
            int backRank = (piece.color == ChessConstants.BLACK) ? 0 : 7;
            if (piece.cury == backRank && target.y != backRank)
            {
                score += 15f;
            }
        }

        return score;
    }

    /// <summary>
    /// Extended scoring with hanging piece analysis (Hard mode).
    /// </summary>
    public static float ScoreMoveWithHangingAnalysis(PieceMove piece, Square target,
                                                      BoardState bs, SquareEffectManager sem,
                                                      int turnNumber)
    {
        float score = ScoreMove(piece, target, bs, sem, turnNumber);

        int opponentColor = (piece.color == ChessConstants.WHITE) ? ChessConstants.BLACK : ChessConstants.WHITE;
        bool targetAttackedByOpponent = bs.IsSquareAttackedBy(target.x, target.y, opponentColor);

        if (targetAttackedByOpponent)
        {
            PieceMove victim = bs.GetPieceAt(target.x, target.y);
            bool isCapture = victim != null && victim.color != piece.color;

            if (isCapture)
            {
                // Trade analysis: is the exchange favorable?
                int victimValue = GetPieceValue(victim.piece);
                int pieceValue = GetPieceValue(piece.piece);
                int netTrade = victimValue - pieceValue;

                if (netTrade < 0)
                {
                    // Bad trade: losing more than we gain
                    score += netTrade * 0.8f;
                }
            }
            else
            {
                // Moving to an attacked square without capturing: hanging piece penalty
                score -= GetPieceValue(piece.piece) * 0.8f;
            }
        }

        return score;
    }

    /// <summary>
    /// Score an ability use. Simple heuristic based on what's near the target.
    /// </summary>
    public static float ScoreAbilityUse(PieceMove piece, Square target,
                                         BoardState bs, SquareEffectManager sem)
    {
        float score = 0f;
        int opponentColor = (piece.color == ChessConstants.WHITE) ? ChessConstants.BLACK : ChessConstants.WHITE;

        // Direct target: enemy piece on target square
        PieceMove targetPiece = bs.GetPieceAt(target.x, target.y);
        if (targetPiece != null && targetPiece.color != piece.color)
        {
            score += GetPieceValue(targetPiece.piece);
        }

        // AoE: count enemies near target (Manhattan distance 1 and 2)
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = target.x + dx;
                int ny = target.y + dy;
                if (!bs.IsInBounds(nx, ny)) continue;

                PieceMove nearby = bs.GetPieceAt(nx, ny);
                if (nearby != null && nearby.color == opponentColor)
                {
                    int dist = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (dist <= 1)
                        score += 60f;
                    else if (dist <= 2)
                        score += 30f;
                }
            }
        }

        // Baseline score for area denial if no enemies affected
        if (score < 10f)
        {
            score += 10f;
        }

        return score;
    }

    // ========== Helpers ==========

    /// <summary>
    /// Check if a move would give check to the opponent's king.
    /// Simulates the move temporarily on the board state.
    /// </summary>
    private static bool WouldMoveGiveCheck(PieceMove piece, Square target,
                                            BoardState bs, int opponentColor)
    {
        // Simple simulation: temporarily move piece and check attacks
        int fromX = piece.curx;
        int fromY = piece.cury;
        int toX = target.x;
        int toY = target.y;

        PieceMove savedTarget = bs.GetPieceAt(toX, toY);

        // Simulate
        bs.MovePiece(fromX, fromY, toX, toY);
        int savedCurX = piece.curx;
        int savedCurY = piece.cury;
        piece.curx = toX;
        piece.cury = toY;
        bs.RecalculateAttacks();

        bool givesCheck = bs.IsKingInCheck(opponentColor);

        // Restore
        bs.MovePiece(toX, toY, fromX, fromY);
        if (savedTarget != null)
        {
            bs.SetPieceAt(toX, toY, savedTarget);
        }
        piece.curx = savedCurX;
        piece.cury = savedCurY;
        bs.RecalculateAttacks();

        return givesCheck;
    }
}
