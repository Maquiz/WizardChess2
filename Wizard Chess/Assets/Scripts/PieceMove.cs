using UnityEngine;
using System.Collections;
using DG.Tweening;
using System.Globalization;
using System.Collections.Generic;
using System;

// Default arc height for piece movement animations (in units)
// Adjust this value to change how high pieces hop when moving

//Control of moving from one square to anotehr
public class PieceMove : MonoBehaviour
{

    private int pieceId
    {
        get { return pieceId; }
        set { pieceId = value; }
    }
    public int color, piece;
    private MeshFilter pieceMeshFilter;
    private MeshCollider pieceMeshCollider;
    private MeshRenderer pieceMeshRenderer;
    public int lastx, lasty;
    public int curx, cury;
    public GameObject last;
    public GameObject checker;

    public List<Square> moves = new List<Square>();
    private HashSet<(int x, int y)> moveSet = new HashSet<(int x, int y)>();

    public bool canMove;
    public bool showMoves;
    public bool firstMove;

    private bool isSet;
    public Square curSquare;
    public GameObject Board;
    public GameMaster gm;
    public ElementalPiece elementalPiece;

    private Vector3 hiddenIsland = new Vector3(-1000f, -1000f, -1000f);

    // Animation settings
    public static float DefaultArcHeight = 1.2f;  // Base height for piece hop (increased for knights)
    public static float ArcHeightPerUnit = 0.3f;  // Additional arc height per unit of horizontal distance

    public void createPiece(int _piece, int _color, MeshCollider _mc, MeshFilter _mf, MeshRenderer _mr)
    {
        //InitializeValues
        piece = _piece;
        color = _color;
        pieceMeshCollider = _mc;
        pieceMeshFilter = _mf;
        pieceMeshRenderer = _mr;
    }

    public void Start()
    {
        canMove = true;
        last = new GameObject();
        curx = cury = lastx = lasty = 0;
        isSet = false;
        curSquare = null;
        Board = GameObject.FindGameObjectWithTag("Board");
        gm = GameObject.FindGameObjectWithTag("GM").GetComponent<GameMaster>();
        pieceMeshFilter = this.gameObject.GetComponent<MeshFilter>();
        pieceMeshCollider = this.gameObject.GetComponent<MeshCollider>();
        pieceMeshRenderer = this.gameObject.GetComponent<MeshRenderer>();
        showMoves = false;
        firstMove = true;
    }

    // Note: OnMouseDown was removed - piece selection is handled by GameMaster via input service raycasting

    public void setIntitialPiece(int x, int y, GameObject sq)
    {
        setPieceLocation(x, y);
        setLastPieceLocation(x, y);
        curSquare = sq.GetComponent<Square>();
        curSquare.piece = this;
        curSquare.taken = true;
        last = sq;
        isSet = true;

        // Register with centralized board state
        gm.RegisterPiece(this, x, y);

        ChessMove cm = new ChessMove(this);
        gm.moveHistory.Push(cm);
        gm.moveHistory.Peek().printMove();
        createPieceMoves(piece);
    }

    public void undoMove(ChessMove move) 
    { 
    
    }

    public void movePiece(int x, int y, Square square)
    {
        int fromX = curx;
        int fromY = cury;

        // Physical Movement
        removePieceFromSquare();
        hideMovesHelper();
        Transform t = this.gameObject.transform;
        t.DOKill(); // Kill any existing tweens instead of forcing completion
        // Animate with parabolic arc to avoid clipping through other pieces
        Vector3 destPos = new Vector3(square.gameObject.transform.position.x, t.position.y, square.gameObject.transform.position.z);
        AnimateWithArc(destPos, 0.4f, DefaultArcHeight);

        if (isSet) { firstMove = false; } else { isSet = true; }

        setLastPieceLocation(curx, cury);
        setPieceLocation(x, y);

        // Movement
        square.piece = this;
        square.taken = true;
        curSquare = square;
        last = square.gameObject;

        // Update centralized board state
        gm.UpdateBoardState(this, fromX, fromY, x, y);

        // Handle en passant capture: remove the captured pawn
        if (piece == ChessConstants.PAWN && fromX != x
            && gm.enPassantTarget != null
            && gm.enPassantTarget.x == x && gm.enPassantTarget.y == y)
        {
            PieceMove epCaptured = gm.boardState.GetPieceAt(x, fromY);
            if (epCaptured != null && epCaptured.color != color
                && epCaptured.piece == ChessConstants.PAWN)
            {
                gm.RemovePieceFromBoardState(x, fromY);
                Square epSquare = gm.boardRows[fromY].transform.GetChild(x).GetComponent<Square>();
                if (epSquare != null)
                {
                    epSquare.taken = false;
                    epSquare.piece = null;
                }
                epCaptured.pieceTaken();
            }
        }

        // Handle en passant: set target if pawn moved 2 squares
        if (piece == ChessConstants.PAWN && System.Math.Abs(y - fromY) == 2)
        {
            gm.enPassantTarget = getSquare(x, (fromY + y) / 2);
        }
        else
        {
            gm.enPassantTarget = null;
        }

        // Check for pawn promotion
        checkPromotion();

        // Handle castling: move the rook if this was a castle move
        if (piece == ChessConstants.KING && System.Math.Abs(x - fromX) == 2)
        {
            executeCastle(x > fromX);
        }

        // Elemental passive: after move hook
        if (elementalPiece != null && elementalPiece.passive != null)
        {
            elementalPiece.passive.OnAfterMove(this, fromX, fromY, x, y, gm.boardState);
        }

        ChessMove cm = new ChessMove(this);
        gm.moveHistory.Push(cm);
        gm.moveHistory.Peek().printMove();
        createPieceMoves(piece);
    }

