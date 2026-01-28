using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Test helper that builds a minimal Unity board hierarchy for chess tests.
/// Creates GameMaster, BoardState, 8 row GameObjects, 64 Square children,
/// and provides methods to place pieces and verify state.
///
/// Usage:
///   var builder = new ChessBoardBuilder();
///   builder.Build();
///   var piece = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 4, 4);
///   // ... run tests ...
///   builder.Cleanup();
/// </summary>
public class ChessBoardBuilder
{
    public GameMaster GM { get; private set; }
    public BoardState BoardState => GM.boardState;
    public SquareEffectManager SEM => GM.squareEffectManager;
    public AbilityExecutor AE => GM.abilityExecutor;

    private GameObject gmObject;
    private GameObject boardRoot;
    private GameObject hitBoardObject;
    private List<GameObject> allCreatedObjects = new List<GameObject>();
    private List<PieceMove> allPieces = new List<PieceMove>();

    /// <summary>
    /// Build the minimal board hierarchy needed for tests.
    /// </summary>
    public void Build()
    {
        // Create HitBoard (needed by Square.Start)
        hitBoardObject = new GameObject("HitBoard");
        hitBoardObject.AddComponent<AudioSource>();
        allCreatedObjects.Add(hitBoardObject);

        // Create GameMaster GameObject
        gmObject = new GameObject("GameMaster");
        gmObject.tag = "GM";
        GM = gmObject.AddComponent<GameMaster>();
        allCreatedObjects.Add(gmObject);

        // Initialize GM fields that Start() would set
        GM.boardSize = 8;
        GM.boardPos = new GameObject[8, 8];
        GM.boardRows = new GameObject[8];
        GM.moveHistory = new System.Collections.Generic.Stack<ChessMove>();
        GM.boardState = new BoardState();
        GM.currentMove = ChessConstants.WHITE;
        GM.currentGameState = GameState.Playing;

        // LineRenderer (needed by deSelectPiece)
        GM.lr = gmObject.AddComponent<LineRenderer>();
        GM.lr.enabled = false;

        // Create PieceUI stubs (needed by selectPiece)
        GM.selectedUI = CreatePieceUIStub("SelectedUI");
        GM.canMoveUI = CreatePieceUIStub("CanMoveUI");
        GM.cantMoveUI = CreatePieceUIStub("CantMoveUI");
        GM.takeMoveUI = CreatePieceUIStub("TakeMoveUI");

        // Create SquareEffectManager
        GM.squareEffectManager = gmObject.AddComponent<SquareEffectManager>();
        GM.squareEffectManager.Init(GM);

        // Create AbilityExecutor
        GM.abilityExecutor = gmObject.AddComponent<AbilityExecutor>();
        GM.abilityExecutor.Init(GM, GM.squareEffectManager);

        // Create board root with tag
        boardRoot = new GameObject("Board");
        boardRoot.tag = "Board";
        allCreatedObjects.Add(boardRoot);

        // Create 8 row GameObjects, each with 8 Square children
        for (int y = 0; y < 8; y++)
        {
            GameObject row = new GameObject("Row_" + y);
            row.transform.parent = boardRoot.transform;
            GM.boardRows[y] = row;
            allCreatedObjects.Add(row);

            for (int x = 0; x < 8; x++)
            {
                GameObject sqObj = new GameObject("Square_" + x + "_" + y);
                sqObj.tag = "Board";
                sqObj.transform.parent = row.transform;
                sqObj.transform.position = new Vector3(x, 0, y);

                // Add BoxCollider for raycasting
                sqObj.AddComponent<BoxCollider>();

                Square sq = sqObj.AddComponent<Square>();
                sq.x = x;
                sq.y = y;
                sq.taken = false;
                sq.piece = null;

                // Create showMoveSquare child (needed by showMovesHelper/hideMovesHelper)
                GameObject showMoveChild = new GameObject("ShowMove");
                showMoveChild.transform.parent = sqObj.transform;
                showMoveChild.SetActive(false);
                sq.showMoveSquare = showMoveChild;

                GM.boardPos[y, x] = sqObj;
            }
        }
    }

