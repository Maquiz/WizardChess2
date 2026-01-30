/// <summary>
/// Static helper providing ability names and descriptions for tooltip display.
/// </summary>
public static class AbilityInfo
{
    public static string GetElementName(int elementId)
    {
        switch (elementId)
        {
            case ChessConstants.ELEMENT_FIRE: return "Fire";
            case ChessConstants.ELEMENT_EARTH: return "Earth";
            case ChessConstants.ELEMENT_LIGHTNING: return "Lightning";
            case ChessConstants.ELEMENT_ICE: return "Ice";
            case ChessConstants.ELEMENT_SHADOW: return "Shadow";
            default: return "None";
        }
    }

    public static string GetPassiveName(int elementId, int pieceType)
    {
        var cfg = AbilityBalanceConfig.Instance;
        if (cfg != null)
        {
            var t = cfg.GetTextOverride(elementId, pieceType);
            if (t != null && !string.IsNullOrEmpty(t.passiveName))
                return t.passiveName;
        }

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
                case ChessConstants.KING: return "Stone Shield";
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
        else if (elementId == ChessConstants.ELEMENT_ICE)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Frostbite";
                case ChessConstants.ROOK: return "Glacial Wake";
                case ChessConstants.KNIGHT: return "Frozen Hoof";
                case ChessConstants.BISHOP: return "Rime Trail";
                case ChessConstants.QUEEN: return "Permafrost Aura";
                case ChessConstants.KING: return "Frozen Heart";
            }
        }
        else if (elementId == ChessConstants.ELEMENT_SHADOW)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Shadow Step";
                case ChessConstants.ROOK: return "Looming Presence";
                case ChessConstants.KNIGHT: return "Phantom Rider";
                case ChessConstants.BISHOP: return "Dark Corners";
                case ChessConstants.QUEEN: return "Mistress of Shadows";
                case ChessConstants.KING: return "Cloak of Shadows";
            }
        }
        return "None";
    }

    public static string GetPassiveDescription(int elementId, int pieceType)
    {
        var cfg = AbilityBalanceConfig.Instance;
        if (cfg != null)
        {
            var t = cfg.GetTextOverride(elementId, pieceType);
            if (t != null && !string.IsNullOrEmpty(t.passiveDescription))
                return t.passiveDescription;
        }

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
                case ChessConstants.KING: return "Once per game, survives capture and destroys the attacker.";
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
        else if (elementId == ChessConstants.ELEMENT_ICE)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "When capturing, adjacent enemies become Chilled.";
                case ChessConstants.ROOK: return "After moving, departure square becomes Ice for 2 turns.";
                case ChessConstants.KNIGHT: return "After landing, one adjacent enemy is Frozen for 1 turn.";
                case ChessConstants.BISHOP: return "After moving 3+ squares, all traversed squares become Ice.";
                case ChessConstants.QUEEN: return "Immune to all Ice effects (Ice squares, Frozen, Chilled).";
                case ChessConstants.KING: return "Once per game, when checked, freeze all checking pieces.";
            }
        }
        else if (elementId == ChessConstants.ELEMENT_SHADOW)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "After a non-capture move, become Veiled (type hidden).";
                case ChessConstants.ROOK: return "At turn start, all adjacent enemies become Marked.";
                case ChessConstants.KNIGHT: return "After capturing, become Veiled for 2 turns.";
                case ChessConstants.BISHOP: return "After moving 3+ squares, leave ShadowVeil on departure.";
                case ChessConstants.QUEEN: return "Always Veiled. All friendly Veil durations +1 turn.";
                case ChessConstants.KING: return "Permanently Veiled (type hidden, but check still visible).";
            }
        }
        return "";
    }

    public static string GetActiveName(int elementId, int pieceType)
    {
        var cfg = AbilityBalanceConfig.Instance;
        if (cfg != null)
        {
            var t = cfg.GetTextOverride(elementId, pieceType);
            if (t != null && !string.IsNullOrEmpty(t.activeName))
                return t.activeName;
        }

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
        else if (elementId == ChessConstants.ELEMENT_ICE)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Flash Freeze";
                case ChessConstants.ROOK: return "Avalanche";
                case ChessConstants.KNIGHT: return "Blizzard Leap";
                case ChessConstants.BISHOP: return "Deep Freeze";
                case ChessConstants.QUEEN: return "Absolute Zero";
                case ChessConstants.KING: return "Glacial Fortress";
            }
        }
        else if (elementId == ChessConstants.ELEMENT_SHADOW)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Shadow Lunge";
                case ChessConstants.ROOK: return "Shadowmeld";
                case ChessConstants.KNIGHT: return "Doppelganger";
                case ChessConstants.BISHOP: return "Eclipse";
                case ChessConstants.QUEEN: return "Umbral Assault";
                case ChessConstants.KING: return "Shadow Swap";
            }
        }
        return "Ability";
    }

    public static string GetActiveDescription(int elementId, int pieceType)
    {
        var cfg = AbilityBalanceConfig.Instance;
        if (cfg != null)
        {
            var t = cfg.GetTextOverride(elementId, pieceType);
            if (t != null && !string.IsNullOrEmpty(t.activeDescription))
                return t.activeDescription;
        }

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
        else if (elementId == ChessConstants.ELEMENT_ICE)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Freeze an enemy within 2 squares for 1 turn.";
                case ChessConstants.ROOK: return "Ice line in a direction. Push and Chill enemies.";
                case ChessConstants.KNIGHT: return "Knight move + create Ice on all 8 adjacent squares.";
                case ChessConstants.BISHOP: return "Freeze target enemy and all adjacent enemies.";
                case ChessConstants.QUEEN: return "3x3 Ice zone on target. Freeze all enemies inside.";
                case ChessConstants.KING: return "Create Ice on all 8 adjacent squares + gain Ice immunity.";
            }
        }
        else if (elementId == ChessConstants.ELEMENT_SHADOW)
        {
            switch (pieceType)
            {
                case ChessConstants.PAWN: return "Capture an enemy 1-2 squares directly forward.";
                case ChessConstants.ROOK: return "Teleport within 4 sq. Leave ShadowVeil on departure.";
                case ChessConstants.KNIGHT: return "Create a Shadow Decoy on any knight-move square.";
                case ChessConstants.BISHOP: return "Veil all friendly pieces in a 2x2 area.";
                case ChessConstants.QUEEN: return "Move up to 3 sq, Mark all adjacent enemies after.";
                case ChessConstants.KING: return "Swap with any friendly piece within 3 sq. Both Veiled.";
            }
        }
        return "";
    }

    // ========== Square Effect Descriptions ==========

    public static string GetSquareEffectName(SquareEffectType type)
    {
        switch (type)
        {
            case SquareEffectType.Fire: return "Fire Square";
            case SquareEffectType.StoneWall: return "Stone Wall";
            case SquareEffectType.LightningField: return "Lightning Field";
            case SquareEffectType.Ice: return "Ice Square";
            case SquareEffectType.ShadowVeil: return "Shadow Veil";
            case SquareEffectType.ShadowDecoy: return "Shadow Decoy";
            default: return "None";
        }
    }

    public static string GetSquareEffectDescription(SquareEffectType type)
    {
        switch (type)
        {
            case SquareEffectType.Fire:
                return "Blocks movement for all pieces. Fire Queens are immune. Created by various Fire abilities. Expires after a set number of turns.";
            case SquareEffectType.StoneWall:
                return "Blocks movement for all pieces. Has hit points and can be destroyed by attacks. Created by Earth abilities. Earth Queen grants +1 HP to all friendly Stone Walls.";
            case SquareEffectType.LightningField:
                return "Does NOT block movement. Any piece that enters a Lightning Field square is Stunned. Created by Lightning King's active ability.";
            case SquareEffectType.Ice:
                return "Does NOT block movement. Pieces entering may slide in their movement direction. Ice Queens are immune. Created by various Ice abilities.";
            case SquareEffectType.ShadowVeil:
                return "Does NOT block movement. Pieces standing on this square have their type hidden from the opponent. Created by Shadow abilities.";
            case SquareEffectType.ShadowDecoy:
                return "Appears as a fake piece. When 'captured', disappears without being a real capture. Created by Shadow Knight's Doppelganger ability.";
            default: return "";
        }
    }

    public static string GetSquareEffectColor(SquareEffectType type)
    {
        switch (type)
        {
            case SquareEffectType.Fire: return "FF6600";
            case SquareEffectType.StoneWall: return "996633";
            case SquareEffectType.LightningField: return "3399FF";
            case SquareEffectType.Ice: return "66CCFF";
            case SquareEffectType.ShadowVeil: return "663399";
            case SquareEffectType.ShadowDecoy: return "330066";
            default: return "FFFFFF";
        }
    }

    // ========== Status Effect Descriptions ==========

    public static string GetStatusEffectName(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Stunned: return "Stunned";
            case StatusEffectType.Singed: return "Singed";
            case StatusEffectType.Frozen: return "Frozen";
            case StatusEffectType.Chilled: return "Chilled";
            case StatusEffectType.Veiled: return "Veiled";
            case StatusEffectType.Marked: return "Marked";
            default: return "None";
        }
    }

    public static string GetStatusEffectDescription(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Stunned:
                return "Cannot move or use abilities for the duration. Caused by Earth Knight (Tremor Hop / Earthquake), Earth Bishop (Earthen Shield), and Lightning Field squares.";
            case StatusEffectType.Singed:
                return "Automatically captured when attacked by any piece. One-hit vulnerability. Caused by Fire Knight (Splash Damage) and Lightning Bishop (Voltage Burst).";
            case StatusEffectType.Frozen:
                return "Cannot move or use abilities for the duration. Caused by various Ice abilities. Ice Queens are immune.";
            case StatusEffectType.Chilled:
                return "Sliding pieces (Rook, Bishop, Queen) have halved movement range. Caused by Ice Pawn and Ice Rook abilities.";
            case StatusEffectType.Veiled:
                return "Piece type is hidden from the opponent (shown as '?'). Check status is still visible. Caused by various Shadow abilities.";
            case StatusEffectType.Marked:
                return "Takes bonus damage on next attack. Marked pieces can be captured even when normally protected. Caused by Shadow Rook and Shadow Queen abilities.";
            default: return "";
        }
    }

    public static string GetStatusEffectColor(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Stunned: return "FFAA00";
            case StatusEffectType.Singed: return "FF6600";
            case StatusEffectType.Frozen: return "66CCFF";
            case StatusEffectType.Chilled: return "99DDFF";
            case StatusEffectType.Veiled: return "663399";
            case StatusEffectType.Marked: return "FF0066";
            default: return "FFFFFF";
        }
    }
}