    // ========== Parabolic Arc Animation System ==========

    /// <summary>
    /// Animate piece along a parabolic arc from current position to destination.
    /// Uses DOTween's DOPath with CatmullRom for smooth curved movement.
    /// Arc height scales with horizontal distance so knights clear other pieces.
    /// </summary>
    /// <param name="destination">Target world position</param>
    /// <param name="duration">Animation duration in seconds</param>
    /// <param name="arcHeight">Height of the arc above the higher of start/end Y positions (-1 for auto)</param>
    /// <returns>The DOTween tween for chaining or waiting</returns>
    public Tween AnimateWithArc(Vector3 destination, float duration = 0.4f, float arcHeight = -1f)
    {
        Transform t = this.gameObject.transform;
        Vector3 start = t.position;

        // Calculate horizontal distance (ignoring Y)
        float horizontalDistance = Vector2.Distance(
            new Vector2(start.x, start.z),
            new Vector2(destination.x, destination.z)
        );

        // Auto-calculate arc height based on distance if not specified
        // Knights move ~2.2 units, adjacent pieces move ~1 unit
        if (arcHeight < 0)
        {
            arcHeight = DefaultArcHeight + (horizontalDistance * ArcHeightPerUnit);
        }

        // Calculate apex point at midpoint, raised above the higher Y position
        float apexY = Mathf.Max(start.y, destination.y) + arcHeight;
        Vector3 apex = new Vector3(
            (start.x + destination.x) / 2f,
            apexY,
            (start.z + destination.z) / 2f
        );

        // Create 3-point path: start -> apex -> end
        Vector3[] path = new Vector3[] { start, apex, destination };

        return t.DOPath(path, duration, PathType.CatmullRom)
            .SetEase(Ease.InOutQuad)
            .SetOptions(false); // Don't close the path
    }

    // ========== Multi-Step Move Animation System ==========

    /// <summary>
    /// Animate piece to destination square without updating board state.
    /// Returns the Tween so callers can wait on completion.
    /// Uses parabolic arc animation to avoid clipping through other pieces.
    /// </summary>
    public Tween AnimateToSquare(Square destination, float duration = 0.4f)
    {
        Vector3 targetPos = new Vector3(
            destination.gameObject.transform.position.x,
            this.transform.position.y,
            destination.gameObject.transform.position.z
        );
        return AnimateWithArc(targetPos, duration, DefaultArcHeight);
    }

    /// <summary>
    /// Coroutine that animates piece to destination and yields until animation completes.
    /// Does not update board state - use UpdateBoardStateOnly() separately.
    /// </summary>
    public IEnumerator AnimateToSquareCoroutine(Square destination, float duration = 0.4f)
    {
        Tween tween = AnimateToSquare(destination, duration);
        yield return tween.WaitForCompletion();
    }

    /// <summary>
    /// Update board state and piece position tracking without animation.
    /// Used by MultiStepMoveController to separate state from animation.
    /// </summary>
    public void UpdateBoardStateOnly(int toX, int toY, Square square)
    {
        int fromX = curx;
        int fromY = cury;

        // Update square references
        removePieceFromSquare();

        if (isSet) { firstMove = false; } else { isSet = true; }

        setLastPieceLocation(curx, cury);
        setPieceLocation(toX, toY);

        square.piece = this;
        square.taken = true;
        curSquare = square;
        last = square.gameObject;

        // Update centralized board state
        gm.UpdateBoardState(this, fromX, fromY, toX, toY);
    }

    /// <summary>
    /// Perform a complete animated move with board state update.
    /// Returns a coroutine that completes when animation finishes.
    /// This is the preferred method for multi-step moves.
    /// </summary>
    public IEnumerator MovePieceAnimated(int toX, int toY, Square square, float duration = 0.4f, Action onComplete = null)
    {
        // Update state first
        UpdateBoardStateOnly(toX, toY, square);

        // Then animate
        yield return AnimateToSquareCoroutine(square, duration);

        // Handle post-move hooks (en passant, promotion, castling, passives)
        HandlePostMoveEffects(toX, toY, square);

        // Record move in history
        ChessMove cm = new ChessMove(this);
        gm.moveHistory.Push(cm);
        createPieceMoves(piece);

        onComplete?.Invoke();
    }

    /// <summary>
    /// Handle special move effects (en passant, promotion, castling, passives).
    /// Called after board state is updated but position may still be animating.
    /// </summary>
    private void HandlePostMoveEffects(int toX, int toY, Square square)
    {
        int fromX = lastx;
        int fromY = lasty;

        // Handle en passant capture
        if (piece == ChessConstants.PAWN && fromX != toX
            && gm.enPassantTarget != null
            && gm.enPassantTarget.x == toX && gm.enPassantTarget.y == toY)
        {
            PieceMove epCaptured = gm.boardState.GetPieceAt(toX, fromY);
            if (epCaptured != null && epCaptured.color != color
                && epCaptured.piece == ChessConstants.PAWN)
            {
                gm.RemovePieceFromBoardState(toX, fromY);
                Square epSquare = gm.boardRows[fromY].transform.GetChild(toX).GetComponent<Square>();
                if (epSquare != null)
                {
                    epSquare.taken = false;
                    epSquare.piece = null;
                }
                epCaptured.pieceTaken();
            }
        }

        // Set en passant target if pawn moved 2 squares
        if (piece == ChessConstants.PAWN && System.Math.Abs(toY - fromY) == 2)
        {
            gm.enPassantTarget = getSquare(toX, (fromY + toY) / 2);
        }
        else
        {
            gm.enPassantTarget = null;
        }

        // Check for pawn promotion
        checkPromotion();

        // Handle castling rook movement
        if (piece == ChessConstants.KING && System.Math.Abs(toX - fromX) == 2)
        {
            executeCastle(toX > fromX);
        }

        // Elemental passive: after move hook
        if (elementalPiece != null && elementalPiece.passive != null)
        {
            elementalPiece.passive.OnAfterMove(this, fromX, fromY, toX, toY, gm.boardState);
        }
    }

