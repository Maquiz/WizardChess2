/// <summary>
/// A single deck definition: a name and 16 element assignments (one per piece index).
/// </summary>
[System.Serializable]
public class DeckSlot
{
    public string name;
    public int[] elements = new int[16];
    public bool isEmpty = true;

    public DeckSlot()
    {
        name = "";
        elements = new int[16];
        isEmpty = true;
        for (int i = 0; i < 16; i++)
        {
            elements[i] = ChessConstants.ELEMENT_FIRE;
        }
    }

    /// <summary>
    /// Replace any ELEMENT_NONE values with ELEMENT_FIRE.
    /// Ensures all pieces have a valid element assigned.
    /// </summary>
    public void MigrateNoneToFire()
    {
        for (int i = 0; i < 16; i++)
        {
            if (elements[i] == ChessConstants.ELEMENT_NONE)
                elements[i] = ChessConstants.ELEMENT_FIRE;
        }
    }

    /// <summary>
    /// Create a deep copy of this deck slot.
    /// </summary>
    public DeckSlot Clone()
    {
        DeckSlot copy = new DeckSlot();
        copy.name = name;
        copy.isEmpty = isEmpty;
        for (int i = 0; i < 16; i++)
        {
            copy.elements[i] = elements[i];
        }
        return copy;
    }
}
