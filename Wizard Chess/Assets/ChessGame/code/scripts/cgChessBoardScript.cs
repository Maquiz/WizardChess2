using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// The script attached to the chessboard prefab, should have gameobjects with squarescripts, this class: verifies if the player can drag and drop pieces, handles whether or not the engine should make move, checks whether or not the game is over(and if so shows the game over prefab), flips the board, resets the board etc.
/// </summary>
public class cgChessBoardScript : MonoBehaviour
{
    #region Properties & fields
    /// <summary>
    /// A sound to play whenever a move is made.
    /// </summary>
    public AudioClip moveSound;

    /// <summary>
    /// A sound to play when any player makes a move that checks the king.
    /// </summary>
    public AudioClip checkSound;

    /// <summary>
    /// A sound to play when the game is won.
    /// </summary>
    public AudioClip winSound;

    /// <summary>
    /// A sound to play when the game is lost or drawn.
    /// </summary>
    public AudioClip loseSound;

    /// <summary>
    /// The number of moves made.
    /// </summary>
    public int movesMade = 0;

    /// <summary>
    /// The textfield to log all moves made.
    /// </summary>
    public GUIText moveLog;

    /// <summary>
    /// The sprite to display when the board is not flipped
    /// </summary>
    public Sprite borderUnflipped;

    /// <summary>
    /// The sprite to display on the border when the board is flipped
    /// </summary>
    public Sprite borderFlipped;

    /// <summary>
    /// The target to flip when the user click the Flip board button.
    /// </summary>
    public GameObject flipTarget;

    /// <summary>
    /// Game over prefab, instantiated when the game is won/drawn/lost
    /// </summary>
    public GameObject GameOverPrefab;

    /// <summary>
    /// Main menu prefab, instantiated when the user clicks Main menu button.
    /// </summary>
    public GameObject MainMenuPrefab;

    /// <summary>
    /// The coordinate borders of the board, the sprite must be changed when the board is flipped.
    /// </summary>
    public SpriteRenderer border;

    /// <summary>
    /// Is it whites turn to move? if false then its blacks turn.
    /// </summary>
    public bool whiteTurnToMove = true;

    /// <summary>
    /// Should the last move be highlighted on the board?
    /// </summary>
    public bool highlightLastMove = true;

    /// <summary>
    /// Should the all legal moves be highlighted when the player drags a piece?
    /// </summary>
    public bool highlightLegalMoves = true;

    /// <summary>
    /// All possible board modes.
    /// </summary>
    public enum BoardMode { PlayerVsPlayer, PlayerVsEngine, EngineVsPlayer, EngineVsEngine }

    /// <summary>
    /// The current board mode.
    /// </summary>
    public BoardMode Mode = BoardMode.PlayerVsEngine;

    /// <summary>
    /// Which notationtype should be used to notate the game?
    /// </summary>
    public cgNotation.NotationType NotationType = cgNotation.NotationType.Algebraic;

    /// <summary>
    /// The instance of the game over screen.
    /// </summary>
    private GameObject _gameOverScreen;

    /// <summary>
    /// This is the underlying board representation, we test and find legality of moves on this.
    /// </summary>
    private cgBoard _abstractBoard = new cgBoard();


    /// <summary>
    /// All currently captured pieces.
    /// </summary>
    private List<cgChessPieceScript> _deadPieces = new List<cgChessPieceScript>();

    /// <summary>
    /// All currently uncaptured pieces on the board.
    /// </summary>
    private List<cgChessPieceScript> _livePieces = new List<cgChessPieceScript>();

    /// <summary>
    /// The AI opponent
    /// </summary>
    private cgEngine _engine;

    /// <summary>
    /// Number of dead white pieces.
    /// </summary>
    private int _deadWhitePieces = 0;

    /// <summary>
    /// Number of dead black pieces.
    /// </summary>
    private int _deadBlackPieces = 0;

    /// <summary>
    /// The current piece being dragged by the mouse.
    /// </summary>
    private cgChessPieceScript _downPiece;

