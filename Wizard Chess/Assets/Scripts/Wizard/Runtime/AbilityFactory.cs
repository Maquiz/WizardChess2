/// <summary>
/// Factory class that creates the correct passive and active ability instances
/// based on element ID and piece type. Reads balance parameters from AbilityBalanceConfig
/// when available, falls back to default constructors otherwise.
/// </summary>
public static class AbilityFactory
{
    public static IPassiveAbility CreatePassive(int elementId, int pieceType)
    {
        switch (elementId)
        {
            case ChessConstants.ELEMENT_FIRE:
                return CreateFirePassive(pieceType);
            case ChessConstants.ELEMENT_EARTH:
                return CreateEarthPassive(pieceType);
            case ChessConstants.ELEMENT_LIGHTNING:
                return CreateLightningPassive(pieceType);
            case ChessConstants.ELEMENT_ICE:
                return CreateIcePassive(pieceType);
            case ChessConstants.ELEMENT_SHADOW:
                return CreateShadowPassive(pieceType);
            default:
                return null;
        }
    }

    public static IActiveAbility CreateActive(int elementId, int pieceType)
    {
        switch (elementId)
        {
            case ChessConstants.ELEMENT_FIRE:
                return CreateFireActive(pieceType);
            case ChessConstants.ELEMENT_EARTH:
                return CreateEarthActive(pieceType);
            case ChessConstants.ELEMENT_LIGHTNING:
                return CreateLightningActive(pieceType);
            case ChessConstants.ELEMENT_ICE:
                return CreateIceActive(pieceType);
            case ChessConstants.ELEMENT_SHADOW:
                return CreateShadowActive(pieceType);
            default:
                return null;
        }
    }

    public static int GetCooldown(int elementId, int pieceType)
    {
        var cfg = AbilityBalanceConfig.Instance;
        if (cfg != null)
            return cfg.cooldowns.Get(pieceType);

        // Fallback hardcoded defaults
        switch (pieceType)
        {
            case ChessConstants.PAWN: return 3;
            case ChessConstants.ROOK: return 5;
            case ChessConstants.KNIGHT: return 4;
            case ChessConstants.BISHOP: return 5;
            case ChessConstants.QUEEN: return 7;
            case ChessConstants.KING: return 8;
            default: return 0;
        }
    }

    // ========== Fire ==========

    private static IPassiveAbility CreateFirePassive(int pieceType)
    {
        var cfg = AbilityBalanceConfig.Instance;
        switch (pieceType)
        {
            case ChessConstants.PAWN:
                return cfg != null ? new FirePawnPassive(cfg.fire.pawnPassive) : new FirePawnPassive();
            case ChessConstants.ROOK:
                return cfg != null ? new FireRookPassive(cfg.fire.rookPassive) : new FireRookPassive();
            case ChessConstants.KNIGHT:
                return cfg != null ? new FireKnightPassive(cfg.fire.knightPassive) : new FireKnightPassive();
            case ChessConstants.BISHOP:
                return cfg != null ? new FireBishopPassive(cfg.fire.bishopPassive) : new FireBishopPassive();
            case ChessConstants.QUEEN:
                return new FireQueenPassive();
            case ChessConstants.KING:
                return new FireKingPassive();
            default: return null;
        }
    }

    private static IActiveAbility CreateFireActive(int pieceType)
    {
        var cfg = AbilityBalanceConfig.Instance;
        switch (pieceType)
        {
            case ChessConstants.PAWN:
                return cfg != null ? new FirePawnActive(cfg.fire.pawnActive) : new FirePawnActive();
            case ChessConstants.ROOK:
                return cfg != null ? new FireRookActive(cfg.fire.rookActive) : new FireRookActive();
            case ChessConstants.KNIGHT:
                return cfg != null ? new FireKnightActive(cfg.fire.knightActive) : new FireKnightActive();
            case ChessConstants.BISHOP:
                return cfg != null ? new FireBishopActive(cfg.fire.bishopActive) : new FireBishopActive();
            case ChessConstants.QUEEN:
                return cfg != null ? new FireQueenActive(cfg.fire.queenActive) : new FireQueenActive();
            case ChessConstants.KING:
                return new FireKingActive();
            default: return null;
        }
    }

    // ========== Earth ==========

