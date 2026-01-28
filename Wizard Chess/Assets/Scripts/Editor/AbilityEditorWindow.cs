using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor window for viewing and balancing all 36 wizard chess abilities.
/// Opens via menu: WizardChess > Ability Balance Editor.
/// </summary>
public class AbilityEditorWindow : EditorWindow
{
    private static readonly string[] ElementNames = { "Fire", "Earth", "Lightning" };
    private static readonly int[] ElementIds =
    {
        ChessConstants.ELEMENT_FIRE,
        ChessConstants.ELEMENT_EARTH,
        ChessConstants.ELEMENT_LIGHTNING
    };
    private static readonly string[] PieceNames = { "Pawn", "Rook", "Knight", "Bishop", "Queen", "King" };
    private static readonly int[] PieceTypes =
    {
        ChessConstants.PAWN, ChessConstants.ROOK, ChessConstants.KNIGHT,
        ChessConstants.BISHOP, ChessConstants.QUEEN, ChessConstants.KING
    };

    // Element tab colors
    private static readonly Color FireColor = new Color(0.9f, 0.35f, 0.15f);
    private static readonly Color EarthColor = new Color(0.65f, 0.5f, 0.2f);
    private static readonly Color LightningColor = new Color(0.3f, 0.5f, 0.9f);
    private static readonly Color[] ElementColors = { FireColor, EarthColor, LightningColor };

    private int selectedElement = 0;
    private int selectedPiece = 0;

    private AbilityBalanceConfig config;
    private SerializedObject serializedConfig;
    private Vector2 rightPanelScroll;

    [MenuItem("WizardChess/Ability Balance Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<AbilityEditorWindow>("Ability Balance Editor");
        window.minSize = new Vector2(620, 400);
    }

    private void OnEnable()
    {
        LoadOrCreateConfig();
    }

    private void LoadOrCreateConfig()
    {
        config = AbilityBalanceConfig.Instance;
        if (config == null)
        {
            // Try to find any existing asset
            string[] guids = AssetDatabase.FindAssets("t:AbilityBalanceConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                config = AssetDatabase.LoadAssetAtPath<AbilityBalanceConfig>(path);
            }
        }

        if (config != null)
        {
            serializedConfig = new SerializedObject(config);
        }
    }

    private void CreateConfigAsset()
    {
        // Ensure Resources folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        config = ScriptableObject.CreateInstance<AbilityBalanceConfig>();
        AssetDatabase.CreateAsset(config, "Assets/Resources/AbilityBalanceConfig.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        serializedConfig = new SerializedObject(config);
        Debug.Log("[WizardChess] Created AbilityBalanceConfig asset in Resources/");
    }

    private void OnGUI()
    {
        if (config == null || serializedConfig == null)
        {
            DrawNoConfigUI();
            return;
        }

        serializedConfig.Update();

        EditorGUILayout.BeginHorizontal();

        // Left panel: element tabs + piece list
        DrawLeftPanel();

        // Separator
        EditorGUILayout.BeginVertical(GUILayout.Width(1));
        GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));
        EditorGUILayout.EndVertical();

        // Right panel: ability details
        DrawRightPanel();

        EditorGUILayout.EndHorizontal();

        if (serializedConfig.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(config);
        }
    }