    public bool checkMoves(int x, int y)
    {
        if (!showMoves)
        {
            showMoves = true;
            showMovesHelper();
        }
        // O(1) lookup using HashSet
        return moveSet.Contains((x, y));
    }

    /// <summary>
    /// Check if a move is valid without showing move indicators.
    /// Use this for UI queries that shouldn't trigger visual side effects.
    /// </summary>
    public bool IsMoveValid(int x, int y)
    {
        return moveSet.Contains((x, y));
    }

    // Debug toggle for move validation logging
    public static bool DebugMoveValidation = false;

    public void createPieceMoves(int piece)
    {
        //1 pawn, 2 rook, 3 knight, 4 bishop, 5 queen, 6 king,
        //Color: 1 Black, 2 White

        moves.Clear();
        MoveRejectionTracker.Clear();

        // Generate pseudo-legal moves based on piece type
        if (piece == ChessConstants.KING)
        {
            createKingMoves();
            addCastlingMoves();
        }
        else if (piece == ChessConstants.QUEEN)
        {
            createQueenMoves();
        }
        else if (piece == ChessConstants.BISHOP)
        {
            createBishopMoves();
        }
        else if (piece == ChessConstants.KNIGHT)
        {
            createKnightMoves();
        }
        else if (piece == ChessConstants.ROOK)
        {
            createRookMoves();
        }
        else
        {
            createPawnMoves();
        }

        // Apply Chilled status effect: halve movement range for sliding pieces
        if (elementalPiece != null && elementalPiece.IsChilled())
        {
            if (piece == ChessConstants.ROOK || piece == ChessConstants.BISHOP || piece == ChessConstants.QUEEN)
            {
                moves = FilterChilledMoves(moves);
            }
        }

        // Elemental passive: modify generated moves (track removed moves)
        if (elementalPiece != null && elementalPiece.passive != null)
        {
            List<Square> beforeModify = new List<Square>(moves);
            moves = elementalPiece.passive.ModifyMoveGeneration(moves, this, gm.boardState);

            // Track moves removed by elemental passive
            HashSet<(int, int)> afterSet = new HashSet<(int, int)>();
            foreach (Square sq in moves) afterSet.Add((sq.x, sq.y));
            foreach (Square sq in beforeModify)
            {
                if (!afterSet.Contains((sq.x, sq.y)))
                {
                    string elementName = GetElementName(elementalPiece.elementId);
                    MoveRejectionTracker.AddRejection(sq.x, sq.y, MoveRejectionReason.ElementalPassiveBlocked,
                        elementName + " passive");
                }
            }
        }

        // Filter moves blocked by square effects (fire, stone walls, etc.)
        if (gm.squareEffectManager != null)
        {
            List<Square> unblocked = new List<Square>();
            foreach (Square move in moves)
            {
                if (!gm.squareEffectManager.IsSquareBlocked(move.x, move.y, this))
                {
                    unblocked.Add(move);
                }
                else
                {
                    string effectName = gm.squareEffectManager.GetBlockingEffectName(move.x, move.y);
                    MoveRejectionTracker.AddRejection(move.x, move.y, MoveRejectionReason.SquareEffectBlocked,
                        effectName);
                }
            }
            moves = unblocked;
        }

        // Filter captures blocked by defender's (or attacker's) passive abilities
        FilterProtectedCaptures();

        // Filter out moves that would leave king in check
        filterIllegalMoves();

        // Debug output
        if (DebugMoveValidation && MoveRejectionTracker.HasRejections())
        {
            LogMoveValidationDebug();
        }
    }

    private string GetElementName(int elementId)
    {
        switch (elementId)
        {
            case ChessConstants.ELEMENT_FIRE: return "Fire";
            case ChessConstants.ELEMENT_EARTH: return "Earth";
            case ChessConstants.ELEMENT_LIGHTNING: return "Lightning";
            case ChessConstants.ELEMENT_ICE: return "Ice";
            case ChessConstants.ELEMENT_SHADOW: return "Shadow";
            default: return "Unknown";
        }
    }

    private void LogMoveValidationDebug()
    {
        UnityEngine.Debug.Log("=== " + printPieceName() + " Move Validation ===");
        UnityEngine.Debug.Log("Legal moves: " + moves.Count);
        UnityEngine.Debug.Log("Rejected squares: " + MoveRejectionTracker.RejectionCount);

        foreach (var kvp in MoveRejectionTracker.CurrentRejections)
        {
            string details = kvp.Value.Details ?? "";
            if (!string.IsNullOrEmpty(details)) details = " - " + details;
            UnityEngine.Debug.Log("  X (" + kvp.Key.Item1 + "," + kvp.Key.Item2 + "): " + kvp.Value.Reason + details);
        }
    }

