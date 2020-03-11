using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Castling moves the king and a rook, this class has additional properties to handle this additional information.
/// </summary>
public class cgCastlingMove : cgSimpleMove
{
    /// <summary>
    /// The square the rook being castled is departing from.
    /// </summary>
    public byte secondFrom;

    /// <summary>
    /// The square the rook being castled will arrive at.
    /// </summary>
    public byte secondTo;
    public cgCastlingMove(byte fromp, byte top, sbyte posVal,byte s_from, byte s_to)
        :base(fromp,top,posVal)
    {
        secondFrom = s_from;
        secondTo = s_to;
    }
}

