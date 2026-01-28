using UnityEngine;

/// <summary>
/// Central ScriptableObject holding all tunable balance parameters for all 36 wizard chess abilities.
/// Accessed at runtime via AbilityBalanceConfig.Instance (Resources.Load singleton).
/// Created via Assets > Create > WizardChess > Ability Balance Config.
/// </summary>
[CreateAssetMenu(fileName = "AbilityBalanceConfig", menuName = "WizardChess/Ability Balance Config")]
public class AbilityBalanceConfig : ScriptableObject
{
    private static AbilityBalanceConfig _instance;

    /// <summary>
    /// Singleton accessor. Loads from Resources/ folder. Returns null if no asset exists.
    /// </summary>
    public static AbilityBalanceConfig Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<AbilityBalanceConfig>("AbilityBalanceConfig");
            return _instance;
        }
    }

    /// <summary>
    /// Get the text override for a given element and piece type, or null if none exists.
    /// </summary>
    public AbilityTextOverride GetTextOverride(int elementId, int pieceType)
    {
        switch (elementId)
        {
            case ChessConstants.ELEMENT_FIRE: return fire.GetTextOverride(pieceType);
            case ChessConstants.ELEMENT_EARTH: return earth.GetTextOverride(pieceType);
            case ChessConstants.ELEMENT_LIGHTNING: return lightning.GetTextOverride(pieceType);
            default: return null;
        }
    }

    [Header("Cooldowns (turns)")]
    public CooldownConfig cooldowns = new CooldownConfig();

    [Header("Fire Abilities")]
    public FireAbilityParams fire = new FireAbilityParams();

    [Header("Earth Abilities")]
    public EarthAbilityParams earth = new EarthAbilityParams();

    [Header("Lightning Abilities")]
    public LightningAbilityParams lightning = new LightningAbilityParams();
}

// ============================================================
// Cooldown Config
// ============================================================

[System.Serializable]
public class CooldownConfig
{
    [Tooltip("Pawn active ability cooldown")]
    public int pawn = 3;
    [Tooltip("Rook active ability cooldown")]
    public int rook = 5;
    [Tooltip("Knight active ability cooldown")]
    public int knight = 4;
    [Tooltip("Bishop active ability cooldown")]
    public int bishop = 5;
    [Tooltip("Queen active ability cooldown")]
    public int queen = 7;
    [Tooltip("King active ability cooldown")]
    public int king = 8;

    public int Get(int pieceType)
    {
        switch (pieceType)
        {
            case ChessConstants.PAWN: return pawn;
            case ChessConstants.ROOK: return rook;
            case ChessConstants.KNIGHT: return knight;
            case ChessConstants.BISHOP: return bishop;
            case ChessConstants.QUEEN: return queen;
            case ChessConstants.KING: return king;
            default: return 0;
        }
    }
}

// ============================================================
// Fire Ability Params
// ============================================================

[System.Serializable]
public class FireAbilityParams
{
    public AbilityTextOverride pawnText = new AbilityTextOverride();
    public FirePawnPassiveParams pawnPassive = new FirePawnPassiveParams();
    public FirePawnActiveParams pawnActive = new FirePawnActiveParams();
    public AbilityTextOverride rookText = new AbilityTextOverride();
    public FireRookPassiveParams rookPassive = new FireRookPassiveParams();
    public FireRookActiveParams rookActive = new FireRookActiveParams();
    public AbilityTextOverride knightText = new AbilityTextOverride();
    public FireKnightPassiveParams knightPassive = new FireKnightPassiveParams();
    public FireKnightActiveParams knightActive = new FireKnightActiveParams();
    public AbilityTextOverride bishopText = new AbilityTextOverride();
    public FireBishopPassiveParams bishopPassive = new FireBishopPassiveParams();
    public FireBishopActiveParams bishopActive = new FireBishopActiveParams();
    public AbilityTextOverride queenText = new AbilityTextOverride();
    public FireQueenPassiveParams queenPassive = new FireQueenPassiveParams();
    public FireQueenActiveParams queenActive = new FireQueenActiveParams();
    public AbilityTextOverride kingText = new AbilityTextOverride();
    public FireKingPassiveParams kingPassive = new FireKingPassiveParams();
    public FireKingActiveParams kingActive = new FireKingActiveParams();