    private static IPassiveAbility CreateEarthPassive(int pieceType)
    {
        var cfg = AbilityBalanceConfig.Instance;
        switch (pieceType)
        {
            case ChessConstants.PAWN:
                return new EarthPawnPassive();
            case ChessConstants.ROOK:
                return new EarthRookPassive();
            case ChessConstants.KNIGHT:
                return cfg != null ? new EarthKnightPassive(cfg.earth.knightPassive) : new EarthKnightPassive();
            case ChessConstants.BISHOP:
                return cfg != null ? new EarthBishopPassive(cfg.earth.bishopPassive) : new EarthBishopPassive();
            case ChessConstants.QUEEN:
                return cfg != null ? new EarthQueenPassive(cfg.earth.queenPassive) : new EarthQueenPassive();
            case ChessConstants.KING:
                return new EarthKingPassive();
            default: return null;
        }
    }

    private static IActiveAbility CreateEarthActive(int pieceType)
    {
        var cfg = AbilityBalanceConfig.Instance;
        switch (pieceType)
        {
            case ChessConstants.PAWN:
                return cfg != null ? new EarthPawnActive(cfg.earth.pawnActive) : new EarthPawnActive();
            case ChessConstants.ROOK:
                return cfg != null ? new EarthRookActive(cfg.earth.rookActive) : new EarthRookActive();
            case ChessConstants.KNIGHT:
                return cfg != null ? new EarthKnightActive(cfg.earth.knightActive) : new EarthKnightActive();
            case ChessConstants.BISHOP:
                return cfg != null ? new EarthBishopActive(cfg.earth.bishopActive) : new EarthBishopActive();
            case ChessConstants.QUEEN:
                return cfg != null ? new EarthQueenActive(cfg.earth.queenActive) : new EarthQueenActive();
            case ChessConstants.KING:
                return cfg != null ? new EarthKingActive(cfg.earth.kingActive) : new EarthKingActive();
            default: return null;
        }
    }

    // ========== Lightning ==========

    private static IPassiveAbility CreateLightningPassive(int pieceType)
    {
        var cfg = AbilityBalanceConfig.Instance;
        switch (pieceType)
        {
            case ChessConstants.PAWN:
                return cfg != null ? new LightningPawnPassive(cfg.lightning.pawnPassive) : new LightningPawnPassive();
            case ChessConstants.ROOK:
                return cfg != null ? new LightningRookPassive(cfg.lightning.rookPassive) : new LightningRookPassive();
            case ChessConstants.KNIGHT:
                return cfg != null ? new LightningKnightPassive(cfg.lightning.knightPassive) : new LightningKnightPassive();
            case ChessConstants.BISHOP:
                return cfg != null ? new LightningBishopPassive(cfg.lightning.bishopPassive) : new LightningBishopPassive();
            case ChessConstants.QUEEN:
                return cfg != null ? new LightningQueenPassive(cfg.lightning.queenPassive) : new LightningQueenPassive();
            case ChessConstants.KING:
                return cfg != null ? new LightningKingPassive(cfg.lightning.kingPassive) : new LightningKingPassive();
            default: return null;
        }
    }

    private static IActiveAbility CreateLightningActive(int pieceType)
    {
        var cfg = AbilityBalanceConfig.Instance;
        switch (pieceType)
        {
            case ChessConstants.PAWN:
                return cfg != null ? new LightningPawnActive(cfg.lightning.pawnActive) : new LightningPawnActive();
            case ChessConstants.ROOK:
                return new LightningRookActive();
            case ChessConstants.KNIGHT:
                return cfg != null ? new LightningKnightActive(cfg.lightning.knightActive) : new LightningKnightActive();
            case ChessConstants.BISHOP:
                return new LightningBishopActive();
            case ChessConstants.QUEEN:
                return cfg != null ? new LightningQueenActive(cfg.lightning.queenActive) : new LightningQueenActive();
            case ChessConstants.KING:
                return cfg != null ? new LightningKingActive(cfg.lightning.kingActive) : new LightningKingActive();
            default: return null;
        }
    }

    // ========== Ice ==========

