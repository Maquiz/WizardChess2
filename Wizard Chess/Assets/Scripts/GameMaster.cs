using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Game state for tracking win/draw conditions
/// </summary>
public enum GameState
{
    Playing,
    WhiteInCheck,
    BlackInCheck,
    WhiteWins,      // Black is checkmated
    BlackWins,      // White is checkmated
    Stalemate,
    Draw
}

[System.Serializable]
public class GameMaster : MonoBehaviour
{

    //Physical Board Components
    //Player Controlled - Can B more than 2 colors
    public GameObject[] BPieces;
    //Black Piece Array
    public GameObject[] WPieces;
    //white Piece Array
    [SerializeField] public GameObject[,] boardPos;
    public GameObject[] boardRows;
    public GameObject Board;
    private Vector3 hiddenIsland = new Vector3(-1000f, -1000f, -1000f);
    public int boardSize;
    public int currentMove = 2;
    //Assuming the board is 8x8
    public string[] letters = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K" };
    enum PieceType
    {
        PAWN, ROOK, KNIGHT, BISHOP, QUEEN, KING
    }
    enum PieceColor
    {
        BLACK, WHITE
    }

    //UI
    public Canvas boardUI;
    enum MouseUI
    {
        CANMOVE, CANTMOVE, TAKEPIECE, START
    }

    public PieceUI selectedUI, canMoveUI, cantMoveUI, takeMoveUI;
    public LineRenderer lr;
    public Material lineMaterial;

    //Logic Vars
    public bool isPieceSelected;
    public PieceMove selectedPiece;
    public Stack<ChessMove> moveHistory;

    // Chess rules state
    public BoardState boardState;
    public GameState currentGameState = GameState.Playing;
    public Square enPassantTarget = null;  // Square that can be captured en passant

    // Wizard chess systems
    public SquareEffectManager squareEffectManager;
    public AbilityExecutor abilityExecutor;
    public int turnNumber = 0;
    public bool isDraftPhase = false;

    //Instantiated objects
    public GameObject blackSquare;
    public GameObject whiteSquare;
    public GameObject[] pieces;
    public GameObject[] pieceUI;
    private Transform lastHittedObject = null;
    public bool showMoves;

    void Start()
    {
        boardPos = new GameObject[boardSize, boardSize];
        boardSize = 8;
        createBoard(boardSize);
        moveHistory = new Stack<ChessMove>();
        lr = this.gameObject.AddComponent<LineRenderer>();
        swapUIIcon(MouseUI.START);

        // Initialize board state manager
        boardState = new BoardState();
        currentGameState = GameState.Playing;
        currentMove = ChessConstants.WHITE; // Ensure White always goes first (overrides Inspector)

        // Initialize wizard chess systems
        squareEffectManager = this.gameObject.AddComponent<SquareEffectManager>();
        squareEffectManager.Init(this);
        abilityExecutor = this.gameObject.AddComponent<AbilityExecutor>();
        abilityExecutor.Init(this, squareEffectManager);

        // Element setup: use deck system if configured by menu, else default Fire vs Earth
        if (MatchConfig.useDeckSystem && MatchConfig.draftData != null)
        {
            DeckBasedSetup deckSetup = this.gameObject.AddComponent<DeckBasedSetup>();
            deckSetup.Init(this, MatchConfig.draftData);
        }
        else
        {
            FireVsEarthSetup setup = this.gameObject.AddComponent<FireVsEarthSetup>();
            setup.Init(this);
        }

        // Tooltip UI for mouse-over piece info
        this.gameObject.AddComponent<PieceTooltipUI>();

        // Ability mode visual indicator
        this.gameObject.AddComponent<AbilityModeUI>();

        // Check banner and game over UI
        this.gameObject.AddComponent<CheckBannerUI>();
        this.gameObject.AddComponent<GameOverUI>();

        // Game log panel
        GameLogUI logUI = this.gameObject.AddComponent<GameLogUI>();
        logUI.Init(this);

        // AI opponent
        if (MatchConfig.isAIMatch)
        {
            ChessAI ai = this.gameObject.AddComponent<ChessAI>();
            ai.Init(this, MatchConfig.aiDifficulty, MatchConfig.aiColor);
        }
    }