    public AbilityTextOverride GetTextOverride(int pieceType)
    {
        switch (pieceType)
        {
            case ChessConstants.PAWN: return pawnText;
            case ChessConstants.ROOK: return rookText;
            case ChessConstants.KNIGHT: return knightText;
            case ChessConstants.BISHOP: return bishopText;
            case ChessConstants.QUEEN: return queenText;
            case ChessConstants.KING: return kingText;
            default: return null;
        }
    }
}

[System.Serializable]
public class FirePawnPassiveParams
{
    [Tooltip("Duration of fire left on square when pawn is captured (turns)")]
    public int fireDuration = 2;
}

[System.Serializable]
public class FirePawnActiveParams
{
    [Tooltip("Maximum forward range for Flame Rush (squares)")]
    public int maxForwardRange = 3;
    [Tooltip("Duration of fire trail left on passed squares (turns)")]
    public int fireTrailDuration = 2;
}

[System.Serializable]
public class FireRookPassiveParams
{
    [Tooltip("Duration of fire on departure square (turns)")]
    public int fireDuration = 1;
}

[System.Serializable]
public class FireRookActiveParams
{
    [Tooltip("Length of fire line (squares)")]
    public int lineLength = 4;
    [Tooltip("Duration of fire on line squares (turns)")]
    public int fireDuration = 2;
    [Tooltip("Maximum enemies captured in the line")]
    public int maxCaptures = 1;
}

[System.Serializable]
public class FireKnightPassiveParams
{
    [Tooltip("If true, singe enemies in all 8 directions; if false, only orthogonal (4 directions)")]
    public bool includeDiagonals = false;
}

[System.Serializable]
public class FireKnightActiveParams
{
    [Tooltip("Duration of fire on adjacent squares (turns)")]
    public int fireDuration = 2;
}

[System.Serializable]
public class FireBishopPassiveParams
{
    [Tooltip("Duration of fire on first traversed square (turns)")]
    public int fireDuration = 1;
}

[System.Serializable]
public class FireBishopActiveParams
{
    [Tooltip("Length of each arm of the + pattern (squares)")]
    public int armLength = 2;
    [Tooltip("Duration of fire on cross squares (turns)")]
    public int fireDuration = 2;
    [Tooltip("Whether bishop gains fire immunity after using Flame Cross")]
    public bool grantFireImmunity = true;
}

[System.Serializable]
public class FireQueenPassiveParams
{
    // Royal Inferno: immunity is set externally via ElementalPiece.AddImmunity
    // No tunable parameters
}

[System.Serializable]
public class FireQueenActiveParams
{
    [Tooltip("Radius of the fire zone around target (1 = 3x3)")]
    public int aoeRadius = 1;
    [Tooltip("Duration of fire in the zone (turns)")]
    public int fireDuration = 3;
    [Tooltip("Maximum enemies captured in the zone")]
    public int maxCaptures = 1;
}

[System.Serializable]
public class FireKingPassiveParams
{
    // Ember Aura: always-on orthogonal fire, no tunable parameters
}

[System.Serializable]
public class FireKingActiveParams
{
    // Backdraft: captures all fire-adjacent enemies, removes all fire
    // No tunable parameters
}

// ============================================================
// Earth Ability Params
// ============================================================

[System.Serializable]
public class EarthAbilityParams
{
    public AbilityTextOverride pawnText = new AbilityTextOverride();
    public EarthPawnPassiveParams pawnPassive = new EarthPawnPassiveParams();
    public EarthPawnActiveParams pawnActive = new EarthPawnActiveParams();
    public AbilityTextOverride rookText = new AbilityTextOverride();
    public EarthRookPassiveParams rookPassive = new EarthRookPassiveParams();
    public EarthRookActiveParams rookActive = new EarthRookActiveParams();
    public AbilityTextOverride knightText = new AbilityTextOverride();
    public EarthKnightPassiveParams knightPassive = new EarthKnightPassiveParams();
    public EarthKnightActiveParams knightActive = new EarthKnightActiveParams();
    public AbilityTextOverride bishopText = new AbilityTextOverride();
    public EarthBishopPassiveParams bishopPassive = new EarthBishopPassiveParams();
    public EarthBishopActiveParams bishopActive = new EarthBishopActiveParams();
    public AbilityTextOverride queenText = new AbilityTextOverride();
    public EarthQueenPassiveParams queenPassive = new EarthQueenPassiveParams();
    public EarthQueenActiveParams queenActive = new EarthQueenActiveParams();
    public AbilityTextOverride kingText = new AbilityTextOverride();
    public EarthKingPassiveParams kingPassive = new EarthKingPassiveParams();
    public EarthKingActiveParams kingActive = new EarthKingActiveParams();

