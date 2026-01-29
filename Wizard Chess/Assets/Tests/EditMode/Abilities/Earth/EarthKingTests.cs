using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;

[TestFixture]
public class EarthKingTests
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

    // ========== King Capture Prevention ==========
    // In standard chess, kings can NEVER be captured - the game ends at checkmate.
    // The TryCapture safety guard enforces this rule before any passives are checked.

    [Test]
    public void KingCapture_AlwaysBlocked()
    {
        // Any king (Earth or otherwise) should never be capturable
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var attacker = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.BLACK, 4, 5);

        builder.BoardState.RecalculateAttacks();

        // Expect the error log from the safety guard
        LogAssert.Expect(LogType.Error, "[TryCapture] Attempted to capture a king! This should never happen.");

        bool captureAllowed = builder.GM.TryCapture(attacker, earthKing);

        Assert.IsFalse(captureAllowed, "King capture should always be blocked");
        // King survives
        PieceMove kingOnBoard = builder.BoardState.GetPieceAt(4, 4);
        Assert.AreEqual(earthKing, kingOnBoard, "King should survive - capture is never allowed");
    }

    [Test]
    public void KingCapture_NonEarthKing_AlsoBlocked()
    {
        // Normal (non-earth) king - capture should also be blocked
        var normalKing = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var attacker = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.BLACK, 4, 5);

        builder.BoardState.RecalculateAttacks();

        LogAssert.Expect(LogType.Error, "[TryCapture] Attempted to capture a king! This should never happen.");

        bool captureAllowed = builder.GM.TryCapture(attacker, normalKing);
        Assert.IsFalse(captureAllowed, "Non-earth king capture should also be blocked");
    }

    // ========== EarthKingPassive (Stone Shield) ==========
    // Note: Stone Shield never actually triggers in normal gameplay because:
    // 1. Kings can never be captured (TryCapture blocks before passive check)
    // 2. Checkmate detection ends the game before capture is attempted
    // These tests verify the passive WOULD work if somehow invoked directly.

    [Test]
    public void StoneShield_FlagStartsFalse()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        Assert.IsFalse(earthKing.elementalPiece.hasUsedStoneShield, "Shield flag should start false");
    }

    [Test]
    public void StoneShield_DirectInvocation_PreventsCapture()
    {
        // Test the passive directly (bypassing TryCapture safety guard)
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var attacker = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.BLACK, 4, 5);

        builder.BoardState.RecalculateAttacks();

        // Invoke passive directly
        var passive = earthKing.elementalPiece.passive as EarthKingPassive;
        Assert.IsNotNull(passive);

        bool allowCapture = passive.OnBeforeCapture(attacker, earthKing, builder.BoardState);

        Assert.IsFalse(allowCapture, "Stone Shield passive should return false (prevent capture)");
        Assert.IsTrue(earthKing.elementalPiece.hasUsedStoneShield, "Shield should be marked as used");
    }

    [Test]
    public void StoneShield_DirectInvocation_DestroysAttacker()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var attacker = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.BLACK, 4, 5);

        builder.BoardState.RecalculateAttacks();
        int attackerX = attacker.curx;
        int attackerY = attacker.cury;

        var passive = earthKing.elementalPiece.passive as EarthKingPassive;
        passive.OnBeforeCapture(attacker, earthKing, builder.BoardState);

        // Attacker should be removed from board state
        PieceMove pieceAtAttackerPos = builder.BoardState.GetPieceAt(attackerX, attackerY);
        Assert.IsNull(pieceAtAttackerPos, "Attacker should be removed when Stone Shield activates");
    }

    [Test]
    public void StoneShield_DirectInvocation_SecondAttemptAllows()
    {
        // After shield is used, passive returns true (would allow capture)
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var attacker1 = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.BLACK, 4, 5);

        builder.BoardState.RecalculateAttacks();

        var passive = earthKing.elementalPiece.passive as EarthKingPassive;

        // First invocation - shield activates
        passive.OnBeforeCapture(attacker1, earthKing, builder.BoardState);
        Assert.IsTrue(earthKing.elementalPiece.hasUsedStoneShield);

        // Second invocation - passive returns true (would allow, but TryCapture still blocks)
        var attacker2 = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 3, 4);
        builder.BoardState.RecalculateAttacks();

        bool allowSecond = passive.OnBeforeCapture(attacker2, earthKing, builder.BoardState);
        Assert.IsTrue(allowSecond, "Passive should return true after shield is used");
    }

    [Test]
    public void StoneShield_CheckStillWorks()
    {
        // Earth King should still be able to be put in check (shield only prevents capture)
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        // Rook attacks the king's square
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 2);

        builder.BoardState.RecalculateAttacks();
        bool inCheck = builder.BoardState.IsKingInCheck(ChessConstants.WHITE);

        Assert.IsTrue(inCheck, "Earth King should still be in check (shield only prevents capture)");
    }

    [Test]
    public void StoneShield_CheckmateStillWorks()
    {
        // Classic back-rank mate position: King trapped by own pawns
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);

        // Own pawns blocking escape
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 0, 6);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 1, 6);

        // Rook delivers mate on back rank
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 7, 7);

        builder.BoardState.RecalculateAttacks();

        bool inCheck = builder.BoardState.IsKingInCheck(ChessConstants.WHITE);
        Assert.IsTrue(inCheck, "Earth King should be in check in this position");

        // Generate king's moves - should have none (checkmate)
        earthKing.createPieceMoves(earthKing.piece);
        Assert.AreEqual(0, earthKing.moves.Count, "Checkmated Earth King should have no legal moves");
    }

    [Test]
    public void StoneShield_DirectInvocation_WorksAgainstAnyPieceType()
    {
        // Test passive works against different piece types (bypassing TryCapture)
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);

        builder.BoardState.RecalculateAttacks();

        var passive = earthKing.elementalPiece.passive as EarthKingPassive;
        bool allowCapture = passive.OnBeforeCapture(pawn, earthKing, builder.BoardState);

        Assert.IsFalse(allowCapture, "Stone Shield passive should work against pawn attacks");
        Assert.IsTrue(earthKing.elementalPiece.hasUsedStoneShield, "Shield should be marked as used");
    }

    [Test]
    public void StoneShield_DirectInvocation_WorksOnAnySquare()
    {
        // Earth King not on starting square - passive should still work
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 2, 3, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var attacker = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.BLACK, 2, 5);

        builder.BoardState.RecalculateAttacks();

        var passive = earthKing.elementalPiece.passive as EarthKingPassive;
        bool allowCapture = passive.OnBeforeCapture(attacker, earthKing, builder.BoardState);

        Assert.IsFalse(allowCapture, "Stone Shield passive should work regardless of king's position");
    }

    // ========== EarthKingActive (Sanctuary) ==========

    [Test]
    public void Sanctuary_CreatesAdjacentStoneWalls()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        Assert.IsNotNull(active);

        Square target = earthKing.curSquare;
        active.Execute(earthKing, target, builder.BoardState, builder.SEM);

        // KingDirections: (0,-1),(0,1),(1,0),(1,1),(1,-1),(-1,0),(-1,1),(-1,-1)
        // All 8 adjacent squares from (4,4)
        Assert.IsNotNull(builder.SEM.GetEffectAt(4, 3), "Wall at (4,3)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(4, 5), "Wall at (4,5)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(5, 4), "Wall at (5,4)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(5, 5), "Wall at (5,5)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(5, 3), "Wall at (5,3)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(3, 4), "Wall at (3,4)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(3, 5), "Wall at (3,5)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(3, 3), "Wall at (3,3)");
    }

    [Test]
    public void Sanctuary_WallsHaveCorrectDefaultProperties()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        active.Execute(earthKing, earthKing.curSquare, builder.BoardState, builder.SEM);

        var effect = builder.SEM.GetEffectAt(4, 3);
        Assert.IsNotNull(effect);
        Assert.AreEqual(SquareEffectType.StoneWall, effect.effectType);
        // Default wallHP = 1, wallDuration = 2 from EarthKingActiveParams
        Assert.AreEqual(1, effect.hitPoints, "Sanctuary wall should have default HP of 1");
        Assert.AreEqual(2, effect.remainingTurns, "Sanctuary wall should have default duration of 2");
    }

    [Test]
    public void Sanctuary_StunsKing()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        active.Execute(earthKing, earthKing.curSquare, builder.BoardState, builder.SEM);

        Assert.IsTrue(earthKing.elementalPiece.IsStunned(), "King should be stunned after activating Sanctuary");
    }

    [Test]
    public void Sanctuary_KingSelfStunDuration_DefaultIs2()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        active.Execute(earthKing, earthKing.curSquare, builder.BoardState, builder.SEM);

        Assert.IsTrue(earthKing.elementalPiece.IsStunned());

        // Default selfStunDuration = 2
        earthKing.elementalPiece.TickStatusEffects(); // tick 1
        Assert.IsTrue(earthKing.elementalPiece.IsStunned(), "King should still be stunned after 1 tick (duration 2)");

        earthKing.elementalPiece.TickStatusEffects(); // tick 2
        Assert.IsFalse(earthKing.elementalPiece.IsStunned(), "King stun should expire after 2 ticks");
    }

    [Test]
    public void Sanctuary_StunsAdjacentFriendlyPieces()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        // Place friendly piece adjacent at (5,4)
        var friendly = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 5, 4);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        active.Execute(earthKing, earthKing.curSquare, builder.BoardState, builder.SEM);

        Assert.IsNotNull(friendly.elementalPiece, "Friendly should get elementalPiece for stun tracking");
        Assert.IsTrue(friendly.elementalPiece.IsStunned(), "Adjacent friendly should be stunned by Sanctuary");
    }

    [Test]
    public void Sanctuary_AllyStunDuration_DefaultIs2()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        var friendly = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 5, 4);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        active.Execute(earthKing, earthKing.curSquare, builder.BoardState, builder.SEM);

        Assert.IsTrue(friendly.elementalPiece.IsStunned());

        // Default allyStunDuration = 2
        friendly.elementalPiece.TickStatusEffects(); // tick 1
        Assert.IsTrue(friendly.elementalPiece.IsStunned(), "Ally should still be stunned after 1 tick (duration 2)");

        friendly.elementalPiece.TickStatusEffects(); // tick 2
        Assert.IsFalse(friendly.elementalPiece.IsStunned(), "Ally stun should expire after 2 ticks");
    }

    [Test]
    public void Sanctuary_DoesNotStunAdjacentEnemies()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        // Place enemy adjacent at (3,4)
        var enemy = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 4);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        active.Execute(earthKing, earthKing.curSquare, builder.BoardState, builder.SEM);

        Assert.IsTrue(enemy.elementalPiece == null || !enemy.elementalPiece.IsStunned(),
            "Adjacent enemy should NOT be stunned by Sanctuary (only allies are stunned)");
    }

    [Test]
    public void Sanctuary_CanAlwaysActivate()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        Assert.IsTrue(active.CanActivate(earthKing, builder.BoardState, builder.SEM),
            "Sanctuary should always be activatable");
    }

    [Test]
    public void Sanctuary_GetTargetSquares_ReturnsKingsOwnSquare()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        List<Square> targets = active.GetTargetSquares(earthKing, builder.BoardState);

        Assert.AreEqual(1, targets.Count, "Sanctuary should have exactly 1 target (king's own square)");
        Assert.AreEqual(earthKing.curSquare, targets[0], "Target should be the king's own square");
    }

    [Test]
    public void Sanctuary_CreatesWallsAtEdge_HandlesOutOfBounds()
    {
        // King at corner (0,0) -- some adjacent squares are out of bounds
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 0, 0, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 7);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        active.Execute(earthKing, earthKing.curSquare, builder.BoardState, builder.SEM);

        // Only in-bounds adjacent squares should have walls
        // From (0,0): valid adjacent = (0,1), (1,0), (1,1)
        Assert.IsNotNull(builder.SEM.GetEffectAt(0, 1), "Wall at (0,1)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(1, 0), "Wall at (1,0)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(1, 1), "Wall at (1,1)");
    }
}