    private static IPassiveAbility CreateIcePassive(int pieceType)
    {
        var cfg = AbilityBalanceConfig.Instance;
        switch (pieceType)
        {
            case ChessConstants.PAWN:
                return cfg != null ? new IcePawnPassive(cfg.ice.pawnPassive) : new IcePawnPassive();
            case ChessConstants.ROOK:
                return cfg != null ? new IceRookPassive(cfg.ice.rookPassive) : new IceRookPassive();
            case ChessConstants.KNIGHT:
                return cfg != null ? new IceKnightPassive(cfg.ice.knightPassive) : new IceKnightPassive();
            case ChessConstants.BISHOP:
                return cfg != null ? new IceBishopPassive(cfg.ice.bishopPassive) : new IceBishopPassive();
            case ChessConstants.QUEEN:
                return cfg != null ? new IceQueenPassive(cfg.ice.queenPassive) : new IceQueenPassive();
            case ChessConstants.KING:
                return cfg != null ? new IceKingPassive(cfg.ice.kingPassive) : new IceKingPassive();
            default: return null;
        }
    }

    private static IActiveAbility CreateIceActive(int pieceType)
    {
        var cfg = AbilityBalanceConfig.Instance;
        switch (pieceType)
        {
            case ChessConstants.PAWN:
                return cfg != null ? new IcePawnActive(cfg.ice.pawnActive) : new IcePawnActive();
            case ChessConstants.ROOK:
                return cfg != null ? new IceRookActive(cfg.ice.rookActive) : new IceRookActive();
            case ChessConstants.KNIGHT:
                return cfg != null ? new IceKnightActive(cfg.ice.knightActive) : new IceKnightActive();
            case ChessConstants.BISHOP:
                return cfg != null ? new IceBishopActive(cfg.ice.bishopActive) : new IceBishopActive();
            case ChessConstants.QUEEN:
                return cfg != null ? new IceQueenActive(cfg.ice.queenActive) : new IceQueenActive();
            case ChessConstants.KING:
                return cfg != null ? new IceKingActive(cfg.ice.kingActive) : new IceKingActive();
            default: return null;
        }
    }

    // ========== Shadow ==========

    private static IPassiveAbility CreateShadowPassive(int pieceType)
    {
        var cfg = AbilityBalanceConfig.Instance;
        switch (pieceType)
        {
            case ChessConstants.PAWN:
                return cfg != null ? new ShadowPawnPassive(cfg.shadow.pawnPassive) : new ShadowPawnPassive();
            case ChessConstants.ROOK:
                return cfg != null ? new ShadowRookPassive(cfg.shadow.rookPassive) : new ShadowRookPassive();
            case ChessConstants.KNIGHT:
                return cfg != null ? new ShadowKnightPassive(cfg.shadow.knightPassive) : new ShadowKnightPassive();
            case ChessConstants.BISHOP:
                return cfg != null ? new ShadowBishopPassive(cfg.shadow.bishopPassive) : new ShadowBishopPassive();
            case ChessConstants.QUEEN:
                return cfg != null ? new ShadowQueenPassive(cfg.shadow.queenPassive) : new ShadowQueenPassive();
            case ChessConstants.KING:
                return cfg != null ? new ShadowKingPassive(cfg.shadow.kingPassive) : new ShadowKingPassive();
            default: return null;
        }
    }

    private static IActiveAbility CreateShadowActive(int pieceType)
    {
        var cfg = AbilityBalanceConfig.Instance;
        switch (pieceType)
        {
            case ChessConstants.PAWN:
                return cfg != null ? new ShadowPawnActive(cfg.shadow.pawnActive) : new ShadowPawnActive();
            case ChessConstants.ROOK:
                return cfg != null ? new ShadowRookActive(cfg.shadow.rookActive) : new ShadowRookActive();
            case ChessConstants.KNIGHT:
                return cfg != null ? new ShadowKnightActive(cfg.shadow.knightActive) : new ShadowKnightActive();
            case ChessConstants.BISHOP:
                return cfg != null ? new ShadowBishopActive(cfg.shadow.bishopActive) : new ShadowBishopActive();
            case ChessConstants.QUEEN:
                return cfg != null ? new ShadowQueenActive(cfg.shadow.queenActive) : new ShadowQueenActive();
            case ChessConstants.KING:
                return cfg != null ? new ShadowKingActive(cfg.shadow.kingActive) : new ShadowKingActive();
            default: return null;
        }
    }
}