    public AbilityTextOverride GetTextOverride(int pieceType)
    {
        switch (pieceType)
        {
            case ChessConstants.PAWN: return pawnText;
            case ChessConstants.ROOK: return rookText;
            case ChessConstants.KNIGHT: return knightText;
            case ChessConstants.BISHOP: return bishopText;
            case ChessConstants.QUEEN: return queenText;
            case ChessConstants.KING: return kingText;
            default: return null;
        }
    }
}

[System.Serializable]
public class EarthPawnPassiveParams
{
    // Shield Wall: binary logic (adjacent friendly = protected), no tunable parameters
}

[System.Serializable]
public class EarthPawnActiveParams
{
    [Tooltip("Hit points of the created stone wall")]
    public int wallHP = 2;
    [Tooltip("Duration of the stone wall (turns)")]
    public int wallDuration = 3;
}

[System.Serializable]
public class EarthRookPassiveParams
{
    // Fortified: binary logic (on starting square = invulnerable), no tunable parameters
}

[System.Serializable]
public class EarthRookActiveParams
{
    [Tooltip("Maximum number of stone walls created in a line")]
    public int maxWalls = 3;
    [Tooltip("Hit points of each stone wall")]
    public int wallHP = 2;
    [Tooltip("Duration of stone walls (turns)")]
    public int wallDuration = 3;
}

[System.Serializable]
public class EarthKnightPassiveParams
{
    [Tooltip("Duration of stun applied to adjacent enemy (turns)")]
    public int stunDuration = 1;
    [Tooltip("Maximum number of enemies stunned after moving")]
    public int maxTargets = 1;
}

[System.Serializable]
public class EarthKnightActiveParams
{
    [Tooltip("Manhattan distance range for Earthquake stun")]
    public int range = 2;
    [Tooltip("Duration of stun applied to affected enemies (turns)")]
    public int stunDuration = 1;
}

[System.Serializable]
public class EarthBishopPassiveParams
{
    [Tooltip("Duration of stun applied to capturing piece (turns)")]
    public int stunDuration = 1;
}

[System.Serializable]
public class EarthBishopActiveParams
{
    [Tooltip("Duration of stun applied to petrified enemy (turns)")]
    public int stunDuration = 2;
    [Tooltip("Hit points of the stone wall placed on enemy")]
    public int wallHP = 1;
    [Tooltip("Duration of the stone wall placed on enemy (turns)")]
    public int wallDuration = 2;
}

[System.Serializable]
public class EarthQueenPassiveParams
{
    [Tooltip("Bonus HP added to all friendly stone walls")]
    public int bonusHP = 1;
}

[System.Serializable]
public class EarthQueenActiveParams
{
    [Tooltip("Maximum number of stone walls in the line")]
    public int maxWalls = 5;
    [Tooltip("Hit points of each stone wall")]
    public int wallHP = 3;
    [Tooltip("Duration of stone walls (turns)")]
    public int wallDuration = 4;
}

[System.Serializable]
public class EarthKingPassiveParams
{
    // Bedrock Throne: binary logic (on starting square = immune to check), no tunable parameters
}

[System.Serializable]
public class EarthKingActiveParams
{
    [Tooltip("Hit points of adjacent stone walls")]
    public int wallHP = 1;
    [Tooltip("Duration of adjacent stone walls (turns)")]
    public int wallDuration = 2;
    [Tooltip("Duration of stun on the king itself (turns)")]
    public int selfStunDuration = 2;
    [Tooltip("Duration of stun on adjacent friendly pieces (turns)")]
    public int allyStunDuration = 2;
}

// ============================================================
// Lightning Ability Params
// ============================================================