    /// <summary>
    /// Remove captures that are blocked by the attacker's or defender's passive abilities.
    /// Non-capture moves pass through unchanged. Called before filterIllegalMoves().
    /// </summary>
    private void FilterProtectedCaptures()
    {
        if (gm.boardState == null) return;

        List<Square> allowed = new List<Square>();
        foreach (Square move in moves)
        {
            if (move.taken && move.piece != null && move.piece.color != color)
            {
                PieceMove defender = move.piece;
                bool captureAllowed = true;
                string blockReason = null;

                // Check attacker's passive
                if (elementalPiece != null && elementalPiece.passive != null)
                {
                    if (!elementalPiece.passive.OnBeforeCapture(this, defender, gm.boardState))
                    {
                        captureAllowed = false;
                        string elemName = GetElementName(elementalPiece.elementId);
                        blockReason = "attacker's " + elemName + " passive";
                    }
                }

                // Check defender's passive
                if (captureAllowed && defender.elementalPiece != null && defender.elementalPiece.passive != null)
                {
                    if (!defender.elementalPiece.passive.OnBeforeCapture(this, defender, gm.boardState))
                    {
                        captureAllowed = false;
                        string elemName = GetElementName(defender.elementalPiece.elementId);
                        blockReason = "defender's " + elemName + " passive";
                    }
                }

                if (captureAllowed)
                {
                    allowed.Add(move);
                }
                else
                {
                    // Track the rejection
                    MoveRejectionReason reason = (blockReason != null && blockReason.Contains("attacker"))
                        ? MoveRejectionReason.AttackerCannotCapture
                        : MoveRejectionReason.CaptureProtected;
                    MoveRejectionTracker.AddRejection(move.x, move.y, reason, blockReason);
                }
            }
            else
            {
                allowed.Add(move);
            }
        }
        moves = allowed;
    }

    /// <summary>
    /// Filter moves for Chilled pieces - halve the maximum range in each direction.
    /// Sliding pieces (Rook, Bishop, Queen) have reduced movement when Chilled.
    /// </summary>
    private List<Square> FilterChilledMoves(List<Square> originalMoves)
    {
        // Group moves by direction from current position
        Dictionary<(int, int), List<(Square sq, int dist)>> directionMoves = new Dictionary<(int, int), List<(Square sq, int dist)>>();

        foreach (Square move in originalMoves)
        {
            int dx = move.x - curx;
            int dy = move.y - cury;

            // Normalize direction
            int dirX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
            int dirY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

            var dirKey = (dirX, dirY);
            if (!directionMoves.ContainsKey(dirKey))
            {
                directionMoves[dirKey] = new List<(Square, int)>();
            }

            int distance = UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(dx), UnityEngine.Mathf.Abs(dy));
            directionMoves[dirKey].Add((move, distance));
        }

        List<Square> filteredMoves = new List<Square>();

        foreach (var kvp in directionMoves)
        {
            // Sort by distance
            kvp.Value.Sort((a, b) => a.dist.CompareTo(b.dist));

            // Find max distance in this direction
            int maxDist = kvp.Value.Count > 0 ? kvp.Value[kvp.Value.Count - 1].dist : 0;

            // Halved range (minimum 1)
            int allowedRange = UnityEngine.Mathf.Max(1, maxDist / 2);

            foreach (var (sq, dist) in kvp.Value)
            {
                if (dist <= allowedRange)
                {
                    filteredMoves.Add(sq);
                }
                else
                {
                    MoveRejectionTracker.AddRejection(sq.x, sq.y, MoveRejectionReason.ElementalPassiveBlocked, "Chilled (range halved)");
                }
            }
        }

