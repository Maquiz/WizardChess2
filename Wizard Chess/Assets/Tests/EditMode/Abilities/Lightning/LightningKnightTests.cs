using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class LightningKnightTests
{
    private ChessBoardBuilder builder;

    [SetUp]
    public void SetUp()
    {
        builder = new ChessBoardBuilder();
        builder.Build();
    }

    [TearDown]
    public void TearDown()
    {
        builder.Cleanup();
    }

    private void PlaceKings()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
    }

    // ========== LightningKnightPassive (Double Jump) ==========

    [Test]
    public void Passive_AddsCardinalMovesFromLandingSquares()
    {
        // Double Jump: from each knight landing square, adds cardinal moves (1 extra step)
        PlaceKings();
        var knight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var moves = builder.GenerateMoves(knight);

        // Knight at (3,4) has standard L-moves: (4,6),(5,5),(5,3),(4,2),(2,2),(1,3),(1,5),(2,6)
        // From each landing, extra cardinal moves (range=1) are added if empty and not the origin
        // For example, from landing (4,2): cardinal extras -> (5,2),(3,2),(4,1),(4,3) if empty and not (3,4)
        // Check that at least some extra moves exist beyond standard knight moves
        bool hasExtraMove = false;
        HashSet<(int, int)> standardKnightMoves = new HashSet<(int, int)>
        {
            (4, 6), (5, 5), (5, 3), (4, 2), (2, 2), (1, 3), (1, 5), (2, 6)
        };
        foreach (var m in moves)
        {
            if (!standardKnightMoves.Contains((m.x, m.y)))
            {
                hasExtraMove = true;
                break;
            }
        }
        Assert.IsTrue(hasExtraMove, "Passive should add extra cardinal moves beyond standard knight moves");
    }

    [Test]
    public void Passive_ExtraMovesDoNotIncludeOrigin()
    {
        // The knight's current position should not appear as an extra move
        PlaceKings();
        var knight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var moves = builder.GenerateMoves(knight);

        TestExtensions.AssertDoesNotContainMove(moves, 3, 4,
            "Extra cardinal moves should not include the knight's own position");
    }

    [Test]
    public void Passive_ExtraMovesBlockedByPieces()
    {
        // Extra cardinal moves from landing squares are blocked by occupied squares
        PlaceKings();
        var knight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        // Landing square (5,5) has cardinal extras: (6,5),(4,5),(5,6),(5,4)
        // Block (6,5) with a piece
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 6, 5);

        var moves = builder.GenerateMoves(knight);

        // (6,5) is occupied, so it should not be in extra moves (only empty squares)
        TestExtensions.AssertDoesNotContainMove(moves, 6, 5,
            "Extra move should not be added to occupied square");
    }

    [Test]
    public void Passive_OnlyApplesToKnights()
    {
        // ModifyMoveGeneration returns unmodified list for non-knights
        PlaceKings();
        var rook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        // Manually test the passive with a rook
        var passive = new LightningKnightPassive();
        var baseMoves = new List<Square>();
        var result = passive.ModifyMoveGeneration(baseMoves, rook, builder.BoardState);
        Assert.AreEqual(0, result.Count, "Should not add moves for non-knight pieces");
    }

    [Test]
    public void Passive_ExtraMovesAreOnlyOnEmptySquares()
    {
        // Extra moves from landing squares only go to empty squares
        PlaceKings();
        var knight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 4, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var moves = builder.GenerateMoves(knight);

        // All moves should be to either empty squares or capture squares (standard knight behavior)
        foreach (var m in moves)
        {
            PieceMove occupant = builder.BoardState.GetPieceAt(m.x, m.y);
            if (occupant != null)
            {
                // If occupied, it should be an enemy and also a standard knight move
                Assert.AreNotEqual(ChessConstants.WHITE, occupant.color,
                    "Extra moves should not target friendly-occupied squares");
            }
        }
    }

    // ========== LightningKnightActive (Lightning Rod) ==========

    [Test]
    public void Active_CanActivate_WhenEmptySquaresInRange()
    {
        PlaceKings();
        var knight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        Assert.IsTrue(knight.elementalPiece.active.CanActivate(knight, builder.BoardState, builder.SEM),
            "Should be able to activate with empty squares in range");
    }

    [Test]
    public void Active_GetTargetSquares_UseManhattanDistance()
    {
        // Teleport range=5 (Manhattan distance). Only empty squares within range.
        PlaceKings();
        var knight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var targets = knight.elementalPiece.active.GetTargetSquares(knight, builder.BoardState);

        // Check a square within Manhattan distance 5
        bool hasNearTarget = false;
        bool hasOutOfRange = false;
        foreach (var t in targets)
        {
            int dist = System.Math.Abs(t.x - 3) + System.Math.Abs(t.y - 4);
            if (dist <= 5 && dist > 0) hasNearTarget = true;
            if (dist > 5) hasOutOfRange = true;
        }
        Assert.IsTrue(hasNearTarget, "Should have targets within teleport range");
        Assert.IsFalse(hasOutOfRange, "Should not have targets beyond teleport range");
    }

    [Test]
    public void Active_GetTargetSquares_ExcludesOccupiedSquares()
    {
        PlaceKings();
        var knight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);

        var targets = knight.elementalPiece.active.GetTargetSquares(knight, builder.BoardState);

        bool hasOccupied = false;
        foreach (var t in targets)
        {
            if (t.x == 3 && t.y == 3) hasOccupied = true;
        }
        Assert.IsFalse(hasOccupied, "Occupied squares should not be teleport targets");
    }

    [Test]
    public void Active_Execute_TeleportsKnight()
    {
        PlaceKings();
        var knight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var target = builder.GetSquare(5, 5);
        bool result = knight.elementalPiece.active.Execute(knight, target, builder.BoardState, builder.SEM);

        Assert.IsTrue(result);
        Assert.AreEqual(5, knight.curx);
        Assert.AreEqual(5, knight.cury, "Knight should have teleported to (5,5)");
    }

    [Test]
    public void Active_Execute_StunsSharedAdjacentEnemies()
    {
        // Lightning Rod: stun enemies adjacent to BOTH origin and destination
        PlaceKings();
        var knight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        // Place enemy adjacent to both (3,4) and (4,4) [target]
        // (3,3) is adjacent to origin (3,4) via (0,-1). Check if it's adjacent to target (4,4):
        //   (4,4) neighbors: (3,3),(3,4),(3,5),(4,3),(4,5),(5,3),(5,4),(5,5)
        // So (3,3) is adjacent to both origin and destination
        var enemy = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3,
            ChessConstants.ELEMENT_LIGHTNING);

        var target = builder.GetSquare(4, 4);
        knight.elementalPiece.active.Execute(knight, target, builder.BoardState, builder.SEM);

        // Enemy at (3,3) is adjacent to both origin (3,4) and destination (4,4)
        Assert.IsTrue(enemy.elementalPiece.IsStunned(),
            "Enemy adjacent to both origin and destination should be stunned");
    }

    [Test]
    public void Active_Execute_DoesNotStunNonSharedAdjacent()
    {
        // Enemies adjacent to only origin OR only destination should NOT be stunned
        PlaceKings();
        var knight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 0, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        // Place enemy adjacent only to origin (0,4), not to destination (3,6)
        var enemy = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.BLACK, 1, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var target = builder.GetSquare(3, 6);
        knight.elementalPiece.active.Execute(knight, target, builder.BoardState, builder.SEM);

        // (1,4) is adjacent to origin (0,4) but NOT adjacent to destination (3,6)
        Assert.IsFalse(enemy.elementalPiece.IsStunned(),
            "Enemy only adjacent to origin should not be stunned");
    }
}
