using UnityEngine;
using System.Collections;
using DG.Tweening;
using System.Globalization;
using System.Collections.Generic;

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

    void OnMouseDown()
    {
        //Check if you are taking or piece if the player color = piece color
        if (!gm.isPieceSelected && gm.currentMove == color)
        {
            // Stunned pieces cannot move (but can still use active abilities)
            if (elementalPiece != null && elementalPiece.IsStunned())
            {
                Debug.Log(printPieceName() + " is stunned and cannot move!");
                return;
            }

            gm.selectPiece(this.gameObject.transform, this);
        }
    }

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
        t.DOPause();
        t.DOMove(new Vector3(this.transform.position.x, 1, this.transform.position.z), .5f);
        t.DOComplete();
        t.DOMove(new Vector3(square.gameObject.transform.position.x, this.transform.position.y, square.gameObject.transform.position.z), .5f);
        t.DOComplete();

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

    public void createPieceMoves(int piece)
    {
        //1 pawn, 2 rook, 3 knight, 4 bishop, 5 queen, 6 king,
        //Color: 1 Black, 2 White

        moves.Clear();

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

        // Elemental passive: modify generated moves
        if (elementalPiece != null && elementalPiece.passive != null)
        {
            moves = elementalPiece.passive.ModifyMoveGeneration(moves, this, gm.boardState);
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
            }
            moves = unblocked;
        }

        // Filter captures blocked by defender's (or attacker's) passive abilities
        FilterProtectedCaptures();

        // Filter out moves that would leave king in check
        filterIllegalMoves();
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

                // Check attacker's passive
                if (elementalPiece != null && elementalPiece.passive != null)
                {
                    if (!elementalPiece.passive.OnBeforeCapture(this, defender, gm.boardState))
                        captureAllowed = false;
                }

                // Check defender's passive
                if (captureAllowed && defender.elementalPiece != null && defender.elementalPiece.passive != null)
                {
                    if (!defender.elementalPiece.passive.OnBeforeCapture(this, defender, gm.boardState))
                        captureAllowed = false;
                }

                if (captureAllowed)
                    allowed.Add(move);
            }
            else
            {
                allowed.Add(move);
            }
        }
        moves = allowed;
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
        }
        moves = legalMoves;
    }

    public void createKingMoves()
    {
        (int x, int y) [] kingSquares = new[] {(0, -1), (0, 1), (1, 0), (1, 1), (1, -1), (-1, 0), (-1, 1), (-1, -1)};
        //No Check checking
        for (int index = 0; index< kingSquares.Length; index++) {
            if (isCoordsInBounds(curx + kingSquares[index].x) && isCoordsInBounds(cury + kingSquares[index].y)) {
                Square curSquareChecker = getSquare(curx + kingSquares[index].x, cury + kingSquares[index].y);
                if (curSquareChecker != null && curSquareChecker.taken)
                {
                    if (color != curSquareChecker.piece.color)
                    {
                        moves.Add(curSquareChecker);
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
            while (isCoordsInBounds(i + bishopSquares[index].x) && isCoordsInBounds(j + bishopSquares[index].y))
            {
                Square curSquareChecker = getSquare(i + bishopSquares[index].x, j + bishopSquares[index].y);
                if (curSquareChecker.taken)
                {
                    if (color != curSquareChecker.piece.color)
                    {
                        moves.Add(curSquareChecker);
                        break;
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
            if (isCoordsInBounds(curx + knightSquares[index].x) && isCoordsInBounds(cury + knightSquares[index].y))
            {
                Square curSquareChecker = getSquare(curx + knightSquares[index].x, cury + knightSquares[index].y);
                if (curSquareChecker != null && curSquareChecker.taken)
                {
                    if (color != curSquareChecker.piece.color)
                    {
                        moves.Add(curSquareChecker);
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
        int i = cury;
        while (isCoordsInBounds(i + 1))
        {

            Square curSquareChecker = getSquare(curx, i + 1);
            if (curSquareChecker.taken)
            {
                if (color != curSquareChecker.piece.color)
                {
                    moves.Add(curSquareChecker);
                    break;
                }
                break;
            }
            moves.Add(curSquareChecker);
            i++;

        }

        i = cury;
        while (isCoordsInBounds(i - 1))
        {
            Square curSquareChecker = getSquare(curx, i - 1);
            if (curSquareChecker.taken)
            {
                if (color != curSquareChecker.piece.color)
                {
                    moves.Add(curSquareChecker);
                    break;
                }
                break;
            }
            moves.Add(curSquareChecker);
            i--;
        }

        i = curx;
        while (isCoordsInBounds(i - 1))
        {
            Square curSquareChecker = getSquare(i - 1, cury);
            if (curSquareChecker.taken)
            {
                if (color != curSquareChecker.piece.color)
                {
                    moves.Add(curSquareChecker);
                    break;
                }
                break;
            }
            moves.Add(curSquareChecker);
            i--;
        }

        i = curx;
        while (isCoordsInBounds(i + 1))//!getSquare(i, cury).taken || 
        {
            Square curSquareChecker = getSquare(i + 1, cury);
            if (curSquareChecker.taken)
            {
                if (color != curSquareChecker.piece.color)
                {
                    moves.Add(curSquareChecker);
                    break;
                }
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
            Square curSquareChecker = getSquare(curx, cury + direction);
            if (curSquareChecker != null && !curSquareChecker.taken)
            {
                moves.Add(curSquareChecker);

                // Forward two squares on first move (only if one square ahead is also clear)
                if (firstMove && isCoordsInBounds(cury + (2 * direction)))
                {
                    Square twoAhead = getSquare(curx, cury + (2 * direction));
                    if (twoAhead != null && !twoAhead.taken)
                    {
                        moves.Add(twoAhead);
                    }
                }
            }
        }

        // Diagonal captures
        if (isCoordsInBounds(cury + direction))
        {
            // Capture to the right
            if (isCoordsInBounds(curx + 1))
            {
                Square curSquareChecker = getSquare(curx + 1, cury + direction);
                if (curSquareChecker != null && curSquareChecker.taken)
                {
                    if (color != curSquareChecker.piece.color)
                    {
                        moves.Add(curSquareChecker);
                    }
                }
            }

            // Capture to the left
            if (isCoordsInBounds(curx - 1))
            {
                Square curSquareChecker = getSquare(curx - 1, cury + direction);
                if (curSquareChecker != null && curSquareChecker.taken)
                {
                    if (color != curSquareChecker.piece.color)
                    {
                        moves.Add(curSquareChecker);
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
        if (!firstMove) return;
        if (gm.boardState == null) return;
        if (gm.boardState.IsKingInCheck(color)) return;

        int homeRank = (color == ChessConstants.WHITE) ? 7 : 0;
        if (cury != homeRank) return;

        // Kingside castle (O-O)
        if (canCastleKingside(homeRank))
        {
            Square castleSquare = getSquare(curx + 2, homeRank);
            if (castleSquare != null)
            {
                moves.Add(castleSquare);
            }
        }

        // Queenside castle (O-O-O)
        if (canCastleQueenside(homeRank))
        {
            Square castleSquare = getSquare(curx - 2, homeRank);
            if (castleSquare != null)
            {
                moves.Add(castleSquare);
            }
        }
    }

    private bool canCastleKingside(int homeRank)
    {
        // Check rook is in place and hasn't moved
        PieceMove rook = gm.boardState.GetPieceAt(7, homeRank);
        if (rook == null || rook.piece != ChessConstants.ROOK || rook.color != color || !rook.firstMove)
            return false;

        // Check squares between king and rook are empty
        if (!gm.boardState.IsSquareEmpty(5, homeRank) || !gm.boardState.IsSquareEmpty(6, homeRank))
            return false;

        // Check king doesn't pass through or end in check
        int opponentColor = (color == ChessConstants.WHITE) ? ChessConstants.BLACK : ChessConstants.WHITE;
        if (gm.boardState.IsSquareAttackedBy(5, homeRank, opponentColor) ||
            gm.boardState.IsSquareAttackedBy(6, homeRank, opponentColor))
            return false;

        return true;
    }

    private bool canCastleQueenside(int homeRank)
    {
        // Check rook is in place and hasn't moved
        PieceMove rook = gm.boardState.GetPieceAt(0, homeRank);
        if (rook == null || rook.piece != ChessConstants.ROOK || rook.color != color || !rook.firstMove)
            return false;

        // Check squares between king and rook are empty
        if (!gm.boardState.IsSquareEmpty(1, homeRank) ||
            !gm.boardState.IsSquareEmpty(2, homeRank) ||
            !gm.boardState.IsSquareEmpty(3, homeRank))
            return false;

        // Check king doesn't pass through or end in check
        int opponentColor = (color == ChessConstants.WHITE) ? ChessConstants.BLACK : ChessConstants.WHITE;
        if (gm.boardState.IsSquareAttackedBy(2, homeRank, opponentColor) ||
            gm.boardState.IsSquareAttackedBy(3, homeRank, opponentColor))
            return false;

        return true;
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
                // Update visual position
                rook.removePieceFromSquare();
                Transform t = rook.gameObject.transform;
                t.DOMove(new Vector3(rookDestSquare.gameObject.transform.position.x, t.position.y, rookDestSquare.gameObject.transform.position.z), .5f);

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