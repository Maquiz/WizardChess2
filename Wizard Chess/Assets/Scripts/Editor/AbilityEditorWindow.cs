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
        string passiveName = AbilityInfo.GetPassiveName(elementId, pieceType);
        string passiveDesc = AbilityInfo.GetPassiveDescription(elementId, pieceType);
        EditorGUILayout.LabelField("PASSIVE: " + passiveName, EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(passiveDesc, MessageType.None);
        GUILayout.Space(2);
        DrawPassiveParams(elementId, pieceType);

        GUILayout.Space(10);
        DrawHorizontalLine();
        GUILayout.Space(4);

        // Active section with cooldown
        string activeName = AbilityInfo.GetActiveName(elementId, pieceType);
        string activeDesc = AbilityInfo.GetActiveDescription(elementId, pieceType);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("ACTIVE: " + activeName, EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        // Cooldown field
        DrawCooldownField(pieceType);

        EditorGUILayout.HelpBox(activeDesc, MessageType.None);
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
}