    /// <summary>
    /// Register a piece with the board state (called by pieces during initialization)
    /// </summary>
    public void RegisterPiece(PieceMove piece, int x, int y)
    {
        if (boardState != null)
        {
            boardState.SetPieceAt(x, y, piece);
            boardState.RecalculateAttacks();
        }
    }

    /// <summary>
    /// Update board state after a move
    /// </summary>
    public void UpdateBoardState(PieceMove piece, int fromX, int fromY, int toX, int toY)
    {
        if (boardState != null)
        {
            boardState.MovePiece(fromX, fromY, toX, toY);
            boardState.RecalculateAttacks();
        }
    }

    /// <summary>
    /// Remove a piece from board state (for captures)
    /// </summary>
    public void RemovePieceFromBoardState(int x, int y)
    {
        if (boardState != null)
        {
            boardState.RemovePiece(x, y);
            boardState.RecalculateAttacks();
        }
    }


    //Game LOOP
    void Update()
    {
        // Don't allow moves if game is over or in draft phase
        if (isDraftPhase) return;
        if (currentGameState == GameState.WhiteWins ||
            currentGameState == GameState.BlackWins ||
            currentGameState == GameState.Stalemate ||
            currentGameState == GameState.Draw)
        {
            return;
        }

        // Block human input during AI turn
        ChessAI ai = GetComponent<ChessAI>();
        if (ai != null && ai.IsAITurn())
        {
            return;
        }

        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Handle ability mode
        if (abilityExecutor != null && abilityExecutor.isInAbilityMode)
        {
            // Q or Right-click: cancel ability mode, return to normal move selection
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1))
            {
                abilityExecutor.ExitAbilityMode();
                // Return to normal move mode (don't deselect the piece)
                if (selectedPiece != null)
                {
                    selectedPiece.createPieceMoves(selectedPiece.piece);
                    selectedPiece.showMoves = false;
                    Debug.Log("[Ability] Cancelled. Back to normal moves.");
                }
                return;
            }