    /// <summary>
    /// Get a Square at the given board coordinates.
    /// </summary>
    public Square GetSquare(int x, int y)
    {
        if (x < 0 || x >= 8 || y < 0 || y >= 8) return null;
        return GM.boardRows[y].transform.GetChild(x).GetComponent<Square>();
    }

    /// <summary>
    /// Place a piece on the board at the given coordinates.
    /// Returns the PieceMove component.
    /// </summary>
    public PieceMove PlacePiece(int pieceType, int color, int x, int y)
    {
        GameObject pieceObj = new GameObject("Piece_" + pieceType + "_" + color + "_" + x + "_" + y);
        pieceObj.tag = "Piece";
        pieceObj.transform.position = new Vector3(x, 1, y);
        allCreatedObjects.Add(pieceObj);

        // Add required components
        pieceObj.AddComponent<MeshFilter>();
        pieceObj.AddComponent<MeshRenderer>();
        pieceObj.AddComponent<MeshCollider>();

        PieceMove pm = pieceObj.AddComponent<PieceMove>();
        // Manually initialize instead of relying on Start() which uses FindGameObjectWithTag
        pm.piece = pieceType;
        pm.color = color;
        pm.curx = x;
        pm.cury = y;
        pm.lastx = x;
        pm.lasty = y;
        pm.canMove = true;
        pm.firstMove = true;
        pm.showMoves = false;
        pm.gm = GM;
        pm.Board = boardRoot;

        // Wire up square
        Square sq = GetSquare(x, y);
        sq.taken = true;
        sq.piece = pm;
        pm.curSquare = sq;

        // Register with BoardState
        BoardState.SetPieceAt(x, y, pm);
        BoardState.RecalculateAttacks();

        allPieces.Add(pm);
        return pm;
    }

    /// <summary>
    /// Place a piece with elemental abilities.
    /// </summary>
    public PieceMove PlaceElementalPiece(int pieceType, int color, int x, int y, int elementId)
    {
        PieceMove pm = PlacePiece(pieceType, color, x, y);

        ElementalPiece ep = pm.gameObject.AddComponent<ElementalPiece>();
        IPassiveAbility passive = AbilityFactory.CreatePassive(elementId, pieceType);
        IActiveAbility active = AbilityFactory.CreateActive(elementId, pieceType);
        int cooldown = AbilityFactory.GetCooldown(elementId, pieceType);
        ep.Init(elementId, passive, active, cooldown);

        return pm;
    }

    /// <summary>
    /// Move a piece to a new position, updating all state (BoardState, Square, PieceMove).
    /// Does NOT trigger DOTween animation or passive hooks — pure state update for tests.
    /// </summary>
    public void MovePieceState(PieceMove piece, int toX, int toY)
    {
        int fromX = piece.curx;
        int fromY = piece.cury;

        // Clear old square
        Square oldSq = GetSquare(fromX, fromY);
        oldSq.taken = false;
        oldSq.piece = null;

        // Handle capture
        Square newSq = GetSquare(toX, toY);
        if (newSq.taken && newSq.piece != null)
        {
            PieceMove captured = newSq.piece;
            BoardState.RemovePiece(toX, toY);
            allPieces.Remove(captured);
        }

        // Update piece state
        piece.lastx = fromX;
        piece.lasty = fromY;
        piece.curx = toX;
        piece.cury = toY;
        piece.curSquare = newSq;
        piece.firstMove = false;

        // Update square state
        newSq.taken = true;
        newSq.piece = piece;

        // Update BoardState
        BoardState.MovePiece(fromX, fromY, toX, toY);
        BoardState.RecalculateAttacks();
    }

