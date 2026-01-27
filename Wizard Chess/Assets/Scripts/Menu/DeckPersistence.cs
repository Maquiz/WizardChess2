using UnityEngine;

/// <summary>
/// Static utility for loading/saving deck data to disk as JSON.
/// File location: Application.persistentDataPath + "/decks.json"
/// </summary>
public static class DeckPersistence
{
    private static string FilePath => System.IO.Path.Combine(Application.persistentDataPath, "decks.json");

    /// <summary>
    /// Load all deck data from disk. Returns fresh data if file doesn't exist.
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
        return new DeckSaveData();
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