    /// <summary>
    /// Logged moves, used by coordinate notation.
    /// </summary>
    private int _loggedMoves = 0;

    private cgSquareScript[] _squares;
    /// <summary>
    /// Can the player drag and move pieces? Yes if a human controls the current color whoms turn it is to move.
    /// </summary>
    public bool playerCanMove
    {
        get
        {
            return ((_humanPlayerIsBlack && !whiteTurnToMove) || (_humanPlayerIsWhite && whiteTurnToMove)) ? true : false;
        }
    }

    /// <summary>
    /// Is it a human playing black? Determined by the current boardmode.
    /// </summary>
    private bool _humanPlayerIsBlack
    {
        get
        {
            return ((Mode == BoardMode.EngineVsPlayer) || (Mode == BoardMode.PlayerVsPlayer));
        }
    }

    /// <summary>
    /// Is it a human playing white? Determined by the current boardmode.
    /// </summary>
    private bool _humanPlayerIsWhite
    {
        get
        {
            return ((Mode == BoardMode.PlayerVsEngine) || (Mode == BoardMode.PlayerVsPlayer));
        }
    }
    #endregion

    #region Initialization

    private void _start()
    {
        _setBoardTo(new cgBoard());
        if (Mode == BoardMode.PlayerVsEngine && !whiteTurnToMove) _engine.MakeMove(_abstractBoard, false, _engineCallback);
        else if (Mode == BoardMode.EngineVsPlayer && whiteTurnToMove) _engine.MakeMove(_abstractBoard, true, _engineCallback);
    }
    public void StartEngineVsPlayer()
    {
        Mode = BoardMode.EngineVsPlayer;
        _start();
    }
    public void StartPlayerVsPlayer()
    {
        Mode = BoardMode.PlayerVsPlayer;
        _start();
    }
    public void StartGame(BoardMode mode)
    {
        if (mode == BoardMode.PlayerVsEngine) StartPlayerVsEngine();
        else if (mode == BoardMode.EngineVsPlayer) StartEngineVsPlayer();
        else if (mode == BoardMode.PlayerVsPlayer) StartPlayerVsPlayer();

    }
    public void StartPlayerVsEngine()
    {
        Mode = BoardMode.PlayerVsEngine;
        _start();
    }
    private void _getSquares()
    {
        _squares = GetComponentsInChildren<cgSquareScript>();
    }

    private void _placePieces()
    {
        List<cgChessPieceScript> pieces = new List<cgChessPieceScript>();
        foreach (cgChessPieceScript piece in GetComponentsInChildren<cgChessPieceScript>()) pieces.Add(piece);
        for (byte i = 0; i < _abstractBoard.squares.Count; i++)
        {
            if (_abstractBoard.squares[i] != 0)
            {
                for (int u = pieces.Count; u > 0; u--)
                {
                    if (!_livePieces.Contains(pieces[u - 1]))
                    {
                        pieces[u - 1].StartAtSquare(_getSquare(cgGlobal.SquareNames[i]));
                        _livePieces.Add(pieces[u - 1]);
                        pieces[u - 1].dead = false;
                        pieces[u - 1].SetType((int)_abstractBoard.squares[i]);
                        pieces.RemoveAt(u - 1);
                        break;
                    }
                }
            }
        }


        if (pieces.Count > 0)
        {
            List<sbyte> unplaced = new List<sbyte>();
            foreach (sbyte sby in cgBoard.defaultStartPosition) if (sby != 0) unplaced.Add(sby);

            foreach (cgChessPieceScript scr in _livePieces)
            {
                for (int s = unplaced.Count; s > 0; s--)
                {
                    if (unplaced[s - 1] == (int)scr.type)
                    {
                        unplaced.RemoveAt(s - 1);
                        break;
                    }
                }
            }
            int blackPromotions = 0;
            int whitePromotions = 0;
            foreach (cgSimpleMove mov in _abstractBoard.moves)
            {
                if (mov.queened)
                {
                    if (mov.to < 30) whitePromotions++;
                    else blackPromotions++;
                }
            }

            foreach (sbyte missingType in unplaced)
            {
                if (missingType == -1 && blackPromotions > 0)
                {
                    blackPromotions--;
                    continue;
                }
                else if (missingType == 1 && whitePromotions > 0)
                {
                    whitePromotions--;
                    continue;
                }
                if (pieces.Count > 0)
                {
                    pieces[pieces.Count - 1].SetType(missingType);
                    _setDeadPiece(pieces[pieces.Count - 1]);
                    pieces.RemoveAt(pieces.Count - 1);
                }
            }
        }
        //Give all active pieces callback functions for mouse events.
        foreach (cgChessPieceScript piece in _livePieces)
        {
            piece.SetCallbacks(_pieceDown, _pieceUp);
        }

    }
    #endregion

