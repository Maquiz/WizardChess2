/// <summary>
/// Container for all 9 deck slots. Serialized to/from JSON for persistence.
/// </summary>
[System.Serializable]
public class DeckSaveData
{
    public DeckSlot[] slots = new DeckSlot[9];

    public DeckSaveData()
    {
        for (int i = 0; i < 9; i++)
        {
            slots[i] = new DeckSlot();
        }
    }
}
