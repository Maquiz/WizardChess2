using UnityEngine;

/// <summary>
/// Static utility for loading/saving deck data to disk as JSON.
/// File location: Application.persistentDataPath + "/decks.json"
/// </summary>
public static class DeckPersistence
{
    private static string FilePath => System.IO.Path.Combine(Application.persistentDataPath, "decks.json");

    /// <summary>
    /// Load all deck data from disk. Returns fresh data with default decks if file doesn't exist.
    /// </summary>
    public static DeckSaveData Load()
    {
        string path = FilePath;
        if (System.IO.File.Exists(path))
        {
            try
            {
                string json = System.IO.File.ReadAllText(path);
                DeckSaveData data = JsonUtility.FromJson<DeckSaveData>(json);
                if (data != null && data.slots != null && data.slots.Length == 9)
                {
                    return data;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[DeckPersistence] Failed to load decks.json: " + e.Message);
            }
        }
        // Create fresh data with default decks
        return CreateDefaultDecks();
    }

    /// <summary>
    /// Create default deck data with 3 pre-built decks: All Fire, All Earth, All Lightning.
    /// </summary>
    private static DeckSaveData CreateDefaultDecks()
    {
        DeckSaveData data = new DeckSaveData();

        // Deck 1: All Fire
        data.slots[0] = CreateElementDeck("Fire Army", ChessConstants.ELEMENT_FIRE);

        // Deck 2: All Earth
        data.slots[1] = CreateElementDeck("Earth Army", ChessConstants.ELEMENT_EARTH);

        // Deck 3: All Lightning
        data.slots[2] = CreateElementDeck("Lightning Army", ChessConstants.ELEMENT_LIGHTNING);

        return data;
    }

    /// <summary>
    /// Create a deck with all pieces set to a single element.
    /// </summary>
    private static DeckSlot CreateElementDeck(string name, int elementId)
    {
        DeckSlot deck = new DeckSlot();
        deck.name = name;
        deck.isEmpty = false;
        for (int i = 0; i < 16; i++)
        {
            deck.elements[i] = elementId;
        }
        return deck;
    }

    /// <summary>
    /// Save all deck data to disk.
    /// </summary>
    public static void Save(DeckSaveData data)
    {
        string path = FilePath;
        try
        {
            string json = JsonUtility.ToJson(data, true);
            System.IO.File.WriteAllText(path, json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[DeckPersistence] Failed to save decks.json: " + e.Message);
        }
    }

    /// <summary>
    /// Save a single deck slot at the given index.
    /// </summary>
    public static void SaveDeck(int slotIndex, DeckSlot deck)
    {
        if (slotIndex < 0 || slotIndex >= 9) return;
        DeckSaveData data = Load();
        data.slots[slotIndex] = deck;
        Save(data);
    }
}