    #region Unity messages
    // Use this for initialization
    void Awake()
    {
        _getSquares();
        _placePieces();

        _engine = GetComponent<cgEngine>();

        if (Mode == BoardMode.EngineVsPlayer)
        {
            _engine.MakeMove(_abstractBoard, true, _engineCallback);
        }


        
    }
    // Update is called once per frame
    void Update()
    {
        foreach (cgChessPieceScript cp in _livePieces)
        {
            if (cp.dead && !_deadPieces.Contains(cp)) _setDeadPiece(cp);
        }

        for (int i = _deadPieces.Count; i > 0; i--)
        {
            if (_livePieces.Contains(_deadPieces[i - 1])) _livePieces.Remove(_deadPieces[i - 1]);
        }
        if (_downPiece != null)
        {
            Vector3 mouseCoords = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            _downPiece.transform.position = new Vector3(mouseCoords.x, mouseCoords.y, 0);
        }

        if (Input.GetKey(KeyCode.C) && Input.GetKeyDown(KeyCode.LeftControl)) _copyGameToClipboard();
        else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C)) _copyGameToClipboard();


        if (Input.GetKey(KeyCode.V) && Input.GetKeyDown(KeyCode.LeftControl)) _pasteGameFromClipboard();
        else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V)) _pasteGameFromClipboard();

    }
    #endregion

    #region UI Button functions
    public void ResetBoard()
    {
        Debug.Log("reset");
        _setBoardTo(new cgBoard());
        if (_gameOverScreen != null)
        {
            Debug.Log("destroying:" + _gameOverScreen);
            GameObject.Destroy(_gameOverScreen);
            _gameOverScreen = null;
        }
        StartGame(Mode);
    }
    public void RevertLastMove()
    {
        Debug.Log("reverting");
        _abstractBoard.revert();
        _setBoardTo(_abstractBoard);
    }
    public void MainMenu()
    {
        if (_gameOverScreen != null)
        {
            GameObject.Destroy(_gameOverScreen);
            _gameOverScreen = null;
        }
        GameObject.Instantiate(MainMenuPrefab);
        GameObject.DestroyImmediate(gameObject);
    }
    public void FlipBoard()
    {
        if (flipTarget != null)
        {
            int increment = 180;
            flipTarget.transform.RotateAround(Vector3.zero, Vector3.forward, increment);
            //flipTarget.transform.Rotate(Vector3.forward, increment);
            foreach (cgChessPieceScript piece in _livePieces) piece.transform.Rotate(Vector3.forward, increment);
            foreach (cgChessPieceScript piece in _deadPieces) piece.transform.Rotate(Vector3.forward, increment);
        }
        if (border != null)
        {
            border.sprite = border.sprite == borderUnflipped ? borderFlipped : borderUnflipped;
        }
    }

    public void SuggestMove()
    {
        if (playerCanMove) _engine.MakeMove(_abstractBoard, _abstractBoard.whiteTurnToMove, _engineSuggestion);
    }

    private void _engineSuggestion(cgSimpleMove move)
    {
        if (playerCanMove)
        {
            if (_abstractBoard.verifyLegality(move)) _suggestMove(move);
            else
            {
                _engine.Moves.Remove(move);
                if (_engine.Moves.Count > 0) _engineSuggestion(_engine.Moves[0]);
            }

        }
    }

    private void _suggestMove(cgSimpleMove move)
    {
        cgSquareScript departingSquare = _getSquare(cgGlobal.SquareNames[move.from]);
        cgSquareScript arrivalSquare = _getSquare(cgGlobal.SquareNames[move.to]);

        departingSquare.highlightTemporarily(new Color(0, .5f, 0));
        arrivalSquare.highlightTemporarily(new Color(0, .5f, 0));
    }
    #endregion

    /// <summary>
    /// Play provided sound, adds an audiosource if one does not exist on this gameobject.
    /// </summary>
    /// <param name="clip"></param>
    public void playSound(AudioClip clip)
    {
        if (clip == null) return;
        if (GetComponent<AudioSource>() == null) gameObject.AddComponent<AudioSource>();
        GetComponent<AudioSource>().clip = clip;
        GetComponent<AudioSource>().Play();
    }


    /// <summary>
    /// A piece has callbacked that the user has pressed it.
    /// </summary>
    /// <param name="piece"></param>
    private void _pieceDown(cgChessPieceScript piece)
    {
        if (highlightLegalMoves)
        {
            List<cgSimpleMove> moves = _abstractBoard.findStrictLegalMoves(_abstractBoard.whiteTurnToMove);
            foreach (cgSimpleMove move in moves) if (cgGlobal.SquareNames[move.from] == piece.square.uniqueName) _getSquare(cgGlobal.SquareNames[move.to]).changeColor(_getSquare(cgGlobal.SquareNames[move.from]).legalMoveToColor);
        }
        _downPiece = piece;
        
        
        //int indexPosition = cgGlobal.IndexFromCellName(_downPiece.square.uniqueName);

        //_abstractBoard.squares[indexPosition = 2//make the changes you want.
    }

    /// <summary>
    /// The user has released a dragged piece. Verify that its a legal move, if so perform the move and perform the next move if appropriate mode.
    /// </summary>
    /// <param name="piece"></param>
    private void _pieceUp(cgChessPieceScript piece)
    {
        if (_downPiece != null)
        {
            if (playerCanMove || Mode == BoardMode.PlayerVsPlayer)
            {
                cgSimpleMove legalMove = null;
                cgSquareScript closestSquare = _findSquareAt(_downPiece.transform.position);
                List<cgSimpleMove> legalMoves = _abstractBoard.findLegalMoves(whiteTurnToMove);
                foreach (cgSimpleMove move in legalMoves) if (cgGlobal.SquareNames[move.from] == _downPiece.square.uniqueName && cgGlobal.SquareNames[move.to] == closestSquare.uniqueName) legalMove = move;
                //test legality of move here.

                if (legalMove != null && _abstractBoard.verifyLegality(legalMove))
                {

                    //piece.moveToSquare(closestSquare);
                    _makeMove(legalMove);
                    if (Mode == BoardMode.PlayerVsEngine) _engine.MakeMove(_abstractBoard, false, _engineCallback);
                    else if (Mode == BoardMode.EngineVsPlayer) _engine.MakeMove(_abstractBoard, true, _engineCallback);
                }
                else piece.moveToSquare(piece.square);
            }
            else piece.moveToSquare(piece.square);
            _downPiece = null;
        }
        else
        {
            piece.moveToSquare(piece.square);
            _downPiece = null;
        }
        if (highlightLastMove)
        {//revert colors if highlighting is active
            foreach (cgSquareScript square in _squares) square.changeColor(square.startColor);
        }
    }

    /// <summary>
    /// Find the square location at the provided position, used to find the square where the user is dragging and dropping a piece.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private cgSquareScript _findSquareAt(Vector3 position)
    {
        float best = Vector3.Distance(position, _squares[0].transform.position);
        cgSquareScript square = _squares[0];

        foreach (cgSquareScript candsquare in _squares)
        {
            if (Vector3.Distance(position, candsquare.transform.position) < best)
            {
                best = Vector3.Distance(position, candsquare.transform.position);
                square = candsquare;
            }
        }
        return square;
    }

    /// <summary>
    /// The engine returns with its prefered move.
    /// </summary>
    /// <param name="move">The prefered move.</param>
    private void _engineCallback(cgSimpleMove move)
    {
        //Debug.Log("move:" + move.from.name + " to:" + move.to.name);
        if (!playerCanMove)
        {
            if (_abstractBoard.verifyLegality(move)) _makeMove(move);
            else
            {
                _engine.Moves.Remove(move);
                if (_engine.Moves.Count > 0) _engineCallback(_engine.Moves[0]);
                else
                {
                    _checkGameOver();
                }
            }

        }
    }

    /// <summary>
    /// Game over, instantiate the game over screen and let it display the provided message.
    /// </summary>
    /// <param name="display"></param>
    private void _gameOver(bool whiteWins, bool blackWins)
    {
        string gameOverString = "Game Over. ";
        if (whiteWins) 
            gameOverString = "White Wins!";
        else if (blackWins) 
            gameOverString = "Black Wins!";
        else if (!blackWins && !whiteWins) 
            gameOverString = "Its a draw!";

        if (_gameOverScreen == null)
        {
            _gameOverScreen = GameObject.Instantiate(GameOverPrefab);
            _gameOverScreen.GetComponent<cgGameOverScript>().initialize(gameOverString, ResetBoard, MainMenu);
        }
    }

    /// <summary>
    /// Check if the game is over, should be performed after every move.
    /// </summary>
    private void _checkGameOver()
    {
        bool isChecked = _abstractBoard.isChecked(whiteTurnToMove);
        List<cgSimpleMove> responses = _abstractBoard.findLegalMoves(_abstractBoard.whiteTurnToMove);
        for (int i = responses.Count; i > 0; i--)
        {
            if (!_abstractBoard.verifyLegality(responses[i - 1]))
            {
                responses.RemoveAt(i - 1);
                continue;
            }
        }

        if (responses.Count == 0 && isChecked)
        {
            _gameOver(!whiteTurnToMove,whiteTurnToMove);
            if ((_humanPlayerIsBlack && whiteTurnToMove) || (_humanPlayerIsWhite && !whiteTurnToMove))
            {
                playSound(winSound);
            }
            else playSound(loseSound);

        }
        else if (isChecked)
        {
            playSound(checkSound);
        }
        else if (responses.Count == 0 && !isChecked)
        {
            //Draw by stalemate - no legal moves available to player whose turn it is to move.
            _gameOver(false,false);
            playSound(loseSound);

        }
    }

    /// <summary>
    /// Peform the provided move on the visual board and the abstract board, with no legality checks - thus should be performed prior to calling this.
    /// </summary>
    /// <param name="move"></param>
    private void _makeMove(cgSimpleMove move)
    {
        //Debug.Log("making move:" + cgGlobal.MoveToString(move));

        movesMade++;
        playSound(moveSound);
        _abstractBoard.move(move);
        _writeLog(move);
        //_abstractBoard.debugReadBoard();
        if (_getPieceOn(cgGlobal.SquareNames[move.to]) != null)
        {
            _setDeadPiece(_getPieceOn(cgGlobal.SquareNames[move.to]));
        }
        cgChessPieceScript piece = _getPieceOn(cgGlobal.SquareNames[move.from]);
        piece.moveToSquare(_getSquare(cgGlobal.SquareNames[move.to]));
        if (move.queened) piece.SetType(_abstractBoard.squares[move.to] > 0 ? cgChessPieceScript.Type.WhiteQueen : cgChessPieceScript.Type.BlackQueen);
        if (move is cgCastlingMove)
        {
            cgChessPieceScript piece2 = _getPieceOn(cgGlobal.SquareNames[(move as cgCastlingMove).secondFrom]);
            if (piece2) piece2.moveToSquare(_getSquare(cgGlobal.SquareNames[(move as cgCastlingMove).secondTo]));
        }
        else if (move is cgEnPassantMove)
        {
            cgChessPieceScript piece2 = _getPieceOn(cgGlobal.SquareNames[(move as cgEnPassantMove).attackingSquare]);
            piece2.dead = true;
        }

        whiteTurnToMove = _abstractBoard.whiteTurnToMove;
        _checkGameOver();
        if (highlightLastMove)
        {
            //Color copyFrom = _getSquare(cgGlobal.SquareNames[move.to]).startColor;
            Color color = _getSquare(cgGlobal.SquareNames[move.to]).recentMoveColor;

            _getSquare(cgGlobal.SquareNames[move.to]).highlightTemporarily(color);
        }


    }


    private cgSquareScript _getSquare(string p)
    {
        foreach (cgSquareScript square in _squares) if (square != null && square.uniqueName == p) return square;
        return null;
    }

    private cgChessPieceScript _getPieceOn(string p)
    {
        foreach (cgChessPieceScript cp in _livePieces) if (cp.square != null && cp.square.uniqueName == p) return cp;
        return null;
    }

    private void _setDeadPiece(cgChessPieceScript cp)
    {
        cp.dead = true;
        cp.square = null;
        if (!_deadPieces.Contains(cp))
        {
            cp.transform.position = new Vector3(3 + (cp.white ? .3f : 0), (float)((cp.white ? _deadWhitePieces : _deadBlackPieces) * .4) - 2, 0);
            _deadPieces.Add(cp);
            if (cp.white) _deadWhitePieces++;
            else _deadBlackPieces++;
        }
    }

    /// <summary>
    /// Paste the game notation from clipboard onto the board.
    /// </summary>
    private void _pasteGameFromClipboard()
    {
        string curgame = GUIUtility.systemCopyBuffer;
        _abstractBoard = new cgBoard();
        Debug.Log("Pasted game from clipboard: " + curgame);
        cgNotation notation = new cgNotation(_abstractBoard);
        notation.Read(curgame);

        _abstractBoard.LoadGame(notation);
        _setBoardTo(_abstractBoard);
    }

    /// <summary>
    /// Copy game notation to clipboard, if for instance the user wants to save his current game.
    /// </summary>
    private void _copyGameToClipboard()
    {
        //Debug.Log("herp");
        string curgame;
        cgNotation notation = new cgNotation(_abstractBoard);
        curgame = notation.writeFullNotation(cgNotation.NotationType.Algebraic, cgNotation.FormatType.None);
        GUIUtility.systemCopyBuffer = curgame;
        //moveLog.text += "ctrl+c";
    }

    /// <summary>
    /// Set the board to the provided abstract board, write any moves provided in said abstract board to the log, etc.
    /// </summary>
    /// <param name="board"></param>
    private void _setBoardTo(cgBoard board)
    {
        _abstractBoard = board;
        _livePieces = new List<cgChessPieceScript>();
        _deadPieces = new List<cgChessPieceScript>();
        _deadWhitePieces = 0;
        _deadBlackPieces = 0;
        movesMade = _abstractBoard.moves.Count;
        moveLog.text = "Moves: \n";
        _loggedMoves = 0;
        foreach (cgSimpleMove move in board.moves) _writeLog(move);
        whiteTurnToMove = _abstractBoard.whiteTurnToMove;
        _placePieces();
    }


    /// <summary>
    /// Write move to log.
    /// </summary>
    /// <param name="move"></param>
    private void _writeLog(cgSimpleMove move)
    {
        if (NotationType == cgNotation.NotationType.Coordinate)
        {
            if (_loggedMoves % 2 == 0) moveLog.text += "\n";
            else moveLog.text += " | ";
            moveLog.text += cgGlobal.SquareNames[move.from] + "-" + cgGlobal.SquareNames[move.to];
        }
        else if (NotationType == cgNotation.NotationType.Algebraic)
        {
            moveLog.text = "Moves:\n";
            cgNotation note = new cgNotation(_abstractBoard);
            moveLog.text = note.getLogFriendlyNotation();
        }
        _loggedMoves++;
    }



}
