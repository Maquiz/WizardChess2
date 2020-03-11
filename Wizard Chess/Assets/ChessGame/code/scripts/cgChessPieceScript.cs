using UnityEngine;
using System.Collections;
using System;
/// <summary>
/// This script controls the piece on the board, it alters graphics according to promotions/reverts and registers mouse down and mouse up events for dragging purposes
/// </summary>
public class cgChessPieceScript : MonoBehaviour
{
    /// <summary>
    /// Is this piece white?
    /// </summary>
    public bool white = true;

    /// <summary>
    /// The current square being occupied by this instance.
    /// </summary>
    public cgSquareScript square;

    /// <summary>
    /// All possible chess types.
    /// </summary>
    public enum Type
    {
        WhitePawn = 1,
        WhiteRook = 2,
        WhiteKnight = 3,
        WhiteBishop = 4,
        WhiteQueen = 5,
        WhiteKing = 6,
        FireWhiteKing = 7,

        BlackPawn = -1,
        BlackRook = -2,
        BlackKnight = -3,
        BlackBishop = -4,
        BlackQueen = -5,
        BlackKing = -6

    }

    /// <summary>
    /// Sprites with index position corresponding to the type.
    /// </summary>
    public Sprite[] sprites;

    /// <summary>
    /// The type of this piece.
    /// </summary>
    public Type type = Type.WhitePawn;
    private Action<cgChessPieceScript> _onDown;
    private Action<cgChessPieceScript> _onUp;

    public bool dead = false;

    /// <summary>
    /// Set mouse callbacks to allow this instance to be dragged and dropped.
    /// </summary>
    /// <param name="onDown">Callback for mouse down</param>
    /// <param name="onUp">Callback for mouse up</param>
    public void SetCallbacks(Action<cgChessPieceScript> onDown, Action<cgChessPieceScript> onUp)
    {
        _onDown = onDown;
        _onUp = onUp;
    }
    void OnMouseDown()
    {
        if (_onDown != null && !dead) _onDown(this);
    }
    void OnMouseUp()
    {
        if (_onUp != null && !dead) _onUp(this);
    }

    /// <summary>
    /// Set the type of this piece, changes its sprite accordingly.
    /// Useful when reverting moves, or when pawns are promoted.
    /// </summary>
    /// <param name="toType">The type to change to.</param>
    public void SetType(Type toType)
    {
        type = toType;
        int i = (int)type;
        white = i > 0;
        if (type > 0) i += 5;
        else i += 6;
        if (GetComponent<SpriteRenderer>() != null && sprites.Length > i)
        {
            GetComponent<SpriteRenderer>().sprite = sprites[i];
        }
    }
    /// <summary>
    /// Set the type of this piece, changes its sprite accordingly.
    /// Useful when reverting moves, or when pawns are promoted.
    /// </summary>
    /// <param name="toType">The type to change to.</param>
    public void SetType(int toType)
    {
        type = (Type)toType;
        int i = (int)type;
        white = i > 0;
        if (type > 0) i += 5;
        else i += 6;
        if (GetComponent<SpriteRenderer>() != null && sprites.Length > i)
        {
            GetComponent<SpriteRenderer>().sprite = sprites[i];
        }

    }

    /// <summary>
    /// Start at provided square.
    /// </summary>
    /// <param name="startSquare">the starting square.</param>
    public void StartAtSquare(cgSquareScript startSquare)
    {
        square = startSquare;
        dead = false;
        if (startSquare != null)
        {

            //piece.SetStartNode(startnode.node);
            transform.position = new Vector3(startSquare.transform.position.x, startSquare.transform.position.y, 0);
        }


    }

    /// <summary>
    /// Move to a new square.
    /// </summary>
    /// <param name="newSquare">the new square to move to.</param>
    public void moveToSquare(cgSquareScript newSquare)
    {
        transform.position = new Vector3(newSquare.transform.position.x, newSquare.transform.position.y, 0);

        square = newSquare;
    }
}