    private void DrawNoConfigUI()
    {
        GUILayout.Space(20);
        EditorGUILayout.HelpBox(
            "No AbilityBalanceConfig asset found. Create one to start balancing abilities.",
            MessageType.Info);
        GUILayout.Space(10);
        if (GUILayout.Button("Create Config Asset", GUILayout.Height(30)))
        {
            CreateConfigAsset();
        }
    }

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(140));

        // Save button
        if (GUILayout.Button("Save", GUILayout.Height(24)))
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[WizardChess] Ability config saved.");
        }

        GUILayout.Space(6);

        // Element tabs
        EditorGUILayout.LabelField("Element", EditorStyles.boldLabel);
        for (int i = 0; i < ElementNames.Length; i++)
        {
            Color oldBg = GUI.backgroundColor;
            if (selectedElement == i)
                GUI.backgroundColor = ElementColors[i];
            if (GUILayout.Button(ElementNames[i], GUILayout.Height(26)))
            {
                selectedElement = i;
            }
            GUI.backgroundColor = oldBg;
        }

        GUILayout.Space(10);

        // Piece list
        EditorGUILayout.LabelField("Piece", EditorStyles.boldLabel);
        for (int i = 0; i < PieceNames.Length; i++)
        {
            var style = selectedPiece == i ? EditorStyles.toolbarButton : EditorStyles.miniButton;
            if (GUILayout.Button(PieceNames[i], style, GUILayout.Height(22)))
            {
                selectedPiece = i;
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }

    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical();
        rightPanelScroll = EditorGUILayout.BeginScrollView(rightPanelScroll);

        int elementId = ElementIds[selectedElement];
        int pieceType = PieceTypes[selectedPiece];
        string title = ElementNames[selectedElement].ToUpper() + " " + PieceNames[selectedPiece].ToUpper();

        // Header
        Color oldColor = GUI.contentColor;
        GUI.contentColor = ElementColors[selectedElement];
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        GUI.contentColor = oldColor;
        DrawHorizontalLine();

        GUILayout.Space(4);

        // Passive section
        string defaultPassiveName = GetHardcodedPassiveName(elementId, pieceType);
        string defaultPassiveDesc = GetHardcodedPassiveDescription(elementId, pieceType);

        EditorGUILayout.LabelField("PASSIVE", EditorStyles.boldLabel);
        DrawTextOverrideField("Name", GetTextPropertyPath(elementId, pieceType, "passiveName"), defaultPassiveName);
        DrawTextOverrideArea("Description", GetTextPropertyPath(elementId, pieceType, "passiveDescription"), defaultPassiveDesc);
        GUILayout.Space(2);
        DrawPassiveParams(elementId, pieceType);

        GUILayout.Space(10);
        DrawHorizontalLine();
        GUILayout.Space(4);

        // Active section with cooldown
        string defaultActiveName = GetHardcodedActiveName(elementId, pieceType);
        string defaultActiveDesc = GetHardcodedActiveDescription(elementId, pieceType);

        EditorGUILayout.LabelField("ACTIVE", EditorStyles.boldLabel);
        DrawTextOverrideField("Name", GetTextPropertyPath(elementId, pieceType, "activeName"), defaultActiveName);

        // Cooldown field
        DrawCooldownField(pieceType);

        DrawTextOverrideArea("Description", GetTextPropertyPath(elementId, pieceType, "activeDescription"), defaultActiveDesc);
        GUILayout.Space(2);
        DrawActiveParams(elementId, pieceType);

        GUILayout.Space(10);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawCooldownField(int pieceType)
    {
        SerializedProperty cooldowns = serializedConfig.FindProperty("cooldowns");
        string fieldName = GetCooldownFieldName(pieceType);
        if (cooldowns != null && fieldName != null)
        {
            SerializedProperty prop = cooldowns.FindPropertyRelative(fieldName);
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop, new GUIContent("Cooldown (turns)"));
            }
        }
    }

    private string GetCooldownFieldName(int pieceType)
    {
        switch (pieceType)
        {
            case ChessConstants.PAWN: return "pawn";
            case ChessConstants.ROOK: return "rook";
            case ChessConstants.KNIGHT: return "knight";
            case ChessConstants.BISHOP: return "bishop";
            case ChessConstants.QUEEN: return "queen";
            case ChessConstants.KING: return "king";
            default: return null;
        }
    }

    private void DrawPassiveParams(int elementId, int pieceType)
    {
        string path = GetPassivePropertyPath(elementId, pieceType);
        if (path == null) return;

        SerializedProperty prop = serializedConfig.FindProperty(path);
        if (prop == null) return;

        if (!prop.hasChildren || CountSerializableFields(prop) == 0)
        {
            EditorGUILayout.LabelField("(No tunable parameters)", EditorStyles.miniLabel);
            return;
        }

        DrawChildProperties(prop);
    }

    private void DrawActiveParams(int elementId, int pieceType)
    {
        string path = GetActivePropertyPath(elementId, pieceType);
        if (path == null) return;

        SerializedProperty prop = serializedConfig.FindProperty(path);
        if (prop == null) return;

        if (!prop.hasChildren || CountSerializableFields(prop) == 0)
        {
            EditorGUILayout.LabelField("(No tunable parameters)", EditorStyles.miniLabel);
            return;
        }

        DrawChildProperties(prop);
    }

    private int CountSerializableFields(SerializedProperty prop)
    {
        int count = 0;
        SerializedProperty iter = prop.Copy();
        SerializedProperty end = prop.GetEndProperty();
        iter.NextVisible(true);
        while (!SerializedProperty.EqualContents(iter, end))
        {
            count++;
            if (!iter.NextVisible(false)) break;
        }
        return count;
    }

    private void DrawChildProperties(SerializedProperty prop)
    {
        SerializedProperty iter = prop.Copy();
        SerializedProperty end = prop.GetEndProperty();
        iter.NextVisible(true);
        while (!SerializedProperty.EqualContents(iter, end))
        {
            EditorGUILayout.PropertyField(iter, true);
            if (!iter.NextVisible(false)) break;
        }
    }

    private string GetPassivePropertyPath(int elementId, int pieceType)
    {
        string element = GetElementFieldName(elementId);
        string piece = GetPiecePassiveFieldName(pieceType);
        if (element == null || piece == null) return null;
        return element + "." + piece;
    }

    private string GetActivePropertyPath(int elementId, int pieceType)
    {
        string element = GetElementFieldName(elementId);
        string piece = GetPieceActiveFieldName(pieceType);
        if (element == null || piece == null) return null;
        return element + "." + piece;
    }

    private string GetElementFieldName(int elementId)
    {
        switch (elementId)
        {
            case ChessConstants.ELEMENT_FIRE: return "fire";
            case ChessConstants.ELEMENT_EARTH: return "earth";
            case ChessConstants.ELEMENT_LIGHTNING: return "lightning";
            default: return null;
        }
    }

    private string GetPiecePassiveFieldName(int pieceType)
    {
        switch (pieceType)
        {
            case ChessConstants.PAWN: return "pawnPassive";
            case ChessConstants.ROOK: return "rookPassive";
            case ChessConstants.KNIGHT: return "knightPassive";
            case ChessConstants.BISHOP: return "bishopPassive";
            case ChessConstants.QUEEN: return "queenPassive";
            case ChessConstants.KING: return "kingPassive";
            default: return null;
        }
    }

    private string GetPieceActiveFieldName(int pieceType)
    {
        switch (pieceType)
        {
            case ChessConstants.PAWN: return "pawnActive";
            case ChessConstants.ROOK: return "rookActive";
            case ChessConstants.KNIGHT: return "knightActive";
            case ChessConstants.BISHOP: return "bishopActive";
            case ChessConstants.QUEEN: return "queenActive";
            case ChessConstants.KING: return "kingActive";
            default: return null;
        }
    }

    private void DrawHorizontalLine()
    {
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
    }

    // ========== Text Override Helpers ==========

    private string GetTextPropertyPath(int elementId, int pieceType, string field)
    {
        string element = GetElementFieldName(elementId);
        string pieceText = GetPieceTextFieldName(pieceType);
        if (element == null || pieceText == null) return null;
        return element + "." + pieceText + "." + field;
    }

    private string GetPieceTextFieldName(int pieceType)
    {
        switch (pieceType)
        {
            case ChessConstants.PAWN: return "pawnText";
            case ChessConstants.ROOK: return "rookText";
            case ChessConstants.KNIGHT: return "knightText";
            case ChessConstants.BISHOP: return "bishopText";
            case ChessConstants.QUEEN: return "queenText";
            case ChessConstants.KING: return "kingText";
            default: return null;
        }
    }

    private void DrawTextOverrideField(string label, string propertyPath, string defaultValue)
    {
        if (propertyPath == null)
        {
            EditorGUILayout.LabelField(label, defaultValue);
            return;
        }

        SerializedProperty prop = serializedConfig.FindProperty(propertyPath);
        if (prop == null)
        {
            EditorGUILayout.LabelField(label, defaultValue);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        string current = prop.stringValue;
        string placeholder = string.IsNullOrEmpty(current) ? defaultValue : current;
        string newValue = EditorGUILayout.TextField(label, placeholder);
        // Only write if user actually changed the text
        if (newValue != placeholder)
            prop.stringValue = newValue;
        else if (!string.IsNullOrEmpty(current) && newValue == current)
            prop.stringValue = current; // keep existing override

        if (!string.IsNullOrEmpty(prop.stringValue) && GUILayout.Button("Reset", GUILayout.Width(50)))
        {
            prop.stringValue = "";
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTextOverrideArea(string label, string propertyPath, string defaultValue)
    {
        if (propertyPath == null)
        {
            EditorGUILayout.HelpBox(defaultValue, MessageType.None);
            return;
        }

        SerializedProperty prop = serializedConfig.FindProperty(propertyPath);
        if (prop == null)
        {
            EditorGUILayout.HelpBox(defaultValue, MessageType.None);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
        string current = prop.stringValue;
        string displayValue = string.IsNullOrEmpty(current) ? defaultValue : current;
        string newValue = EditorGUILayout.TextArea(displayValue, GUILayout.MinHeight(36));
        if (newValue != displayValue)
            prop.stringValue = newValue;
        else if (!string.IsNullOrEmpty(current) && newValue == current)
            prop.stringValue = current;
        EditorGUILayout.EndVertical();

        if (!string.IsNullOrEmpty(prop.stringValue))
        {
            if (GUILayout.Button("Reset", GUILayout.Width(50), GUILayout.Height(36)))
            {
                prop.stringValue = "";
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    // ========== Hardcoded Defaults (bypass config overrides) ==========

    private static string GetHardcodedPassiveName(int elementId, int pieceType)
    {
        if (elementId == ChessConstants.ELEMENT_FIRE)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Scorched Earth";
                case ChessConstants.ROOK: return "Trail Blazer";
                case ChessConstants.KNIGHT: return "Splash Damage";
                case ChessConstants.BISHOP: return "Burning Path";
                case ChessConstants.QUEEN: return "Royal Inferno";
                case ChessConstants.KING: return "Ember Aura";
            }
        }
        else if (elementId == ChessConstants.ELEMENT_EARTH)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Shield Wall";
                case ChessConstants.ROOK: return "Fortified";
                case ChessConstants.KNIGHT: return "Tremor Hop";
                case ChessConstants.BISHOP: return "Earthen Shield";
                case ChessConstants.QUEEN: return "Tectonic Presence";
                case ChessConstants.KING: return "Bedrock Throne";
            }
        }
        else if (elementId == ChessConstants.ELEMENT_LIGHTNING)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Energized";
                case ChessConstants.ROOK: return "Overcharge";
                case ChessConstants.KNIGHT: return "Double Jump";
                case ChessConstants.BISHOP: return "Voltage Burst";
                case ChessConstants.QUEEN: return "Swiftness";
                case ChessConstants.KING: return "Reactive Blink";
            }
        }
        return "None";
    }

    private static string GetHardcodedPassiveDescription(int elementId, int pieceType)
    {
        if (elementId == ChessConstants.ELEMENT_FIRE)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "When captured, leaves Fire on its square for 2 turns.";
                case ChessConstants.ROOK: return "After moving, departure square becomes Fire for 1 turn.";
                case ChessConstants.KNIGHT: return "When capturing, adjacent enemies become Singed.";
                case ChessConstants.BISHOP: return "After moving, first traversed square becomes Fire for 1 turn.";
                case ChessConstants.QUEEN: return "Immune to Fire Squares.";
                case ChessConstants.KING: return "4 orthogonal squares are always Fire while King is there.";
            }
        }
        else if (elementId == ChessConstants.ELEMENT_EARTH)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Cannot be captured by higher-value pieces if a friendly piece is adjacent.";
                case ChessConstants.ROOK: return "Cannot be captured while on its starting square.";
                case ChessConstants.KNIGHT: return "After moving, one adjacent enemy is Stunned for 1 turn.";
                case ChessConstants.BISHOP: return "When captured, the capturing piece is Stunned for 1 turn.";
                case ChessConstants.QUEEN: return "All friendly Stone Walls have +1 HP.";
                case ChessConstants.KING: return "Cannot be checked while on starting square.";
            }
        }
        else if (elementId == ChessConstants.ELEMENT_LIGHTNING)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Can always move 2 squares forward if both empty.";
                case ChessConstants.ROOK: return "Can pass through one friendly piece during its move.";
                case ChessConstants.KNIGHT: return "After moving, may move 1 extra square in a cardinal direction.";
                case ChessConstants.BISHOP: return "After moving 3+ squares, adjacent enemies become Singed.";
                case ChessConstants.QUEEN: return "Can also move like a Knight (no capture in L-shape).";
                case ChessConstants.KING: return "Once per game, when checked, blink to a safe square within 2.";
            }
        }
        return "";
    }

    private static string GetHardcodedActiveName(int elementId, int pieceType)
    {
        if (elementId == ChessConstants.ELEMENT_FIRE)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Flame Rush";
                case ChessConstants.ROOK: return "Inferno Line";
                case ChessConstants.KNIGHT: return "Eruption";
                case ChessConstants.BISHOP: return "Flame Cross";
                case ChessConstants.QUEEN: return "Meteor Strike";
                case ChessConstants.KING: return "Backdraft";
            }
        }
        else if (elementId == ChessConstants.ELEMENT_EARTH)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Barricade";
                case ChessConstants.ROOK: return "Rampart";
                case ChessConstants.KNIGHT: return "Earthquake";
                case ChessConstants.BISHOP: return "Petrify";
                case ChessConstants.QUEEN: return "Continental Divide";
                case ChessConstants.KING: return "Sanctuary";
            }
        }
        else if (elementId == ChessConstants.ELEMENT_LIGHTNING)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Chain Strike";
                case ChessConstants.ROOK: return "Thunder Strike";
                case ChessConstants.KNIGHT: return "Lightning Rod";
                case ChessConstants.BISHOP: return "Arc Flash";
                case ChessConstants.QUEEN: return "Tempest";
                case ChessConstants.KING: return "Static Field";
            }
        }
        return "Ability";
    }

    private static string GetHardcodedActiveDescription(int elementId, int pieceType)
    {
        if (elementId == ChessConstants.ELEMENT_FIRE)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Move forward 1-3, create Fire on passed squares.";
                case ChessConstants.ROOK: return "Fire line in a direction (4 sq). First enemy captured.";
                case ChessConstants.KNIGHT: return "Create Fire on all 8 adjacent squares for 2 turns.";
                case ChessConstants.BISHOP: return "Create Fire in + pattern (2 sq each) for 2 turns.";
                case ChessConstants.QUEEN: return "3x3 Fire zone on target. Captures first enemy in zone.";
                case ChessConstants.KING: return "All Fire Squares capture adjacent enemies, then remove all Fire.";
            }
        }
        else if (elementId == ChessConstants.ELEMENT_EARTH)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Create Stone Wall in front of pawn for 3 turns.";
                case ChessConstants.ROOK: return "Create up to 3 Stone Walls in one direction.";
                case ChessConstants.KNIGHT: return "Stun all enemies within 2 squares for 1 turn.";
                case ChessConstants.BISHOP: return "Turn enemy piece into Stone Wall for 2 turns.";
                case ChessConstants.QUEEN: return "Line of Stone Walls (up to 5) in any direction.";
                case ChessConstants.KING: return "Adjacent squares become Stone Walls for 2 turns.";
            }
        }
        else if (elementId == ChessConstants.ELEMENT_LIGHTNING)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Move forward, chain-capture up to 3 diagonal enemies.";
                case ChessConstants.ROOK: return "Teleport to any legal square, ignoring blockers.";
                case ChessConstants.KNIGHT: return "Teleport within 5 sq. Stun enemies adj to both spots.";
                case ChessConstants.BISHOP: return "Swap positions with any friendly piece.";
                case ChessConstants.QUEEN: return "Push enemies within 3 sq away. Off-board = captured.";
                case ChessConstants.KING: return "For 2 turns, enemies moving adjacent are Stunned.";
            }
        }
        return "";
    }
}