        return filteredMoves;
    }

    /// <summary>
    /// Remove moves that would leave the king in check.
    /// Also builds HashSet for O(1) move validation.
    /// </summary>
    private void filterIllegalMoves()
    {
        moveSet.Clear();

        if (gm.boardState == null)
        {
            // If no board state, just build the set from existing moves
            foreach (Square move in moves)
            {
                moveSet.Add((move.x, move.y));
            }
            return;
        }

        List<Square> legalMoves = new List<Square>();
        foreach (Square move in moves)
        {
            if (!gm.boardState.WouldMoveLeaveKingInCheck(this, move.x, move.y))
            {
                legalMoves.Add(move);
                moveSet.Add((move.x, move.y));
            }
            else
            {
                MoveRejectionTracker.AddRejection(move.x, move.y, MoveRejectionReason.WouldLeaveKingInCheck);
            }
        }
        moves = legalMoves;
    }

    public void createKingMoves()
    {
        (int x, int y) [] kingSquares = new[] {(0, -1), (0, 1), (1, 0), (1, 1), (1, -1), (-1, 0), (-1, 1), (-1, -1)};
        //No Check checking
        for (int index = 0; index< kingSquares.Length; index++) {
            int nx = curx + kingSquares[index].x;
            int ny = cury + kingSquares[index].y;
            if (isCoordsInBounds(nx) && isCoordsInBounds(ny)) {
                Square curSquareChecker = getSquare(nx, ny);
                if (curSquareChecker != null && curSquareChecker.taken)
                {
                    if (color != curSquareChecker.piece.color)
                    {
                        moves.Add(curSquareChecker);
                    }
                    else
                    {
                        MoveRejectionTracker.AddRejection(nx, ny, MoveRejectionReason.BlockedByFriendlyPiece);
                    }
                }
                else
                {
                    moves.Add(curSquareChecker);
                }
            }
        }
    }

    public void createQueenMoves() {
        createRookMoves();
        createBishopMoves();
    }

    public void createBishopMoves() {
        (int x, int y)[] bishopSquares = new[] { (1, 1), (-1, -1), (1, -1), (-1, 1) };

        for (int index = 0; index < bishopSquares.Length; index++) {
            int i = curx;
            int j = cury;
            bool blocked = false;
            while (isCoordsInBounds(i + bishopSquares[index].x) && isCoordsInBounds(j + bishopSquares[index].y))
            {
                int nx = i + bishopSquares[index].x;
                int ny = j + bishopSquares[index].y;
                Square curSquareChecker = getSquare(nx, ny);
                if (curSquareChecker.taken)
                {
                    if (color != curSquareChecker.piece.color)
                    {
                        moves.Add(curSquareChecker);
                    }
                    else
                    {
                        MoveRejectionTracker.AddRejection(nx, ny, MoveRejectionReason.BlockedByFriendlyPiece);
                    }
                    blocked = true;
                    // Track remaining squares in this direction as blocked by path
                    int pi = nx;
                    int pj = ny;
                    while (isCoordsInBounds(pi + bishopSquares[index].x) && isCoordsInBounds(pj + bishopSquares[index].y))
                    {
                        pi += bishopSquares[index].x;
                        pj += bishopSquares[index].y;
                        MoveRejectionTracker.AddRejection(pi, pj, MoveRejectionReason.BlockedByPiecePath);
                    }
                    break;
                }
                moves.Add(curSquareChecker);
                i += bishopSquares[index].x;
                j += bishopSquares[index].y;
            }
        }
    }

    public void createKnightMoves()
    {
        (int x, int y)[] knightSquares = new[] { (1, 2), (2, 1), (-1, 2), (-2, 1), (1, -2), (2, -1), (-1, -2), (-2, -1) };
        for (int index = 0; index < knightSquares.Length; index++) {
            int nx = curx + knightSquares[index].x;
            int ny = cury + knightSquares[index].y;
            if (isCoordsInBounds(nx) && isCoordsInBounds(ny))
            {
                Square curSquareChecker = getSquare(nx, ny);
                if (curSquareChecker != null && curSquareChecker.taken)
                {
                    if (color != curSquareChecker.piece.color)
                    {
                        moves.Add(curSquareChecker);
                    }
                    else
                    {
                        MoveRejectionTracker.AddRejection(nx, ny, MoveRejectionReason.BlockedByFriendlyPiece);
                    }
                }
                else
                {
                    moves.Add(curSquareChecker);
                }
            }
        }
    }

    public void createRookMoves()
    {
        // Helper to track remaining squares as blocked after hitting a piece
        void trackBlockedPath(int startX, int startY, int dx, int dy)
        {
            int px = startX + dx;
            int py = startY + dy;
            while (isCoordsInBounds(px) && isCoordsInBounds(py))
            {
                MoveRejectionTracker.AddRejection(px, py, MoveRejectionReason.BlockedByPiecePath);
                px += dx;
                py += dy;
            }
        }

        // Direction: +Y
        int i = cury;
        while (isCoordsInBounds(i + 1))
        {
            int ny = i + 1;
            Square curSquareChecker = getSquare(curx, ny);
            if (curSquareChecker.taken)
            {
                if (color != curSquareChecker.piece.color)
                {
                    moves.Add(curSquareChecker);
                }
                else
                {
                    MoveRejectionTracker.AddRejection(curx, ny, MoveRejectionReason.BlockedByFriendlyPiece);
                }
                trackBlockedPath(curx, ny, 0, 1);
                break;
            }
            moves.Add(curSquareChecker);
            i++;
        }

        // Direction: -Y
        i = cury;
        while (isCoordsInBounds(i - 1))
        {
            int ny = i - 1;
            Square curSquareChecker = getSquare(curx, ny);
            if (curSquareChecker.taken)
            {
                if (color != curSquareChecker.piece.color)
                {
                    moves.Add(curSquareChecker);
                }
                else
                {
                    MoveRejectionTracker.AddRejection(curx, ny, MoveRejectionReason.BlockedByFriendlyPiece);
                }
                trackBlockedPath(curx, ny, 0, -1);
                break;
            }
            moves.Add(curSquareChecker);
            i--;
        }

        // Direction: -X
        i = curx;
        while (isCoordsInBounds(i - 1))
        {
            int nx = i - 1;
            Square curSquareChecker = getSquare(nx, cury);
            if (curSquareChecker.taken)
            {
                if (color != curSquareChecker.piece.color)
                {
                    moves.Add(curSquareChecker);
                }
                else
                {
                    MoveRejectionTracker.AddRejection(nx, cury, MoveRejectionReason.BlockedByFriendlyPiece);
                }
                trackBlockedPath(nx, cury, -1, 0);
                break;
            }
            moves.Add(curSquareChecker);
            i--;
        }

        // Direction: +X
        i = curx;
        while (isCoordsInBounds(i + 1))
        {
            int nx = i + 1;
            Square curSquareChecker = getSquare(nx, cury);
            if (curSquareChecker.taken)
            {
                if (color != curSquareChecker.piece.color)
                {
                    moves.Add(curSquareChecker);
                }
                else
                {
                    MoveRejectionTracker.AddRejection(nx, cury, MoveRejectionReason.BlockedByFriendlyPiece);
                }
                trackBlockedPath(nx, cury, 1, 0);
                break;
            }
            moves.Add(curSquareChecker);
            i++;
        }
    }

    public void createPawnMoves()
    {
        // Black (1) moves +y, White (2) moves -y
        int direction = color == ChessConstants.WHITE ? -1 : 1;

        // Forward one square
        if (isCoordsInBounds(cury + direction))
        {
            int ny = cury + direction;
            Square curSquareChecker = getSquare(curx, ny);
            if (curSquareChecker != null && !curSquareChecker.taken)
            {
                moves.Add(curSquareChecker);

                // Forward two squares on first move (only if one square ahead is also clear)
                if (firstMove && isCoordsInBounds(cury + (2 * direction)))
                {
                    int ny2 = cury + (2 * direction);
                    Square twoAhead = getSquare(curx, ny2);
                    if (twoAhead != null && !twoAhead.taken)
                    {
                        moves.Add(twoAhead);
                    }
                    else if (twoAhead != null && twoAhead.taken)
                    {
                        MoveRejectionTracker.AddRejection(curx, ny2, MoveRejectionReason.BlockedByPiecePath);
                    }
                }
            }
            else if (curSquareChecker != null && curSquareChecker.taken)
            {
                // Pawn cannot capture forward
                MoveRejectionTracker.AddRejection(curx, ny, MoveRejectionReason.PawnCannotCaptureForward);
            }
        }

        // Diagonal captures
        if (isCoordsInBounds(cury + direction))
        {
            int ny = cury + direction;

            // Capture to the right
            if (isCoordsInBounds(curx + 1))
            {
                int nx = curx + 1;
                Square curSquareChecker = getSquare(nx, ny);
                if (curSquareChecker != null && curSquareChecker.taken)
                {
                    if (color != curSquareChecker.piece.color)
                    {
                        moves.Add(curSquareChecker);
                    }
                    else
                    {
                        MoveRejectionTracker.AddRejection(nx, ny, MoveRejectionReason.BlockedByFriendlyPiece);
                    }
                }
            }

            // Capture to the left
            if (isCoordsInBounds(curx - 1))
            {
                int nx = curx - 1;
                Square curSquareChecker = getSquare(nx, ny);
                if (curSquareChecker != null && curSquareChecker.taken)
                {
                    if (color != curSquareChecker.piece.color)
                    {
                        moves.Add(curSquareChecker);
                    }
                    else
                    {
                        MoveRejectionTracker.AddRejection(nx, ny, MoveRejectionReason.BlockedByFriendlyPiece);
                    }
                }
            }
        }

        // En passant captures
        addEnPassantMoves(direction);
    }

    /// <summary>
    /// Add en passant capture moves if available.
    /// </summary>
    private void addEnPassantMoves(int direction)
    {
        if (gm.enPassantTarget == null) return;

        // En passant is available if:
        // 1. Target square is diagonally adjacent (one forward, one sideways)
        // 2. We're on the correct rank (rank 5 for white, rank 4 for black - 0-indexed: 3 or 4)
        int enPassantRank = (color == ChessConstants.WHITE) ? 3 : 4;
        if (cury != enPassantRank) return;

        if (gm.enPassantTarget.y == cury + direction &&
            System.Math.Abs(gm.enPassantTarget.x - curx) == 1)
        {
            moves.Add(gm.enPassantTarget);
        }
    }

    public void returnpiece()
    {
        Transform t = this.transform;
        t.DOMove(new Vector3(last.transform.position.x, last.transform.position.y + 6f, last.transform.position.z), .5f);
        //  t.DOMove(new Vector3(last.transform.position.x, last.transform.position.y, last.transform.position.z), .3f);
    }

    public void pieceTaken()
    {
        // Remove from board state
        gm.RemovePieceFromBoardState(curx, cury);

        Transform t = this.gameObject.transform;
        t.DOMove(hiddenIsland, .1f);
        t.DOComplete();
        curSquare.taken = false;
    }

    public void setLastPieceLocation(int x, int y)
    {
        lastx = x;
        lasty = y;
    }

    public void setPieceLocation(int x, int y)
    {
        curx = x;
        cury = y;
    }

    public bool getIsSet()
    {
        return isSet;
    }

    public void removePieceFromSquare()
    {
        curSquare.taken = false;
        curSquare.piece = null;
    }

    public Square getSquare(int x, int y)
    {
        if (isCoordsInBounds(x) && isCoordsInBounds(y))
        {
            return gm.boardRows[y].gameObject.transform.GetChild(x).gameObject.GetComponent<Square>();
        }
        else return null;
    }

    public void showMovesHelper()
    {
        foreach (Square move in moves)
        {
            getSquare(move.x, move.y).showMoveSquare.SetActive(true);
        }
    }

    public void hideMovesHelper()
    {
        foreach (Square move in moves)
        {
            getSquare(move.x, move.y).showMoveSquare.SetActive(false);
        }
        showMoves = false;
    }

    public void printMovesList()
    {
        Debug.Log("***********************MOVESLIST*START**************");
        foreach (Square move in moves)
        {
            Debug.Log(move.x + ", " + move.y + " ");
        }
        Debug.Log("***********************END**************************");
    }
    public bool isCoordsInBounds(int x)
    {
        if (x < gm.boardSize && x >= 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //Piece and Square Names
    public string printPieceName()
    {
        string outString = "";
        if (color == 1)
        {
            outString += "Black ";
        }
        else if (color == 2)
        {
            outString += "White ";
        }

        if (piece == 1)
        {
            outString += "Pawn";
        }
        else if (piece == 2)
        {
            outString += "Rook";
        }
        else if (piece == 3)
        {
            outString += "Knight";
        }
        else if (piece == 4)
        {
            outString += "Bishop";
        }
        else if (piece == 5)
        {
            outString += "Queen";
        }
        else if (piece == 6)
        {
            outString += "King";
        }
        return outString;
    }

    public string printSquare(int x, int y)
    {
        string outString = "";
        outString += (char)(65 + x);
        outString += y + 1;
        return outString;
    }

    // ========== Castling ==========

    /// <summary>
    /// Add castling moves if available.
    /// </summary>
    private void addCastlingMoves()
    {
        int homeRank = (color == ChessConstants.WHITE) ? 7 : 0;

        // Track rejections for kingside castle square (g-file)
        int kingsideCastleX = curx + 2;
        // Track rejections for queenside castle square (c-file)
        int queensideCastleX = curx - 2;

        if (!firstMove)
        {
            MoveRejectionTracker.AddRejection(kingsideCastleX, homeRank, MoveRejectionReason.CastlingKingMoved);
            MoveRejectionTracker.AddRejection(queensideCastleX, homeRank, MoveRejectionReason.CastlingKingMoved);
            return;
        }
        if (gm.boardState == null) return;
        if (gm.boardState.IsKingInCheck(color))
        {
            MoveRejectionTracker.AddRejection(kingsideCastleX, homeRank, MoveRejectionReason.CastlingInCheck);
            MoveRejectionTracker.AddRejection(queensideCastleX, homeRank, MoveRejectionReason.CastlingInCheck);
            return;
        }

        if (cury != homeRank) return;

        // Kingside castle (O-O)
        MoveRejectionReason kingsideRejection;
        if (canCastleKingsideWithReason(homeRank, out kingsideRejection))
        {
            Square castleSquare = getSquare(kingsideCastleX, homeRank);
            if (castleSquare != null)
            {
                moves.Add(castleSquare);
            }
        }
        else if (kingsideRejection != MoveRejectionReason.None)
        {
            MoveRejectionTracker.AddRejection(kingsideCastleX, homeRank, kingsideRejection);
        }

        // Queenside castle (O-O-O)
        MoveRejectionReason queensideRejection;
        if (canCastleQueensideWithReason(homeRank, out queensideRejection))
        {
            Square castleSquare = getSquare(queensideCastleX, homeRank);
            if (castleSquare != null)
            {
                moves.Add(castleSquare);
            }
        }
        else if (queensideRejection != MoveRejectionReason.None)
        {
            MoveRejectionTracker.AddRejection(queensideCastleX, homeRank, queensideRejection);
        }
    }

    private bool canCastleKingsideWithReason(int homeRank, out MoveRejectionReason rejection)
    {
        rejection = MoveRejectionReason.None;

        // Check rook is in place and hasn't moved
        PieceMove rook = gm.boardState.GetPieceAt(7, homeRank);
        if (rook == null || rook.piece != ChessConstants.ROOK || rook.color != color || !rook.firstMove)
        {
            rejection = MoveRejectionReason.CastlingRookMoved;
            return false;
        }

        // Check squares between king and rook are empty
        if (!gm.boardState.IsSquareEmpty(5, homeRank) || !gm.boardState.IsSquareEmpty(6, homeRank))
        {
            rejection = MoveRejectionReason.CastlingPathBlocked;
            return false;
        }

        // Check king doesn't pass through or end in check
        int opponentColor = (color == ChessConstants.WHITE) ? ChessConstants.BLACK : ChessConstants.WHITE;
        if (gm.boardState.IsSquareAttackedBy(5, homeRank, opponentColor) ||
            gm.boardState.IsSquareAttackedBy(6, homeRank, opponentColor))
        {
            rejection = MoveRejectionReason.CastlingThroughCheck;
            return false;
        }

        return true;
    }

    private bool canCastleKingside(int homeRank)
    {
        MoveRejectionReason unused;
        return canCastleKingsideWithReason(homeRank, out unused);
    }

    private bool canCastleQueensideWithReason(int homeRank, out MoveRejectionReason rejection)
    {
        rejection = MoveRejectionReason.None;

        // Check rook is in place and hasn't moved
        PieceMove rook = gm.boardState.GetPieceAt(0, homeRank);
        if (rook == null || rook.piece != ChessConstants.ROOK || rook.color != color || !rook.firstMove)
        {
            rejection = MoveRejectionReason.CastlingRookMoved;
            return false;
        }

        // Check squares between king and rook are empty
        if (!gm.boardState.IsSquareEmpty(1, homeRank) ||
            !gm.boardState.IsSquareEmpty(2, homeRank) ||
            !gm.boardState.IsSquareEmpty(3, homeRank))
        {
            rejection = MoveRejectionReason.CastlingPathBlocked;
            return false;
        }

        // Check king doesn't pass through or end in check
        int opponentColor = (color == ChessConstants.WHITE) ? ChessConstants.BLACK : ChessConstants.WHITE;
        if (gm.boardState.IsSquareAttackedBy(2, homeRank, opponentColor) ||
            gm.boardState.IsSquareAttackedBy(3, homeRank, opponentColor))
        {
            rejection = MoveRejectionReason.CastlingThroughCheck;
            return false;
        }

        return true;
    }

    private bool canCastleQueenside(int homeRank)
    {
        MoveRejectionReason unused;
        return canCastleQueensideWithReason(homeRank, out unused);
    }

    /// <summary>
    /// Execute the rook movement for castling.
    /// </summary>
    private void executeCastle(bool kingside)
    {
        int homeRank = cury;
        int rookFromX = kingside ? 7 : 0;
        int rookToX = kingside ? 5 : 3;

        PieceMove rook = gm.boardState.GetPieceAt(rookFromX, homeRank);
        if (rook != null)
        {
            Square rookDestSquare = getSquare(rookToX, homeRank);
            if (rookDestSquare != null)
            {
                // Update visual position with arc animation
                rook.removePieceFromSquare();
                Transform t = rook.gameObject.transform;
                Vector3 rookDest = new Vector3(rookDestSquare.gameObject.transform.position.x, t.position.y, rookDestSquare.gameObject.transform.position.z);
                rook.AnimateWithArc(rookDest, 0.5f, DefaultArcHeight);

                // Update state
                rook.setLastPieceLocation(rook.curx, rook.cury);
                rook.setPieceLocation(rookToX, homeRank);
                rookDestSquare.piece = rook;
                rookDestSquare.taken = true;
                rook.curSquare = rookDestSquare;
                rook.last = rookDestSquare.gameObject;
                rook.firstMove = false;

                // Update board state
                gm.UpdateBoardState(rook, rookFromX, homeRank, rookToX, homeRank);
            }
        }
    }

    // ========== Pawn Promotion ==========

    /// <summary>
    /// Check if pawn has reached promotion rank and promote it.
    /// </summary>
    private void checkPromotion()
    {
        if (piece != ChessConstants.PAWN) return;

        int promotionRank = (color == ChessConstants.WHITE) ? 0 : 7;
        if (cury == promotionRank)
        {
            // Auto-promote to Queen for now
            // TODO: Add UI for piece selection
            PromoteTo(ChessConstants.QUEEN);
            Debug.Log("Pawn promoted to Queen!");
        }
    }

    /// <summary>
    /// Promote pawn to a new piece type. Updates logic, visuals, board state, and elemental abilities.
    /// </summary>
    public void PromoteTo(int newPieceType)
    {
        piece = newPieceType;

        // Log promotion to game log
        GameLogUI.Log("<color=#44FF44>  → Promoted to " + GameLogUI.ShortPieceName(newPieceType) + "!</color>");

        // Visual mesh swap
        SwapMesh(newPieceType);

        // BoardState attack maps now see the new piece type
        if (gm != null && gm.boardState != null)
            gm.boardState.RecalculateAttacks();

        // Re-initialize elemental abilities for the new piece type
        if (elementalPiece != null && elementalPiece.elementId != ChessConstants.ELEMENT_NONE)
        {
            int elementId = elementalPiece.elementId;
            var newPassive = AbilityFactory.CreatePassive(elementId, newPieceType);
            var newActive = AbilityFactory.CreateActive(elementId, newPieceType);
            int newCooldown = AbilityFactory.GetCooldown(elementId, newPieceType);
            elementalPiece.Init(elementId, newPassive, newActive, newCooldown);
        }

        createPieceMoves(piece);
    }

    /// <summary>
    /// Swap the visual mesh and material to match a new piece type using prefabs from Resources/PromotionPrefabs.
    /// </summary>
    private void SwapMesh(int newPieceType)
    {
        string prefabPath = GetPromotionPrefabPath(newPieceType, color);
        if (prefabPath == null) return;
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab == null) return;

        MeshFilter prefabMF = prefab.GetComponent<MeshFilter>();
        if (prefabMF != null && prefabMF.sharedMesh != null)
        {
            if (pieceMeshFilter == null) pieceMeshFilter = GetComponent<MeshFilter>();
            if (pieceMeshFilter == null) return;
            pieceMeshFilter.sharedMesh = prefabMF.sharedMesh;
            if (pieceMeshCollider != null)
                pieceMeshCollider.sharedMesh = prefabMF.sharedMesh;
        }

        // Swap material to match the new piece type's appearance
        MeshRenderer prefabMR = prefab.GetComponent<MeshRenderer>();
        if (prefabMR != null && prefabMR.sharedMaterial != null)
        {
            if (pieceMeshRenderer == null) pieceMeshRenderer = GetComponent<MeshRenderer>();
            if (pieceMeshRenderer != null)
            {
                pieceMeshRenderer.material = prefabMR.sharedMaterial;

                // Re-apply element tint on the new material
                var indicatorUI = GetComponent<ElementIndicatorUI>();
                if (indicatorUI != null)
                    indicatorUI.ReapplyTint();
            }
        }
    }

    private static string GetPromotionPrefabPath(int pieceType, int pieceColor)
    {
        string suffix = pieceColor == ChessConstants.BLACK ? "Dark" : "Light";
        switch (pieceType)
        {
            case ChessConstants.QUEEN: return "PromotionPrefabs/Queen" + suffix;
            case ChessConstants.ROOK: return "PromotionPrefabs/Rook" + suffix;
            case ChessConstants.BISHOP: return "PromotionPrefabs/Bishop" + suffix;
            case ChessConstants.KNIGHT: return "PromotionPrefabs/Knight" + suffix;
            default: return null;
        }
    }
}