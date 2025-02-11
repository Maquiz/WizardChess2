﻿using UnityEngine;
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

    public bool canMove;
    public bool showMoves;
    public bool firstMove;

    private bool isSet;
    public Square curSquare;
    public GameObject Board;
    public GameMaster gm;

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
        curSquare = new Square();
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
            gm.selectPiece(this.gameObject.transform, this);

        }
    }

    public void setIntitialPiece(int x, int y, GameObject sq)
    {
        setPieceLocation(x, y);
        setLastPieceLocation(x, y);
        curSquare = sq.GetComponent<Square>();
        last = sq;
        //createRookMoves();
        //printMovesList();
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
        //The Move is not reseteting the taken square
            //Physical Movement 
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
            //Movement
            square.piece = this;
            curSquare = square;
            last = square.gameObject;

            //if not taking another piece
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
        for (int i = 0; i < moves.Count; i++)
        {
            if (x == moves[i].x && y == moves[i].y)
            {
                return true;
            }
        }
        return false;
    }

     public void createPieceMoves(int piece)
    {
        //1 pawn, 2 rook, 3 knight, 4 bishop, 5 queen, 6 king, 
        //Color: 1 Black, 2 White, 3 Green, 4 Blue, 5 Red, 6 Yellow

        moves.Clear();
        if (piece == 6)
        { 
            createKingMoves();
        }
        else if (piece == 5)
        { //Queen
            createQueenMoves();
        }
        else if (piece == 4)
        { 
            createBishopMoves();
        }
        else if (piece == 3)
        { 
            createKnightMoves();
        }
        else if (piece == 2)
        { 
            createRookMoves();
        }
        else {
            createPawnMoves();
        }
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

    public void createPawnMoves() {
        //black color = 1 moves -y
        //white color = 2 moves +y
        int direction = color == 2 ? -1 : 1; 
  
            if (isCoordsInBounds(cury + direction))
            {
                Square curSquareChecker = getSquare(curx, (cury + direction));
                if (curSquareChecker != null && !curSquareChecker.taken)
                {
                    moves.Add(curSquareChecker);
                }
            }
            if (firstMove) {
                Square curSquareChecker = getSquare(curx, (cury + (2 * direction)));
                if (curSquareChecker != null && !curSquareChecker.taken)
                {
                    moves.Add(curSquareChecker);
                }
            }
            if (isCoordsInBounds(cury + direction)) {
                if (isCoordsInBounds(curx + direction)) {
                    Square curSquareChecker = getSquare((curx +1) , (cury + direction));
                    if (curSquareChecker != null && curSquareChecker.taken)
                    {
                        if (color != curSquareChecker.piece.color)
                        {
                            moves.Add(curSquareChecker);
                        }
                    }
                }
                if (isCoordsInBounds(curx - direction))
                {
                    Square curSquareChecker = getSquare((curx - 1), (cury + direction));
                    if (curSquareChecker != null && curSquareChecker.taken)
                    {
                        if (color != curSquareChecker.piece.color)
                        {
                            moves.Add(curSquareChecker);
                        }
                    }
                }
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
}