            // LineRenderer: draw from piece to cursor (same as normal mode)
            setUpLine();
            lr.SetPosition(1, Camera.main.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, 8.5f)));
            lr.alignment = LineAlignment.View;

            // Single raycast per frame for hover icons + click handling
            if (Physics.Raycast(ray, out hit))
            {
                Square s = null;
                if (hit.collider.gameObject.tag == "Board")
                    s = hit.collider.gameObject.GetComponent<Square>();
                else if (hit.collider.gameObject.tag == "Piece")
                {
                    PieceMove p = hit.collider.gameObject.GetComponent<PieceMove>();
                    if (p != null) s = p.curSquare;
                }

                if (s != null && abilityExecutor.IsValidTarget(s.x, s.y))
                {
                    if (s.taken && s.piece != null && s.piece.color != currentMove)
                        swapUIIcon(MouseUI.TAKEPIECE);
                    else
                        swapUIIcon(MouseUI.CANMOVE);

                    if (Input.GetMouseButtonDown(0))
                    {
                        if (abilityExecutor.TryExecuteOnSquare(s.x, s.y))
                        {
                            GameLogUI.LogAbility(turnNumber, currentMove, selectedPiece, s.x, s.y);
                            deSelectPiece();
                            EndTurn();
                        }
                    }
                }
                else
                {
                    swapUIIcon(MouseUI.CANTMOVE);
                    if (Input.GetMouseButtonDown(0))
                    {
                        Debug.Log("[Ability] Invalid target. Click a highlighted square, or press Q / right-click to cancel.");
                    }
                }
            }
            else
            {
                swapUIIcon(MouseUI.CANTMOVE);
            }
            return;
        }

        if (isPieceSelected)
        {
            if (Input.GetMouseButtonDown(1))
            {
                selectedPiece.hideMovesHelper();
                deSelectPiece();
                return;
            }

            // Q key: activate ability if piece has one ready
            if (Input.GetKeyDown(KeyCode.Q) && selectedPiece != null && selectedPiece.elementalPiece != null)
            {
                ElementalPiece ep = selectedPiece.elementalPiece;
                if (ep.active != null && ep.cooldown != null && ep.cooldown.IsReady
                    && ep.active.CanActivate(selectedPiece, boardState, squareEffectManager))
                {
                    selectedPiece.hideMovesHelper();
                    EnterAbilityMode(selectedPiece);
                    Debug.Log("[Ability] Entered ability mode for " + selectedPiece.printPieceName());
                    return;
                }
                else if (ep.active != null && ep.cooldown != null && !ep.cooldown.IsReady)
                {
                    Debug.Log("[Ability] " + selectedPiece.printPieceName() + " ability on cooldown: " + ep.cooldown.CurrentCooldown + " turns remaining");
                }
            }

            //Line renderer
            setUpLine();
            lr.SetPosition(1, Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 8.5f)));
            lr.alignment = LineAlignment.View;

            Transform lastHittedObject = null;
            if (Physics.Raycast(ray, out hit))
            {
                if (lastHittedObject != hit.collider.transform)
                {
                    //Selecting a piece
                    if (hit.collider.gameObject.tag == "Piece")
                    {
                        PieceMove p = hit.collider.gameObject.GetComponent<PieceMove>();
                        if ((p.color != selectedPiece.color) && selectedPiece.checkMoves(p.curx, p.cury))
                        {
                            swapUIIcon(MouseUI.TAKEPIECE);
                            if (Input.GetMouseButtonDown(0))
                            {
                                if (TryCapture(selectedPiece, p))
                                {
                                    GameLogUI.LogCapture(turnNumber, currentMove, selectedPiece, p, p.curx, p.cury);
                                    selectedPiece.movePiece(p.curx, p.cury, p.curSquare);
                                    deSelectPiece();
                                    EndTurn();
                                }
                            }
                        }
                        // Click own piece while another is selected: switch selection
                        else if (p.color == selectedPiece.color && Input.GetMouseButtonDown(0))
                        {
                            selectedPiece.hideMovesHelper();
                            deSelectPiece();
                            bool canSelect = p.elementalPiece == null || !p.elementalPiece.IsStunned();
                            if (canSelect)
                            {
                                selectPiece(p.gameObject.transform, p);
                            }
                        }
                    }

                    else if (hit.collider.gameObject.tag == "Board")
                    {
                        Square s = hit.collider.gameObject.GetComponent<Square>();
                        if (selectedPiece != null)
                        {
                            if (s.taken && (s.piece.color != selectedPiece.color) && selectedPiece.checkMoves(s.x, s.y))
                            {
                                swapUIIcon(MouseUI.TAKEPIECE);
                                if (Input.GetMouseButtonDown(0))
                                {
                                    if (TryCapture(selectedPiece, s.piece))
                                    {
                                        GameLogUI.LogCapture(turnNumber, currentMove, selectedPiece, s.piece, s.x, s.y);
                                        selectedPiece.movePiece(s.piece.curx, s.piece.cury, s.piece.curSquare);
                                        deSelectPiece();
                                        EndTurn();
                                    }
                                }
                            }

                            else if (selectedPiece.checkMoves(s.x, s.y))
                            {
                                swapUIIcon(MouseUI.CANMOVE);
                                if (Input.GetMouseButtonDown(0))
                                {
                                    GameLogUI.LogPieceMove(turnNumber, currentMove, selectedPiece, s.x, s.y);
                                    selectedPiece.movePiece(s.x, s.y, s);
                                    deSelectPiece();
                                    EndTurn();
                                }
                            }
                            else
                            {
                                swapUIIcon(MouseUI.CANTMOVE);
                            }
                        }
                    }
                    else
                    {
                        swapUIIcon(MouseUI.CANTMOVE);
                    }
                    lastHittedObject = hit.collider.transform;
                }

                else if (lastHittedObject) {
                    lastHittedObject = null;
                }
            }
        }
    }

    /// <summary>
    /// End the current turn: swap player, tick effects/cooldowns, evaluate game state.
    /// </summary>
    public void EndTurn()
    {
        currentMove = currentMove == 1 ? 2 : 1;
        turnNumber++;

        // Tick square effects
        if (squareEffectManager != null)
        {
            squareEffectManager.TickAllEffects();
        }

        // Notify all pieces of turn start (cooldowns, status effects, passives)
        NotifyTurnStart(currentMove);

        EvaluateGameState();
    }

    /// <summary>
    /// Notify all pieces that a new turn has started.
    /// </summary>
    private void NotifyTurnStart(int turnColor)
    {
        if (boardState == null) return;

        List<PieceMove> allPieces = new List<PieceMove>();
        allPieces.AddRange(boardState.GetAllPieces(ChessConstants.WHITE));
        allPieces.AddRange(boardState.GetAllPieces(ChessConstants.BLACK));

        foreach (PieceMove piece in allPieces)
        {
            if (piece.elementalPiece != null)
            {
                piece.elementalPiece.OnTurnStart(turnColor);
            }
        }
    }

    /// <summary>
    /// Try to capture a piece, running passive hooks. Returns true if capture is allowed.
    /// </summary>
    public bool TryCapture(PieceMove attacker, PieceMove defender)
    {
        // Check attacker's passive: OnBeforeCapture
        if (attacker.elementalPiece != null && attacker.elementalPiece.passive != null)
        {
            if (!attacker.elementalPiece.passive.OnBeforeCapture(attacker, defender, boardState))
                return false;
        }

        // Check defender's passive: OnBeforeCapture (defender's passive can also prevent)
        if (defender.elementalPiece != null && defender.elementalPiece.passive != null)
        {
            if (!defender.elementalPiece.passive.OnBeforeCapture(attacker, defender, boardState))
                return false;
        }

        // Execute capture
        takePiece(defender);

        // After capture hooks
        if (attacker.elementalPiece != null && attacker.elementalPiece.passive != null)
        {
            attacker.elementalPiece.passive.OnAfterCapture(attacker, defender, boardState);
        }
        if (defender.elementalPiece != null && defender.elementalPiece.passive != null)
        {
            defender.elementalPiece.passive.OnPieceCaptured(defender, attacker, boardState);
        }

        return true;
    }

    /// <summary>
    /// Enter ability mode for the selected piece.
    /// </summary>
    public void EnterAbilityMode(PieceMove piece)
    {
        if (abilityExecutor != null && piece != null)
        {
            if (abilityExecutor.EnterAbilityMode(piece))
            {
                selectedPiece = piece;
                isPieceSelected = true;
            }
        }
    }

    //Game Piece Control
    public void deSelectPiece()
    {
        //only works for 2 colors
        selectedPiece = null;
        swapUIIcon(MouseUI.START);
        lr.SetPosition(1, hiddenIsland);
        lr.SetPosition(0, hiddenIsland);
        isPieceSelected = false;
        lr.enabled = false;
    }

    public void selectPiece(Transform t, PieceMove piece)
    {
        lr.enabled = true;
        isPieceSelected = true;
        selectedUI.setTransformPosition(t.position);
        selectedPiece = piece;
        selectedPiece.createPieceMoves(selectedPiece.piece);
        selectedPiece.printMovesList();

        // Log element/ability info
        if (piece.elementalPiece != null)
        {
            ElementalPiece ep = piece.elementalPiece;
            string elemName = ep.elementId == ChessConstants.ELEMENT_FIRE ? "Fire"
                            : ep.elementId == ChessConstants.ELEMENT_EARTH ? "Earth"
                            : ep.elementId == ChessConstants.ELEMENT_LIGHTNING ? "Lightning" : "None";
            string cdInfo = ep.cooldown != null
                ? (ep.cooldown.IsReady ? "READY" : ep.cooldown.CurrentCooldown + " turns")
                : "N/A";
            Debug.Log("[" + elemName + "] " + piece.printPieceName() + " | Ability CD: " + cdInfo + " | Press Q to activate");
        }
    }

    public void takePiece(PieceMove p)
    {
        //Currently just moves the piece doesnt score or move it into a position where it can come back
        p.pieceTaken();
    }

    //Game Creation
    private void createPiece(int piece, int color)
    { //1 pawn, 2 rook, 3 knight, 4 bishop, 5 queen, 6 king, 
        /*											//Color: 1 Black, 2 White, 3 Green, 4 Blue, 5 Red, 6 Yellow
		if (piece == 1) {
			if (color == 1) {
				//Instantiate piece

				pieceMeshCollider.sharedMesh = 

				}
			else if (color == 2) {
				createdPC = PieceColor.WHITE;
			}

		}
		else if (piece == 2) {
			createdPT = PieceType.ROOK;
		}
		else if (piece == 3) {
			createdPT = PieceType.KNIGHT;
		}
		else if (piece == 4) {
			createdPT = PieceType.BISHOP;
		}
		else if (piece == 5) {
			createdPT = PieceType.QUEEN;
		}
		else if (piece == 6) {
			createdPT = PieceType.KING;
		}
		*/
        //Instantiate (pieces[piece-1], , Quaternion.identity);


        //create new piece
        // attatch ui 
    }


    private void createBoard(int size)
    {
        Vector3 boardSquarePos = new Vector3(0.0f, 0.0f, 0.0f);
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (i % 2 == 0)
                {
                    if (j % 2 == 0)
                    {
                        boardPos[i, j] = Instantiate(blackSquare, boardSquarePos, Quaternion.identity);
                        boardPos[i, j].GetComponent<Square>().x = j;
                        boardPos[i, j].GetComponent<Square>().y = i;
                        boardPos[i, j].name = letters[i] + (j + 1).ToString();

                    }
                    else
                    {
                        boardPos[i, j] = (GameObject)Instantiate(whiteSquare, boardSquarePos, Quaternion.identity);
                        boardPos[i, j].GetComponent<Square>().x = j;
                        boardPos[i, j].GetComponent<Square>().y = i;
                        boardPos[i, j].name = letters[i] + (j + 1).ToString();
                    }
                }
                else
                {
                    if (j % 2 == 0)
                    {
                        boardPos[i, j] = (GameObject)Instantiate(whiteSquare, boardSquarePos, Quaternion.identity);
                        boardPos[i, j].GetComponent<Square>().x = j;
                        boardPos[i, j].GetComponent<Square>().y = i;
                        boardPos[i, j].name = letters[i] + (j + 1).ToString();
                    }
                    else
                    {
                        boardPos[i, j] = (GameObject)Instantiate(blackSquare, boardSquarePos, Quaternion.identity);
                        boardPos[i, j].GetComponent<Square>().x = j;
                        boardPos[i, j].GetComponent<Square>().y = i;
                        boardPos[i, j].name = letters[i] + (j + 1).ToString();
                    }
                }
                boardSquarePos.x += 1;

                boardPos[i, j].transform.parent = boardRows[i].transform;
            }
            boardSquarePos.z += 1;
            boardSquarePos.x = 0;
        }
    }

    //UI
    void setUpLine()
    {
        lr.alignment = LineAlignment.View;
        lr.endWidth = 0.50f;
        lr.startWidth = 0.5f;
        lr.positionCount = 2;
        lr.SetPosition(0, Camera.main.ScreenToWorldPoint((selectedUI.gameObject.transform.position)));
        lr.material = lineMaterial;
    }

    void swapUIIcon(MouseUI m)
    {
        if (m == MouseUI.CANMOVE)
        {
            takeMoveUI.setTransformPosition(hiddenIsland);
            cantMoveUI.setTransformPosition(hiddenIsland);
            canMoveUI.followTarget(Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 8.5f)));
        }

        if (m == MouseUI.CANTMOVE)
        {
            takeMoveUI.setTransformPosition(hiddenIsland);
            canMoveUI.setTransformPosition(hiddenIsland);
            cantMoveUI.followTarget(Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 8.5f)));
        }

        if (m == MouseUI.TAKEPIECE)
        {
            cantMoveUI.setTransformPosition(hiddenIsland);
            canMoveUI.setTransformPosition(hiddenIsland);
            takeMoveUI.followTarget(Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 8.5f)));
        }

        if (m == MouseUI.START)
        {
            selectedUI.setTransformPosition(hiddenIsland);
            canMoveUI.setTransformPosition(hiddenIsland);
            cantMoveUI.setTransformPosition(hiddenIsland);
            takeMoveUI.setTransformPosition(hiddenIsland);
        }
    }

    // ========== Chess Rules - Check/Checkmate Detection ==========

    /// <summary>
    /// Evaluate game state after each move to detect check, checkmate, or stalemate.
    /// </summary>
    private void EvaluateGameState()
    {
        if (boardState == null) return;

        int nextPlayer = currentMove;
        bool inCheck = boardState.IsKingInCheck(nextPlayer);
        bool hasLegalMoves = HasAnyLegalMoves(nextPlayer);

        if (inCheck && !hasLegalMoves)
        {
            // Checkmate
            currentGameState = (nextPlayer == ChessConstants.WHITE)
                ? GameState.BlackWins
                : GameState.WhiteWins;
            string winner = (nextPlayer == ChessConstants.WHITE) ? "Black" : "White";
            GameLogUI.LogEvent("<color=#FF4444>CHECKMATE! " + winner + " wins.</color>");
            OnGameOver();
        }
        else if (!inCheck && !hasLegalMoves)
        {
            // Stalemate
            currentGameState = GameState.Stalemate;
            GameLogUI.LogEvent("<color=#FFAA44>STALEMATE — Draw.</color>");
            OnGameOver();
        }
        else if (inCheck)
        {
            currentGameState = (nextPlayer == ChessConstants.WHITE)
                ? GameState.WhiteInCheck
                : GameState.BlackInCheck;
            string checkedSide = (nextPlayer == ChessConstants.WHITE) ? "White" : "Black";
            GameLogUI.LogEvent("<color=#FF6666>CHECK! " + checkedSide + " king is in check.</color>");
            Debug.Log("CHECK! " + checkedSide + " king is in check.");
        }
        else
        {
            currentGameState = GameState.Playing;
        }
    }

    /// <summary>
    /// Check if a player has any legal moves available.
    /// </summary>
    private bool HasAnyLegalMoves(int color)
    {
        if (boardState == null) return true;

        List<PieceMove> pieces = boardState.GetAllPieces(color);
        foreach (PieceMove piece in pieces)
        {
            piece.createPieceMoves(piece.piece);
            if (piece.moves.Count > 0)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Handle game over state.
    /// </summary>
    private void OnGameOver()
    {
        // Force exit ability mode if active
        if (abilityExecutor != null && abilityExecutor.isInAbilityMode)
        {
            abilityExecutor.ExitAbilityMode();
        }
        deSelectPiece();

        switch (currentGameState)
        {
            case GameState.WhiteWins:
                Debug.Log("CHECKMATE! White wins the game.");
                break;
            case GameState.BlackWins:
                Debug.Log("CHECKMATE! Black wins the game.");
                break;
            case GameState.Stalemate:
                Debug.Log("STALEMATE! The game is a draw.");
                break;
            case GameState.Draw:
                Debug.Log("DRAW! The game is a draw.");
                break;
        }
    }
}