    /// <summary>
    /// Capture a piece at the given position. Removes from board state and allPieces.
    /// </summary>
    public void CapturePiece(PieceMove attacker, PieceMove defender)
    {
        int dx = defender.curx;
        int dy = defender.cury;

        // Remove defender from board
        Square defSq = GetSquare(dx, dy);
        defSq.taken = false;
        defSq.piece = null;
        BoardState.RemovePiece(dx, dy);
        allPieces.Remove(defender);

        // Move attacker to defender's position
        MovePieceState(attacker, dx, dy);
    }

    /// <summary>
    /// Generate moves for a piece and return the move list.
    /// </summary>
    public List<Square> GenerateMoves(PieceMove piece)
    {
        piece.createPieceMoves(piece.piece);
        return piece.moves;
    }

    /// <summary>
    /// Assert that a piece's move set contains exactly the expected coordinates.
    /// </summary>
    public void AssertMoves(PieceMove piece, params (int x, int y)[] expectedCoords)
    {
        piece.createPieceMoves(piece.piece);
        var moveCoords = new HashSet<(int x, int y)>();
        foreach (var m in piece.moves)
        {
            moveCoords.Add((m.x, m.y));
        }

        var expected = new HashSet<(int x, int y)>(expectedCoords);

        // Check for missing expected moves
        foreach (var e in expected)
        {
            Assert.IsTrue(moveCoords.Contains(e),
                $"Expected move to ({e.x},{e.y}) not found. Piece at ({piece.curx},{piece.cury}). " +
                $"Actual moves: {FormatMoves(moveCoords)}");
        }

        // Check for unexpected moves
        foreach (var m in moveCoords)
        {
            Assert.IsTrue(expected.Contains(m),
                $"Unexpected move to ({m.x},{m.y}). Piece at ({piece.curx},{piece.cury}). " +
                $"Expected moves: {FormatMoves(expected)}");
        }
    }

    /// <summary>
    /// Assert that a piece's PieceMove.curx/cury matches BoardState.GetPieceAt.
    /// </summary>
    public void AssertPositionSync(PieceMove piece)
    {
        PieceMove boardPiece = BoardState.GetPieceAt(piece.curx, piece.cury);
        Assert.AreEqual(piece, boardPiece,
            $"Position desync: PieceMove at ({piece.curx},{piece.cury}) but BoardState has " +
            (boardPiece == null ? "null" : $"different piece at that position"));

        Square sq = GetSquare(piece.curx, piece.cury);
        Assert.IsTrue(sq.taken, $"Square ({piece.curx},{piece.cury}) should be taken");
        Assert.AreEqual(piece, sq.piece, $"Square.piece should match PieceMove at ({piece.curx},{piece.cury})");
        Assert.AreEqual(sq, piece.curSquare, $"PieceMove.curSquare should match actual square at ({piece.curx},{piece.cury})");
    }

    /// <summary>
    /// Assert position sync for all placed pieces.
    /// </summary>
    public void AssertAllPositionsSync()
    {
        foreach (var piece in allPieces)
        {
            AssertPositionSync(piece);
        }
    }

    /// <summary>
    /// Destroy all created GameObjects. Call in [TearDown].
    /// </summary>
    public void Cleanup()
    {
        foreach (var obj in allCreatedObjects)
        {
            if (obj != null)
                Object.DestroyImmediate(obj);
        }
        allCreatedObjects.Clear();
        allPieces.Clear();
    }

    // ========== Helpers ==========

    private PieceUI CreatePieceUIStub(string name)
    {
        GameObject uiObj = new GameObject(name);
        allCreatedObjects.Add(uiObj);
        PieceUI ui = uiObj.AddComponent<PieceUI>();
        return ui;
    }

    private string FormatMoves(IEnumerable<(int x, int y)> moves)
    {
        var sb = new System.Text.StringBuilder("[");
        bool first = true;
        foreach (var m in moves)
        {
            if (!first) sb.Append(", ");
            sb.Append($"({m.x},{m.y})");
            first = false;
        }
        sb.Append("]");
        return sb.ToString();
    }
}