[System.Serializable]
public class LightningAbilityParams
{
    public AbilityTextOverride pawnText = new AbilityTextOverride();
    public LtPawnPassiveParams pawnPassive = new LtPawnPassiveParams();
    public LtPawnActiveParams pawnActive = new LtPawnActiveParams();
    public AbilityTextOverride rookText = new AbilityTextOverride();
    public LtRookPassiveParams rookPassive = new LtRookPassiveParams();
    public LtRookActiveParams rookActive = new LtRookActiveParams();
    public AbilityTextOverride knightText = new AbilityTextOverride();
    public LtKnightPassiveParams knightPassive = new LtKnightPassiveParams();
    public LtKnightActiveParams knightActive = new LtKnightActiveParams();
    public AbilityTextOverride bishopText = new AbilityTextOverride();
    public LtBishopPassiveParams bishopPassive = new LtBishopPassiveParams();
    public LtBishopActiveParams bishopActive = new LtBishopActiveParams();
    public AbilityTextOverride queenText = new AbilityTextOverride();
    public LtQueenPassiveParams queenPassive = new LtQueenPassiveParams();
    public LtQueenActiveParams queenActive = new LtQueenActiveParams();
    public AbilityTextOverride kingText = new AbilityTextOverride();
    public LtKingPassiveParams kingPassive = new LtKingPassiveParams();
    public LtKingActiveParams kingActive = new LtKingActiveParams();

    public AbilityTextOverride GetTextOverride(int pieceType)
    {
        switch (pieceType)
        {
            case ChessConstants.PAWN: return pawnText;
            case ChessConstants.ROOK: return rookText;
            case ChessConstants.KNIGHT: return knightText;
            case ChessConstants.BISHOP: return bishopText;
            case ChessConstants.QUEEN: return queenText;
            case ChessConstants.KING: return kingText;
            default: return null;
        }
    }
}

[System.Serializable]
public class LtPawnPassiveParams
{
    [Tooltip("Extra forward range always available (squares)")]
    public int extraForwardRange = 2;
}

[System.Serializable]
public class LtPawnActiveParams
{
    [Tooltip("Maximum chain captures during Chain Strike")]
    public int maxChainCaptures = 3;
}

[System.Serializable]
public class LtRookPassiveParams
{
    [Tooltip("Maximum friendly pieces that can be passed through")]
    public int maxPassthrough = 1;
}

[System.Serializable]
public class LtRookActiveParams
{
    // Thunder Strike: teleport to any empty cardinal square, no tunable parameters
}

[System.Serializable]
public class LtKnightPassiveParams
{
    [Tooltip("Extra cardinal move range after landing (squares)")]
    public int extraMoveRange = 1;
}

[System.Serializable]
public class LtKnightActiveParams
{
    [Tooltip("Manhattan distance teleport range")]
    public int teleportRange = 5;
    [Tooltip("Duration of stun on shared-adjacent enemies (turns)")]
    public int stunDuration = 1;
}

[System.Serializable]
public class LtBishopPassiveParams
{
    [Tooltip("Minimum move distance to trigger Voltage Burst singe (Chebyshev)")]
    public int minMoveDistance = 3;
}

[System.Serializable]
public class LtBishopActiveParams
{
    // Arc Flash: swap with any friendly piece, no tunable parameters
}

[System.Serializable]
public class LtQueenPassiveParams
{
    [Tooltip("If true, knight-move squares also allow captures")]
    public bool allowKnightCapture = false;
}

[System.Serializable]
public class LtQueenActiveParams
{
    [Tooltip("Chebyshev distance to detect enemies for push (squares)")]
    public int detectionRange = 3;
    [Tooltip("Distance enemies are pushed away (squares)")]
    public int pushDistance = 2;
}

[System.Serializable]
public class LtKingPassiveParams
{
    [Tooltip("Maximum distance for reactive blink (squares)")]
    public int blinkRange = 2;
}

[System.Serializable]
public class LtKingActiveParams
{
    [Tooltip("Duration of lightning field on adjacent squares (turns)")]
    public int fieldDuration = 2;
}

// ============================================================
// Ability Text Overrides
// ============================================================

[System.Serializable]
public class AbilityTextOverride
{
    public string passiveName = "";
    [TextArea(2, 4)] public string passiveDescription = "";
    public string activeName = "";
    [TextArea(2, 4)] public string activeDescription = "";
}
