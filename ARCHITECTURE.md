# ARCHITECTURE.md — WizardChess2 Class Reference

> **Last updated:** 2026-01-26
>
> This document is the source of truth for class responsibilities, public APIs, and system data flow.
> Update this file whenever classes are added/removed or public interfaces change.

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Data Flow](#data-flow)
3. [Class Reference — Core Chess](#class-reference--core-chess)
   - [GameMaster](#gamemaster)
   - [BoardState](#boardstate)
   - [PieceMove](#piecemove)
   - [Square](#square)
   - [ChessMove](#chessmove)
   - [ChessConstants](#chessconstants)
   - [CameraMove](#cameramove)
   - [PieceUI](#pieceui)
   - [BoardUI](#boardui)
   - [OutofBounds](#outofbounds)
   - [PieceCheck](#piececheck)
4. [Class Reference — Wizard System](#class-reference--wizard-system)
   - [Interfaces](#interfaces)
   - [Data Classes](#data-classes)
   - [Runtime Classes](#runtime-classes)
   - [Draft System](#draft-system)
   - [UI Classes](#ui-classes)
   - [Editor Tools](#editor-tools)
   - [Ability Classes](#ability-classes)
5. [Enums](#enums)
6. [Dependencies Between Classes](#dependencies-between-classes)
7. [Prefabs & Tags](#prefabs--tags)
8. [File Map](#file-map)

---

## System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                        GameMaster                            │
│  (Orchestrator: input, turns, game state, UI, draft)        │
│                                                              │
│  Owns: BoardState, moveHistory, enPassantTarget              │
│  Owns: SquareEffectManager, AbilityExecutor                  │
│  Manages: piece selection, move execution, ability mode,     │
│           draft phase, game end, tooltip UI                  │
└─────┬──────────┬──────────────┬──────────────┬──────────────┘
      │          │              │              │
      ▼          ▼              ▼              ▼
┌──────────┐ ┌───────────┐ ┌────────────┐ ┌────────────────┐
│BoardState│ │PieceMove   │ │SquareEffect│ │AbilityExecutor │
│(Data)    │ │(x32)       │ │Manager     │ │(Ability mode)  │
│          │◄│            │ │            │ │                │
│Piece grid│ │Move gen    │ │Square effs │ │Target/execute  │
│Attack map│ │Castling    │ │Fire/Wall/  │ │Cooldown trigger│
│Check det.│ │En Passant  │ │Lightning   │ └────────────────┘
│Move sim. │ │Promotion   │ └────────────┘
└──────────┘ │Animation   │
             │            │
             │ElementalPiece──► IPassiveAbility
             │(optional)   ──► IActiveAbility
             │             ──► CooldownTracker
             │             ──► StatusEffect[]
             └─────┬───────┘
                   │
                   ▼
             ┌───────────┐
             │Square(x64)│
             │Occupancy  │
             │Move indic.│
             │activeEffect│
             └───────────┘

  ┌─────────────────────────────┐
  │       Draft System          │
  │  DraftManager               │
  │  DraftUI                    │
  │  DraftData                  │
  │  AbilityFactory             │
  └─────────────────────────────┘
```

---

## Scene Flow

```
MainMenu Scene                           Board Scene
┌──────────────────┐                    ┌──────────────────┐
│ Title Screen     │                    │ GameMaster.Start()│
│   Play →         │                    │   MatchConfig set?│
│   Manage Decks → │                    │   YES → DeckBased │
│   Examine Pieces→│                    │          Setup    │
│   Quit           │                    │   NO  → FireVs   │
│                  │  LoadScene("Board")│          EarthSetup│
│ Deck Select:     │───────────────────>│                  │
│   P1 picks deck  │                    │ Game Over UI:    │
│   P2 picks deck  │<───────────────────│   Rematch / Menu │
│   Start Match    │  LoadScene("Menu") │                  │
└──────────────────┘                    └──────────────────┘
```

- **MainMenu scene** (`Assets/Scenes/MainMenu.unity`): Title screen with 4 panels (Title, DeckSelect, DeckEditor, PieceExamine). Managed by `MainMenuUI`.
- **Board scene** (`Assets/Scenes/Board.unity`): Chess gameplay. `GameMaster.Start()` checks `MatchConfig` to decide setup mode.
- **Cross-scene data**: `MatchConfig` static class holds `DraftData` and `useDeckSystem` flag between scene loads.

---

## Data Flow

### Move Execution Sequence (with Wizard hooks)

```
1. Player clicks piece       → PieceMove.OnMouseDown()
                             → [NEW] Stun check (blocked if stunned)
2. GameMaster.selectPiece()  → PieceMove.createPieceMoves()
                             → [NEW] passive.ModifyMoveGeneration()
                             → [NEW] SquareEffectManager filter (blocked squares)
                             → [NEW] FilterProtectedCaptures() (passive-blocked captures)
                             → filterIllegalMoves()
3. Player clicks target      → GameMaster.Update() raycast

   PATH A: Normal Move
     4. Validate checkMoves() → HashSet O(1) lookup
     5. Capture? TryCapture() → [NEW] passive.OnBeforeCapture() (can prevent)
                              → takePiece() → pieceTaken() → BoardState.RemovePiece()
                              → [NEW] passive.OnAfterCapture()
                              → [NEW] passive.OnPieceCaptured() (defender)
     6. PieceMove.movePiece() → DOTween animation
                              → BoardState.MovePiece() + RecalculateAttacks()
                              → En passant / castling / promotion
                              → [NEW] passive.OnAfterMove()
     7. EndTurn()

   PATH B: Active Ability
     4. AbilityExecutor.EnterAbilityMode()
     5. GetTargetSquares() → highlight valid targets
     6. Player clicks target → Execute() → ability effect
     7. CooldownTracker.StartCooldown()
     8. EndTurn()

EndTurn():
  1. currentMove toggles 1↔2
  2. turnNumber++
  3. SquareEffectManager.TickAllEffects() — decrement/remove expired
  4. NotifyTurnStart() — tick cooldowns, status effects for all pieces
  5. EvaluateGameState() — Check / Checkmate / Stalemate
```

### Draft Phase Sequence

```
1. DraftManager.StartDraft()    → isDraftPhase = true
2. White selects elements       → DraftUI → DraftData.SetElement()
3. White confirms               → DraftManager.ConfirmPlayerDraft()
4. Black selects elements       → DraftUI → DraftData.SetElement()
5. Black confirms               → DraftManager.CompleteDraft()
6. ApplyDraftToGame()           → AbilityFactory creates abilities
                                → ElementalPiece attached to each piece
7. isDraftPhase = false         → Game begins
```

### Initialization Sequence

```
1. GameMaster.Start()        → Creates board squares, initializes BoardState
                             → Creates SquareEffectManager, AbilityExecutor
2. Pieces fall onto squares  → Square.OnTriggerEnter()
3. PieceMove.setIntitialPiece() → Registers with BoardState, generates initial moves
```

---

## Class Reference — Core Chess

### GameMaster
**File:** `Scripts/GameMaster.cs`
**Inherits:** `MonoBehaviour`
**Tag:** `"GM"`
**Role:** Central orchestrator — handles input, game loop, turn management, UI, draft phase, ability mode, and game state evaluation.

#### Public Fields
| Field | Type | Description |
|-------|------|-------------|
| `BPieces` | `GameObject[]` | Black piece GameObjects |
| `WPieces` | `GameObject[]` | White piece GameObjects |
| `boardPos` | `GameObject[,]` | 8x8 grid of square GameObjects |
| `boardRows` | `GameObject[]` | Row parent transforms (used for square lookup) |
| `Board` | `GameObject` | Board root object |
| `boardSize` | `int` | Board dimension (8) |
| `currentMove` | `int` | Whose turn: `2`=White, `1`=Black |
| `letters` | `string[]` | Column labels A-K |
| `boardUI` | `Canvas` | UI canvas |
| `selectedUI` | `PieceUI` | Selected piece indicator |
| `canMoveUI` | `PieceUI` | Valid move cursor |
| `cantMoveUI` | `PieceUI` | Invalid move cursor |
| `takeMoveUI` | `PieceUI` | Capture cursor |
| `lr` | `LineRenderer` | Move preview line |
| `lineMaterial` | `Material` | Line renderer material |
| `isPieceSelected` | `bool` | Selection state flag |
| `selectedPiece` | `PieceMove` | Currently selected piece |
| `moveHistory` | `Stack<ChessMove>` | Move history stack |
| `boardState` | `BoardState` | Centralized board data |
| `currentGameState` | `GameState` | Current game state enum |
| `enPassantTarget` | `Square` | En passant target square (or null) |
| `blackSquare` | `GameObject` | Dark square prefab |
| `whiteSquare` | `GameObject` | Light square prefab |
| `showMoves` | `bool` | Move display toggle |
| `squareEffectManager` | `SquareEffectManager` | **[NEW]** Manages square effects |
| `abilityExecutor` | `AbilityExecutor` | **[NEW]** Handles ability targeting mode |
| `turnNumber` | `int` | **[NEW]** Current turn counter |
| `isDraftPhase` | `bool` | **[NEW]** Blocks gameplay during draft |

#### Public Methods
| Method | Signature | Description |
|--------|-----------|-------------|
| `RegisterPiece` | `(PieceMove piece, int x, int y)` | Register piece with BoardState at init |
| `UpdateBoardState` | `(PieceMove piece, int fromX, int fromY, int toX, int toY)` | Update BoardState after a move |
| `RemovePieceFromBoardState` | `(int x, int y)` | Remove captured piece from BoardState |
| `selectPiece` | `(Transform t, PieceMove piece)` | Select a piece and generate its moves |
| `takePiece` | `(PieceMove p)` | Execute a capture |
| `EndTurn` | `()` | **[NEW]** Swap turn, tick effects/cooldowns, evaluate state |
| `EnterAbilityMode` | `(PieceMove piece)` | **[NEW]** Enter ability targeting mode |
| `TryCapture` | `(PieceMove attacker, PieceMove defender) → bool` | **[NEW]** Capture with passive hooks (returns false if blocked) |

#### Private Methods
| Method | Description |
|--------|-------------|
| `deSelectPiece()` | Clear selection state and hide UI |
| `createBoard(int size)` | Instantiate 8x8 checkerboard of squares |
| `EvaluateGameState()` | Check/checkmate/stalemate detection after each move |
| `HasAnyLegalMoves(int color)` | Whether a player has any legal moves |
| `OnGameOver()` | Handle game end (currently Debug.Log only) |
| `setUpLine()` | Configure LineRenderer for move preview |
| `swapUIIcon(MouseUI m)` | Switch cursor feedback icon |
| `NotifyTurnStart(int)` | **[NEW]** Notify all pieces of turn start |
| `OnGameOver()` | **[NEW]** Handle game over — exits ability mode, deselects piece, logs result |

---

### BoardState
**File:** `Scripts/BoardState.cs`
**Inherits:** Plain C# class (not MonoBehaviour)
**Role:** Single source of truth for piece positions, attack maps, and check detection. Provides move simulation for legality checks.

#### Properties
| Property | Type | Description |
|----------|------|-------------|
| `whiteKing` | `PieceMove` | White king reference |
| `blackKing` | `PieceMove` | Black king reference |

#### Public Methods
| Method | Signature | Description |
|--------|-----------|-------------|
| `GetPieceAt` | `(int x, int y) → PieceMove` | Get piece at position (null if empty) |
| `SetPieceAt` | `(int x, int y, PieceMove piece)` | Place piece at position |
| `MovePiece` | `(int fromX, int fromY, int toX, int toY)` | Move piece between positions |
| `RemovePiece` | `(int x, int y)` | Remove piece from board |
| `IsSquareEmpty` | `(int x, int y) → bool` | Check if position is empty |
| `IsInBounds` | `(int x, int y) → bool` | Bounds check |
| `GetAllPieces` | `(int color) → List<PieceMove>` | Get all pieces of a color |
| `IsSquareAttackedBy` | `(int x, int y, int attackerColor) → bool` | O(1) attack query |
| `IsKingInCheck` | `(int kingColor) → bool` | Is king in check? (Bedrock Throne: Earth King immune on starting square) |
| `RecalculateAttacks` | `()` | Rebuild attack maps for both colors |
| `WouldMoveLeaveKingInCheck` | `(PieceMove piece, int toX, int toY) → bool` | Simulate move to test legality |
| `Clone` | `() → BoardState` | Shallow copy for simulation |
| `IsSquareBlockedByEffect` | `(int x, int y, PieceMove, SquareEffectManager) → bool` | **[NEW]** Delegate to SEM |
| `DebugPrintBoard` | `()` | Print board to console |

---

### PieceMove
**File:** `Scripts/PieceMove.cs`
**Inherits:** `MonoBehaviour`
**Tag:** `"Piece"`
**Role:** Attached to each chess piece. Handles move generation, movement animation, special rules (castling, en passant, promotion), input, and elemental ability hooks.

#### Public Fields
| Field | Type | Description |
|-------|------|-------------|
| `color` | `int` | `1`=Black, `2`=White |
| `piece` | `int` | Piece type (1-6, see ChessConstants) |
| `curx`, `cury` | `int` | Current board position |
| `lastx`, `lasty` | `int` | Previous board position |
| `moves` | `List<Square>` | Current legal moves |
| `canMove` | `bool` | Whether piece can move |
| `showMoves` | `bool` | Whether moves are displayed |
| `firstMove` | `bool` | True until piece has moved |
| `curSquare` | `Square` | Current square reference |
| `Board` | `GameObject` | Board reference |
| `gm` | `GameMaster` | GameMaster reference |
| `elementalPiece` | `ElementalPiece` | **[NEW]** Optional elemental component |

#### Move Generation Pipeline (createPieceMoves)
```
1. Generate pseudo-legal moves (King/Queen/Bishop/Knight/Rook/Pawn)
2. [NEW] passive.ModifyMoveGeneration() — add/remove moves based on element
3. [NEW] Filter squares blocked by SquareEffectManager
4. [NEW] FilterProtectedCaptures() — remove captures blocked by passive abilities
5. filterIllegalMoves() — remove moves that leave king in check
```

---

### Square
**File:** `Scripts/Square.cs`
**Inherits:** `MonoBehaviour`
**Tag:** `"Board"`
**Role:** Represents one board position. Tracks occupancy, active square effects, and handles initial piece placement.

#### Public Fields
| Field | Type | Description |
|-------|------|-------------|
| `taken` | `bool` | Whether a piece occupies this square |
| `piece` | `PieceMove` | Occupying piece (null if empty) |
| `x`, `y` | `int` | Board coordinates |
| `showMove` | `bool` | Move indicator flag |
| `showMoveSquare` | `GameObject` | Visual move indicator child object |
| `activeEffect` | `SquareEffect` | **[NEW]** Active elemental effect on this square |

---

### ChessMove
**File:** `Scripts/ChessMove.cs`
**Inherits:** Plain C# class
**Role:** Data structure for recording a single chess move or ability use.

#### Properties
| Property | Type | Description |
|----------|------|-------------|
| `Piece` | `PieceMove` | Piece that moved |
| `TakenPiece` | `PieceMove` | Captured piece (if any) |
| `IsTaken` | `bool` | Whether a capture occurred |
| `IsQueened` | `bool` | Whether promotion occurred |
| `IsCastled` | `bool` | Whether castling occurred |
| `IsAbilityUse` | `bool` | **[NEW]** Whether this was an ability use |

#### Constructors
- `ChessMove(PieceMove pm)` — regular move
- `ChessMove(PieceMove pm, PieceMove tp)` — capture move
- `ChessMove(PieceMove pm, string abilityName, int tX, int tY)` — **[NEW]** ability use

---

### ChessConstants
**File:** `Scripts/ChessConstants.cs`
**Inherits:** Static class
**Role:** Eliminates magic numbers. Defines piece types, colors, board size, direction arrays, element IDs, and piece values.

#### Constants
| Constant | Value | Description |
|----------|-------|-------------|
| `PAWN` | `1` | |
| `ROOK` | `2` | |
| `KNIGHT` | `3` | |
| `BISHOP` | `4` | |
| `QUEEN` | `5` | |
| `KING` | `6` | |
| `BLACK` | `1` | |
| `WHITE` | `2` | |
| `BOARD_SIZE` | `8` | |
| `ELEMENT_NONE` | `0` | **[NEW]** No element |
| `ELEMENT_FIRE` | `1` | **[NEW]** Fire element |
| `ELEMENT_EARTH` | `2` | **[NEW]** Earth element |
| `ELEMENT_LIGHTNING` | `3` | **[NEW]** Lightning element |

#### Static Methods
| Method | Signature | Description |
|--------|-----------|-------------|
| `PieceValue` | `(int pieceType) → int` | **[NEW]** Piece material value (1-100) |

#### Direction Arrays
- `KingDirections` — 8 adjacent squares
- `KnightDirections` — 8 L-shaped jumps
- `RookDirections` — 4 cardinal directions
- `BishopDirections` — 4 diagonal directions

---

### CameraMove
**File:** `Scripts/CameraMove.cs`
**Inherits:** `MonoBehaviour`
**Role:** Keyboard-driven camera control with DOTween transitions. Keys: 1 (White), 2 (Black), 3 (Top-down).

---

### PieceUI, BoardUI, OutofBounds, PieceCheck
Unchanged from original architecture. See previous documentation.

---

## Class Reference — Wizard System

### Interfaces

#### IPassiveAbility
**File:** `Scripts/Wizard/Abilities/IPassiveAbility.cs`
**Role:** Contract for passive elemental abilities. All methods called at specific game events.

| Method | Signature | Description |
|--------|-----------|-------------|
| `ModifyMoveGeneration` | `(List<Square>, PieceMove, BoardState) → List<Square>` | Add/remove moves after base generation |
| `OnBeforeCapture` | `(PieceMove attacker, PieceMove defender, BoardState) → bool` | Return false to prevent capture |
| `OnAfterCapture` | `(PieceMove attacker, PieceMove defender, BoardState)` | Post-capture effects |
| `OnAfterMove` | `(PieceMove, int fromX, int fromY, int toX, int toY, BoardState)` | Post-move effects |
| `OnPieceCaptured` | `(PieceMove capturedPiece, PieceMove capturer, BoardState)` | When this piece is taken |
| `OnTurnStart` | `(int currentTurnColor)` | Per-turn trigger |

#### IActiveAbility
**File:** `Scripts/Wizard/Abilities/IActiveAbility.cs`
**Role:** Contract for active abilities that cost a turn.

| Method | Signature | Description |
|--------|-----------|-------------|
| `CanActivate` | `(PieceMove, BoardState, SquareEffectManager) → bool` | Pre-activation check |
| `GetTargetSquares` | `(PieceMove, BoardState) → List<Square>` | Valid target squares |
| `Execute` | `(PieceMove, Square, BoardState, SquareEffectManager) → bool` | Execute ability |

---

### Data Classes

#### ElementDefinition
**File:** `Scripts/Wizard/Data/ElementDefinition.cs`
**Inherits:** `ScriptableObject`
**Role:** Metadata for an element (name, color, icon, description). Created via Assets > Create > WizardChess > Element Definition.

#### AbilityDefinition
**File:** `Scripts/Wizard/Data/AbilityDefinition.cs`
**Inherits:** `ScriptableObject`
**Role:** Metadata for an ability (name, cooldown, element, piece type, description, implementation class name).

#### AbilityBalanceConfig
**File:** `Scripts/Wizard/Data/AbilityBalanceConfig.cs`
**Inherits:** `ScriptableObject`
**Role:** Central config holding all tunable balance parameters for all 36 wizard chess abilities and cooldowns. Accessed at runtime via `AbilityBalanceConfig.Instance` (singleton via `Resources.Load`). Created via Assets > Create > WizardChess > Ability Balance Config.

| Field | Type | Description |
|-------|------|-------------|
| `cooldowns` | `CooldownConfig` | Per-piece-type active ability cooldowns |
| `fire` | `FireAbilityParams` | All Fire element passive/active params |
| `earth` | `EarthAbilityParams` | All Earth element passive/active params |
| `lightning` | `LightningAbilityParams` | All Lightning element passive/active params |

**Singleton:** `AbilityBalanceConfig.Instance` — loads from `Resources/AbilityBalanceConfig` asset. Returns null if no asset exists (all abilities fall back to hardcoded defaults).

**Structure:** Each element container (e.g., `FireAbilityParams`) holds 12 `[System.Serializable]` param classes (passive + active for each of 6 piece types). Each param class has `[Tooltip]` annotations and default values matching the original hardcoded values.

**Supporting classes** (all in same file):
- `CooldownConfig` — cooldowns per piece type with `Get(int pieceType)` accessor
- `FireAbilityParams`, `EarthAbilityParams`, `LightningAbilityParams` — element containers
- 36 param classes (e.g., `FirePawnPassiveParams`, `EarthKnightActiveParams`, `LtQueenPassiveParams`)

---

### Runtime Classes

#### ElementalPiece
**File:** `Scripts/Wizard/Runtime/ElementalPiece.cs`
**Inherits:** `MonoBehaviour`
**Role:** Attached to pieces with elements. Holds passive/active ability references, cooldown tracker, status effects, and immunities.

| Field/Property | Type | Description |
|----------------|------|-------------|
| `elementId` | `int` | Element constant |
| `passive` | `IPassiveAbility` | Passive ability instance |
| `active` | `IActiveAbility` | Active ability instance |
| `cooldown` | `CooldownTracker` | Cooldown state |
| `pieceMove` | `PieceMove` | Associated PieceMove |
| `hasUsedReactiveBlink` | `bool` | Lightning King once-per-game flag |

| Method | Description |
|--------|-------------|
| `Init(int, IPassiveAbility, IActiveAbility, int)` | Initialize with element and abilities |
| `AddStatusEffect(type, turns, permanent)` | Apply status effect |
| `HasStatusEffect(type) → bool` | Check for status |
| `RemoveStatusEffect(type)` | Remove status |
| `IsStunned() → bool` | Convenience check |
| `IsSinged() → bool` | Convenience check |
| `AddImmunity(SquareEffectType)` | Grant effect immunity |
| `IsImmuneToEffect(SquareEffectType) → bool` | Check immunity |
| `OnTurnStart(int)` | Tick cooldowns and status effects |

#### CooldownTracker
**File:** `Scripts/Wizard/Runtime/CooldownTracker.cs`
**Role:** Plain C# class tracking per-piece ability cooldown.

| Property/Method | Description |
|-----------------|-------------|
| `CurrentCooldown → int` | Remaining turns |
| `MaxCooldown → int` | Max cooldown value |
| `IsReady → bool` | Whether ability can be used |
| `StartCooldown()` | Begin cooldown after use |
| `Tick()` | Decrement by 1 per turn |
| `Reset()` | Set to 0 |

#### StatusEffect
**File:** `Scripts/Wizard/Runtime/StatusEffect.cs`
**Role:** Tracks a status effect on a piece (Stunned, Singed).

| Property | Description |
|----------|-------------|
| `Type` | StatusEffectType enum |
| `RemainingTurns` | Duration |
| `IsPermanentUntilTriggered` | If true, doesn't expire by turns |
| `Tick() → bool` | Returns true if expired |
| `Remove()` | Force-remove |

#### SquareEffect
**File:** `Scripts/Wizard/Runtime/SquareEffect.cs`
**Inherits:** `MonoBehaviour`
**Role:** Component placed on a Square to represent a temporary elemental effect (Fire, StoneWall, LightningField).

| Field | Description |
|-------|-------------|
| `effectType` | SquareEffectType |
| `remainingTurns` | Duration |
| `ownerColor` | Creating player |
| `hitPoints` | HP (for stone walls) |

| Method | Description |
|--------|-------------|
| `Init(Square, type, turns, owner, hp)` | Initialize effect |
| `Tick() → bool` | Returns true if expired |
| `TakeDamage(int) → bool` | Returns true if destroyed |
| `BlocksMovement(PieceMove) → bool` | Whether this blocks the given piece |
| `RemoveEffect()` | Clean up and destroy |

#### SquareEffectManager
**File:** `Scripts/Wizard/Runtime/SquareEffectManager.cs`
**Inherits:** `MonoBehaviour` (singleton on GameMaster object)
**Role:** Manages all active square effects. Handles creation, ticking, querying, and removal.

| Method | Description |
|--------|-------------|
| `Init(GameMaster)` | Initialize |
| `CreateEffect(x, y, type, turns, owner, hp)` | Create new effect |
| `TickAllEffects()` | Tick all, remove expired |
| `RemoveEffect(SquareEffect)` | Remove specific effect |
| `RemoveAllEffectsOfType(type)` | Bulk remove |
| `IsSquareBlocked(x, y, PieceMove) → bool` | Movement query |
| `GetEffectAt(x, y) → SquareEffect` | Get effect at position |
| `GetAllEffectsOfType(type) → List` | Filter query |
| `stoneWallBonusHP` | Earth Queen bonus HP |

#### AbilityExecutor
**File:** `Scripts/Wizard/Runtime/AbilityExecutor.cs`
**Inherits:** `MonoBehaviour`
**Role:** Handles ability targeting mode. Highlights targets, validates clicks, executes abilities.

| Property | Description |
|----------|-------------|
| `isInAbilityMode` | Whether currently targeting |

| Method | Description |
|--------|-------------|
| `Init(GameMaster, SquareEffectManager)` | Initialize |
| `EnterAbilityMode(PieceMove) → bool` | Start targeting (blocked while in check) |
| `TryExecuteOnSquare(x, y) → bool` | Execute on target |
| `ExitAbilityMode()` | Cancel targeting |
| `IsValidTarget(x, y) → bool` | Target validation |

#### AbilityFactory
**File:** `Scripts/Wizard/Runtime/AbilityFactory.cs`
**Role:** Static factory that creates passive/active ability instances based on element + piece type. Reads balance parameters from `AbilityBalanceConfig.Instance` when available, passing param objects to ability constructors. Falls back to parameterless constructors (hardcoded defaults) if no config asset exists.

| Method | Description |
|--------|-------------|
| `CreatePassive(elementId, pieceType) → IPassiveAbility` | Create passive with config params |
| `CreateActive(elementId, pieceType) → IActiveAbility` | Create active with config params |
| `GetCooldown(elementId, pieceType) → int` | Read cooldown from config (fallback to hardcoded) |

#### FireVsEarthSetup
**File:** `Scripts/Wizard/Runtime/FireVsEarthSetup.cs`
**Inherits:** `MonoBehaviour`
**Role:** Auto-assigns Fire element to all White pieces and Earth element to all Black pieces at game start, bypassing the draft system. Waits for pieces to initialize (via physics triggers), then applies elements, attaches `SquareEffectUI` to all squares, and `ElementIndicatorUI` to all pieces.

| Method | Description |
|--------|-------------|
| `Init(GameMaster)` | Initialize with GameMaster reference |

**Activation:** Added automatically by `GameMaster.Start()` when `MatchConfig.useDeckSystem` is false (or no MatchConfig set). Uses `Update()` to wait for all 32 pieces to register with `BoardState`, then applies elements in a single frame. Also attaches `SquareEffectUI` to all squares, `ElementIndicatorUI` to all pieces, and `ElementParticleUI` to all pieces.

**Keyboard Controls:** Press **Q** with a piece selected to enter ability mode. Right-click to cancel. Mouse over any piece to see tooltip with ability details. Click another own piece to switch selection.

#### DeckBasedSetup
**File:** `Scripts/Wizard/Runtime/DeckBasedSetup.cs`
**Inherits:** `MonoBehaviour`
**Role:** Applies per-piece element assignments from `DraftData` (populated by menu deck selection). Unlike `FireVsEarthSetup` which assigns uniform elements per team, this maps each piece to its individually selected element.

| Method | Description |
|--------|-------------|
| `Init(GameMaster, DraftData)` | Initialize with GameMaster and draft data |

**Activation:** Added by `GameMaster.Start()` when `MatchConfig.useDeckSystem` is true. Uses `boardState.GetAllPieces()` (like FireVsEarthSetup) for reliability, then derives each piece's deck-slot index from its type and starting column via `GetPieceIndex()`. Applies elements to all pieces (no ELEMENT_NONE skip).

---

### Menu System

#### MatchConfig
**File:** `Scripts/Menu/MatchConfig.cs`
**Inherits:** Static class
**Role:** Cross-scene data bridge. Populated by the main menu deck selection, read by `GameMaster.Start()` in the Board scene. Survives scene loads via static fields (no `DontDestroyOnLoad` needed).

| Field | Type | Description |
|-------|------|-------------|
| `draftData` | `DraftData` | Element selections for both players |
| `useDeckSystem` | `bool` | Whether to use DeckBasedSetup (false = FireVsEarthSetup) |

| Method | Description |
|--------|-------------|
| `Clear()` | Reset to defaults |

#### DeckSlot
**File:** `Scripts/Menu/DeckSlot.cs`
**Role:** Serializable data class representing a single saved deck. Holds 16 element assignments (one per piece index) and a user-defined name. New decks default all elements to `ELEMENT_FIRE` (no "None" element).

#### DeckSaveData
**File:** `Scripts/Menu/DeckSaveData.cs`
**Role:** Container for all 9 deck slots. Serialized to/from JSON.

#### DeckPersistence
**File:** `Scripts/Menu/DeckPersistence.cs`
**Role:** Static utility for loading/saving `DeckSaveData` to `Application.persistentDataPath/decks.json`.

| Method | Description |
|--------|-------------|
| `Load() → DeckSaveData` | Load from disk (returns fresh data if missing) |
| `Save(DeckSaveData)` | Write to disk |
| `SaveDeck(int, DeckSlot)` | Save a single slot |

#### PieceIndexHelper
**File:** `Scripts/Menu/PieceIndexHelper.cs`
**Role:** Maps piece index 0-15 to piece type, display label, and icon resource path. Index ordering: 0-7=Pawn, 8-9=Rook, 10-11=Knight, 12-13=Bishop, 14=Queen, 15=King.

| Method | Description |
|--------|-------------|
| `GetPieceType(int) → int` | Piece type constant for index |
| `GetPieceLabel(int) → string` | Display label ("Pawn 1", "Queen", etc.) |
| `GetIconResourcePath(int) → string` | Resources path for chess piece icon (e.g. "ChessIcons/pawn") |
| `GetPieceTypeName(int) → string` | Piece type name ("Pawn", "Rook", etc.) |

#### MainMenuUI
**File:** `Scripts/Menu/MainMenuUI.cs`
**Inherits:** `MonoBehaviour`
**Role:** Root controller for MainMenu scene. Creates Canvas + EventSystem at runtime. Manages 4 panels: Title, DeckSelect, DeckEditor, PieceExamine. Provides show/hide navigation and static UI helper methods.

#### DeckSelectPanel
**File:** `Scripts/Menu/DeckSelectPanel.cs`
**Inherits:** `MonoBehaviour`
**Role:** Pre-game deck picking. Phase 1: Player 1 (White) selects deck from 9 slots. Phase 2: same for Player 2. Phase 3: "Start Match!" builds `DraftData`, sets `MatchConfig` (always `useDeckSystem = true`), and loads Board scene. Both players must select a saved deck — no "Standard Chess" option.

#### DeckEditorPanel
**File:** `Scripts/Menu/DeckEditorPanel.cs`
**Inherits:** `MonoBehaviour`
**Role:** Edit a single deck slot. Displays an 8x2 grid of chess pieces using outlined icons tinted by element color. Row 1: back rank (Rook, Knight, Bishop, Queen, King, Bishop, Knight, Rook). Row 2: 8 pawns. Clicking a cell selects it; 3 element buttons (Fire/Earth/Lightning) assign the element. Bulk actions: All Fire, All Earth, All Lightning. "?" button opens PieceExaminePanel. No "None" element option. Navigates between 9 deck slots.

#### PieceExaminePanel
**File:** `Scripts/Menu/PieceExaminePanel.cs`
**Inherits:** `MonoBehaviour`
**Role:** Browse all 18 piece-element ability combinations plus an Effects Glossary. Top: 3 element tabs + "Effects" tab. When an element tab is active: 6 piece type buttons, passive name+desc, active name+desc+cooldown. When "Effects" tab is active: scrollable glossary of all square effects (Fire Square, Stone Wall, Lightning Field) and status effects (Stunned, Singed) with descriptions. Data from `AbilityInfo` and `AbilityFactory.GetCooldown()`.

---

### Draft System

#### DraftManager
**File:** `Scripts/Wizard/Draft/DraftManager.cs`
**Inherits:** `MonoBehaviour`
**Role:** Orchestrates pre-game element selection. White drafts, then Black drafts, then game starts. After applying elements, reads `stoneWallBonusHP` from `AbilityBalanceConfig` for Earth Queen passive.

| Method | Description |
|--------|-------------|
| `Init(GameMaster)` | Initialize |
| `StartDraft()` | Begin draft phase |
| `ConfirmPlayerDraft()` | Confirm current player's selections |
| `SkipDraft()` | Skip to standard chess |

#### DraftUI
**File:** `Scripts/Wizard/Draft/DraftUI.cs`
**Inherits:** `MonoBehaviour`
**Role:** Draft screen UI overlay with piece grid, element buttons, confirm/skip buttons.

#### DraftData
**File:** `Scripts/Wizard/Draft/DraftData.cs`
**Role:** Serializable data holding element selections for all 32 pieces. `int[16]` per player.

---

### UI Classes

#### AbilityUI
**File:** `Scripts/Wizard/UI/AbilityUI.cs`
**Inherits:** `MonoBehaviour`
**Role:** In-game ability button display. Shows ability name, cooldown, and triggers ability mode.

#### SquareEffectUI
**File:** `Scripts/Wizard/UI/SquareEffectUI.cs`
**Inherits:** `MonoBehaviour`
**Role:** Tints square material based on active effect type (fire=red, stone=brown, lightning=blue).

#### ElementIndicatorUI
**File:** `Scripts/Wizard/UI/ElementIndicatorUI.cs`
**Inherits:** `MonoBehaviour`
**Role:** Tints piece material based on assigned element.

#### AbilityInfo
**File:** `Scripts/Wizard/UI/AbilityInfo.cs`
**Inherits:** Static class
**Role:** Lookup tables for ability names and descriptions. Maps (elementId, pieceType) to passive name, passive description, active name, active description, and element display name. Also provides square effect names/descriptions/colors (`GetSquareEffectName`, `GetSquareEffectDescription`, `GetSquareEffectColor`) and status effect names/descriptions/colors (`GetStatusEffectName`, `GetStatusEffectDescription`, `GetStatusEffectColor`). Used by `PieceTooltipUI`, `PieceExaminePanel`, and other UI components.

| Method | Description |
|--------|-------------|
| `GetElementName(int) → string` | Display name for element |
| `GetPassiveName(int, int) → string` | Passive ability name |
| `GetPassiveDescription(int, int) → string` | Short passive description |
| `GetActiveName(int, int) → string` | Active ability name |
| `GetActiveDescription(int, int) → string` | Short active description |

#### PieceTooltipUI
**File:** `Scripts/Wizard/UI/PieceTooltipUI.cs`
**Inherits:** `MonoBehaviour`
**Role:** Mouse-over tooltip showing piece info, element, passive/active abilities, cooldown status, status effects with descriptions, and current square effect info. Attached to GameMaster object. Creates its own UI panel at runtime as a child of the scene Canvas. Uses raycasting to detect hovered pieces. Status effects (Stunned, Singed) and square effects (Fire, Stone Wall, Lightning Field) show full descriptions from `AbilityInfo`.

| Method | Description |
|--------|-------------|
| `ShowTooltip(PieceMove)` | Build and show tooltip for piece |
| `HideTooltip()` | Hide tooltip panel |

#### ElementParticleUI
**File:** `Scripts/Wizard/UI/ElementParticleUI.cs`
**Inherits:** `MonoBehaviour`
**Role:** Adds a subtle ambient particle effect above each elemental piece, colored by element. Fire = orange embers drifting upward, Earth = brown-gold dust settling, Lightning = blue sparks with erratic movement. Creates a child ParticleSystem at runtime.

| Method | Description |
|--------|-------------|
| `CreateParticleEffect(int elementId)` | Creates ParticleSystem with element-specific color, gravity, and velocity |

#### AbilityModeUI
**File:** `Scripts/Wizard/UI/AbilityModeUI.cs`
**Inherits:** `MonoBehaviour`
**Role:** Displays an orange banner at the top of the screen when ability targeting mode is active. Shows the ability name and cancel instructions ("Q or Right-click to cancel"). Watches `AbilityExecutor.isInAbilityMode` state to auto-show/hide. Attached to the GameMaster object.

#### CheckBannerUI
**File:** `Scripts/Wizard/UI/CheckBannerUI.cs`
**Inherits:** `MonoBehaviour`
**Role:** Displays a red banner below the ability mode banner when the current player's king is in check. Shows "CHECK! [White/Black] king is in check!" text. Watches `GameMaster.currentGameState` for WhiteInCheck/BlackInCheck states. Attached to the GameMaster object. Positioned at `anchoredPosition = (0, -65)` to avoid overlap with AbilityModeUI.

#### GameOverUI
**File:** `Scripts/Wizard/UI/GameOverUI.cs`
**Inherits:** `MonoBehaviour`
**Role:** Displays a fullscreen dark overlay with result panel when the game ends (checkmate, stalemate, draw). Shows result text ("CHECKMATE!" / "STALEMATE!" / "DRAW!"), detail text ("White wins the game" etc.), and two buttons: "Rematch" (reloads Board scene keeping MatchConfig) and "Main Menu" (clears MatchConfig, loads MainMenu scene). Watches `GameMaster.currentGameState` for terminal states. Attached to the GameMaster object.

| Method | Description |
|--------|-------------|
| `OnRematchClicked()` | Reloads Board scene with same MatchConfig |
| `OnMainMenuClicked()` | Clears MatchConfig, loads MainMenu scene |

---

### Editor Tools

#### AbilityEditorWindow
**File:** `Scripts/Editor/AbilityEditorWindow.cs`
**Inherits:** `EditorWindow`
**Role:** Custom IMGUI-based editor window for viewing and balancing all 36 wizard chess abilities. Opens via menu: **WizardChess > Ability Balance Editor**.

**Layout:**
- Left panel: Save button, 3 color-coded element tabs (Fire=red-orange, Earth=brown-gold, Lightning=blue), 6 piece type buttons
- Right panel: Passive ability (name, description, editable params), Active ability (name, cooldown, description, editable params)
- Uses `SerializedObject`/`SerializedProperty` for proper undo and dirty support
- Auto-creates `AbilityBalanceConfig` asset in `Resources/` if none found
- Reads ability names and descriptions from `AbilityInfo`

| Method | Description |
|--------|-------------|
| `ShowWindow()` | `[MenuItem]` Opens the editor window |

---

### Ability Classes

36 ability classes (6 piece types × 2 abilities × 3 elements).

**Constructor injection pattern:** Abilities with tunable parameters accept a params object from `AbilityBalanceConfig` via constructor injection. Each has a parameterless fallback constructor with hardcoded defaults:
```csharp
private readonly XxxParams _params;
public ClassName() { _params = new XxxParams(); }           // fallback defaults
public ClassName(XxxParams p) { _params = p; }              // config injection
// Uses _params.field instead of hardcoded values
```

#### Fire (`Scripts/Wizard/Abilities/Fire/`)
| File | Class | Type | Description |
|------|-------|------|-------------|
| `FirePawnPassive.cs` | `FirePawnPassive` | Passive | **Scorched Earth** — When captured, leaves Fire Square for 2 turns |
| `FirePawnActive.cs` | `FirePawnActive` | Active (CD:3) | **Flame Rush** — Move forward 1-3 ignoring blocks, fire trail |
| `FireRookPassive.cs` | `FireRookPassive` | Passive | **Trail Blazer** — Departure square becomes Fire for 1 turn |
| `FireRookActive.cs` | `FireRookActive` | Active (CD:5) | **Inferno Line** — Cardinal fire line (4 sq), capture first enemy |
| `FireKnightPassive.cs` | `FireKnightPassive` | Passive | **Splash Damage** — Adjacent enemies become Singed on capture |
| `FireKnightActive.cs` | `FireKnightActive` | Active (CD:4) | **Eruption** — Fire on all 8 adjacent squares |
| `FireBishopPassive.cs` | `FireBishopPassive` | Passive | **Burning Path** — First traversed square becomes Fire |
| `FireBishopActive.cs` | `FireBishopActive` | Active (CD:5) | **Flame Cross** — + pattern fire, bishop gains fire immunity |
| `FireQueenPassive.cs` | `FireQueenPassive` | Passive | **Royal Inferno** — Immune to Fire Squares (via immunity flag) |
| `FireQueenActive.cs` | `FireQueenActive` | Active (CD:7) | **Meteor Strike** — 3x3 fire zone, capture first enemy |
| `FireKingPassive.cs` | `FireKingPassive` | Passive | **Ember Aura** — 4 orthogonal squares always Fire |
| `FireKingActive.cs` | `FireKingActive` | Active (CD:8) | **Backdraft** — All fire captures adjacent enemies, then remove |

#### Earth (`Scripts/Wizard/Abilities/Earth/`)
| File | Class | Type | Description |
|------|-------|------|-------------|
| `EarthPawnPassive.cs` | `EarthPawnPassive` | Passive | **Shield Wall** — Protected from high-value captures if adjacent friendly |
| `EarthPawnActive.cs` | `EarthPawnActive` | Active (CD:3) | **Barricade** — Stone Wall (2 HP) in front for 3 turns |
| `EarthRookPassive.cs` | `EarthRookPassive` | Passive | **Fortified** — Cannot be captured on starting square |
| `EarthRookActive.cs` | `EarthRookActive` | Active (CD:5) | **Rampart** — Line of Stone Walls (2 HP, 3 sq) for 3 turns |
| `EarthKnightPassive.cs` | `EarthKnightPassive` | Passive | **Tremor Hop** — Stun one adjacent enemy after moving |
| `EarthKnightActive.cs` | `EarthKnightActive` | Active (CD:4) | **Earthquake** — Stun enemies within Manhattan distance 2 |
| `EarthBishopPassive.cs` | `EarthBishopPassive` | Passive | **Earthen Shield** — Captor is Stunned when this is taken |
| `EarthBishopActive.cs` | `EarthBishopActive` | Active (CD:5) | **Petrify** — Target enemy becomes Stone Wall for 2 turns |
| `EarthQueenPassive.cs` | `EarthQueenPassive` | Passive | **Tectonic Presence** — +1 HP to all friendly Stone Walls |
| `EarthQueenActive.cs` | `EarthQueenActive` | Active (CD:7) | **Continental Divide** — Line of Stone Walls (3 HP, 5 sq) |
| `EarthKingPassive.cs` | `EarthKingPassive` | Passive | **Bedrock Throne** — Cannot be checked on starting square |
| `EarthKingActive.cs` | `EarthKingActive` | Active (CD:8) | **Sanctuary** — Adjacent Stone Walls, king+allies immobilized |

#### Lightning (`Scripts/Wizard/Abilities/Lightning/`)
| File | Class | Type | Description |
|------|-------|------|-------------|
| `LightningPawnPassive.cs` | `LightningPawnPassive` | Passive | **Energized** — Always can move 2 forward (both empty) |
| `LightningPawnActive.cs` | `LightningPawnActive` | Active (CD:3) | **Chain Strike** — Forward 1, chain capture diagonals (max 3) |
| `LightningRookPassive.cs` | `LightningRookPassive` | Passive | **Overcharge** — Pass through one friendly piece |
| `LightningRookActive.cs` | `LightningRookActive` | Active (CD:5) | **Thunder Strike** — Teleport ignoring blockers, no capture |
| `LightningKnightPassive.cs` | `LightningKnightPassive` | Passive | **Double Jump** — Extra cardinal move after landing |
| `LightningKnightActive.cs` | `LightningKnightActive` | Active (CD:4) | **Lightning Rod** — Teleport within 5, stun shared adjacents |
| `LightningBishopPassive.cs` | `LightningBishopPassive` | Passive | **Voltage Burst** — Singe adjacent enemies after 3+ sq move |
| `LightningBishopActive.cs` | `LightningBishopActive` | Active (CD:5) | **Arc Flash** — Swap positions with any friendly piece |
| `LightningQueenPassive.cs` | `LightningQueenPassive` | Passive | **Swiftness** — Can move as Knight (L-shape), no capture |
| `LightningQueenActive.cs` | `LightningQueenActive` | Active (CD:7) | **Tempest** — Push enemies within 3 sq two squares away |
| `LightningKingPassive.cs` | `LightningKingPassive` | Passive | **Reactive Blink** — Once per game, move to safe sq within 2 when checked |
| `LightningKingActive.cs` | `LightningKingActive` | Active (CD:8) | **Static Field** — Lightning field on adjacent squares for 2 turns |

---

## Enums

### GameState
**File:** `Scripts/GameMaster.cs`

| Value | Meaning |
|-------|---------|
| `Playing` | Normal play |
| `WhiteInCheck` | White king is in check |
| `BlackInCheck` | Black king is in check |
| `WhiteWins` | Black is checkmated |
| `BlackWins` | White is checkmated |
| `Stalemate` | No legal moves, not in check |
| `Draw` | Draw by other rules |

### SquareEffectType
**File:** `Scripts/ChessConstants.cs`

| Value | Description |
|-------|-------------|
| `None` | No effect |
| `Fire` | Blocks movement, area denial |
| `StoneWall` | Blocks movement, has HP |
| `LightningField` | Doesn't block, stuns on entry |

### StatusEffectType
**File:** `Scripts/ChessConstants.cs`

| Value | Description |
|-------|-------------|
| `None` | No effect |
| `Stunned` | Cannot move for N turns |
| `Singed` | Auto-captured when attacked |

### MouseUI (private)
**File:** `Scripts/GameMaster.cs`

`CANMOVE`, `CANTMOVE`, `TAKEPIECE`, `START` — cursor icon states.

---

## Dependencies Between Classes

```
GameMaster ──────► BoardState              (owns, delegates state queries)
GameMaster ──────► PieceMove               (selects, commands moves)
GameMaster ──────► Square                  (board grid lookup)
GameMaster ──────► ChessMove               (move history)
GameMaster ──────► PieceUI                 (UI indicators)
GameMaster ──────► LineRenderer            (move preview line)
GameMaster ──────► SquareEffectManager     [NEW] (manages square effects)
GameMaster ──────► AbilityExecutor         [NEW] (ability targeting mode)

PieceMove ───────► GameMaster              (gm reference for state access)
PieceMove ───────► BoardState              (via gm.boardState for legality checks)
PieceMove ───────► Square                  (move targets, current position)
PieceMove ───────► DOTween                 (animation)
PieceMove ───────► ElementalPiece          [NEW] (optional elemental component)

ElementalPiece ──► IPassiveAbility         [NEW] (passive ability interface)
ElementalPiece ──► IActiveAbility          [NEW] (active ability interface)
ElementalPiece ──► CooldownTracker         [NEW] (cooldown state)
ElementalPiece ──► StatusEffect            [NEW] (status effect tracking)

SquareEffectManager ► SquareEffect         [NEW] (manages effect instances)
SquareEffectManager ► Square               [NEW] (queries square effects)
SquareEffectManager ► GameMaster           [NEW] (board access)

AbilityExecutor ──► GameMaster             [NEW] (game state access)
AbilityExecutor ──► SquareEffectManager    [NEW] (effect context)
AbilityExecutor ──► IActiveAbility         [NEW] (ability execution)

DraftManager ────► GameMaster              [NEW] (game control)
DraftManager ────► DraftData               [NEW] (selections)
DraftManager ────► DraftUI                 [NEW] (presentation)
DraftManager ────► AbilityFactory          [NEW] (ability creation)
DraftManager ────► AbilityBalanceConfig    [NEW] (reads stoneWallBonusHP)

AbilityFactory ──► All 36 Ability classes  [NEW] (instantiation)
AbilityFactory ──► AbilityBalanceConfig   [NEW] (reads balance params)

AbilityBalanceConfig ► Resources.Load     [NEW] (singleton accessor)

AbilityEditorWindow ► AbilityBalanceConfig [NEW] (SerializedObject editing)
AbilityEditorWindow ► AbilityInfo          [NEW] (ability names/descriptions)

PieceTooltipUI ──► GameMaster             [NEW] (selected piece, game state)
PieceTooltipUI ──► AbilityInfo            [NEW] (ability names/descriptions)
PieceTooltipUI ──► Canvas                 [NEW] (runtime UI creation)

ElementParticleUI ► PieceMove             [NEW] (element info via elementalPiece)
ElementParticleUI ► ParticleSystem        [NEW] (runtime particle creation)

AbilityInfo ─────► ChessConstants         [NEW] (element/piece type constants)

AbilityModeUI ───► GameMaster             [NEW] (ability mode state)
AbilityModeUI ───► AbilityInfo            [NEW] (ability names)
AbilityModeUI ───► Canvas                 [NEW] (runtime UI creation)

CheckBannerUI ──► GameMaster             [NEW] (game state: check detection)
CheckBannerUI ──► Canvas                 [NEW] (runtime UI creation)

GameOverUI ─────► GameMaster             [NEW] (game state: game over detection)
GameOverUI ─────► Canvas                 [NEW] (runtime UI creation)
GameOverUI ─────► SceneManager           [NEW] (scene reload for New Game)

FireVsEarthSetup ► ElementParticleUI      [NEW] (attaches to pieces)
FireVsEarthSetup ► AbilityBalanceConfig   [NEW] (reads stoneWallBonusHP)

DeckBasedSetup ──► GameMaster             [NEW] (board access)
DeckBasedSetup ──► DraftData              [NEW] (element selections per piece)
DeckBasedSetup ──► AbilityFactory         [NEW] (ability creation)
DeckBasedSetup ──► AbilityBalanceConfig   [NEW] (reads stoneWallBonusHP)

MatchConfig ─────► DraftData              [NEW] (cross-scene data bridge)
MainMenuUI ──────► DeckSelectPanel        [NEW] (panel management)
MainMenuUI ──────► DeckEditorPanel        [NEW] (panel management)
MainMenuUI ──────► PieceExaminePanel      [NEW] (panel management)
MainMenuUI ──────► DeckPersistence        [NEW] (load/save decks)
DeckSelectPanel ─► MatchConfig            [NEW] (sets cross-scene data)
DeckSelectPanel ─► SceneManager           [NEW] (loads Board scene)
DeckEditorPanel ─► DeckPersistence        [NEW] (saves deck data)
DeckEditorPanel ─► PieceIndexHelper       [NEW] (piece labels)
PieceExaminePanel ► AbilityInfo           [NEW] (ability lookups)
PieceExaminePanel ► AbilityFactory        [NEW] (cooldown lookups)
GameOverUI ──────► MatchConfig            [NEW] (clears on Main Menu)

Square ──────────► PieceMove               (occupying piece reference)
Square ──────────► SquareEffect            [NEW] (active effect)
Square ──────────► AudioSource             (hit sound)

ChessMove ───────► PieceMove               (records which piece moved)

CameraMove ──────► DOTween                 (camera transitions)

BoardState ──────► PieceMove               (stores piece references in grid)
BoardState ──────► ChessConstants          (directions, constants)
```

---

## Prefabs & Tags

### Prefabs
| Prefab | Description |
|--------|-------------|
| `BlackSquare` | Dark board square |
| `King` | King piece model |
| `Rook` | Rook piece model |
| `PieceUI` | UI feedback overlay |

### Tags
| Tag | Used By |
|-----|---------|
| `"Board"` | Square GameObjects |
| `"Piece"` | Chess piece GameObjects |
| `"GM"` | GameMaster GameObject |

---

## File Map

```
Scripts/
├── GameMaster.cs              Core orchestrator (modified — conditional DeckBasedSetup)
├── BoardState.cs              Board state manager (modified)
├── PieceMove.cs               Per-piece logic (modified)
├── Square.cs                  Board square (modified)
├── ChessMove.cs               Move recording (modified)
├── ChessConstants.cs          Constants + enums (modified)
├── CameraMove.cs              Camera control
├── PieceUI.cs                 UI overlay
├── BoardUI.cs                 World-to-2D utility
├── OutofBounds.cs             Board boundary detection
├── PieceCheck.cs              Piece collision handling
├── Menu/
│   ├── DeckSlot.cs               Single deck definition
│   ├── DeckSaveData.cs           All 9 deck slots container
│   ├── DeckPersistence.cs        JSON save/load utility
│   ├── MatchConfig.cs            Cross-scene static data bridge
│   ├── PieceIndexHelper.cs       Piece index → type/label/icon mapping
│   ├── MainMenuUI.cs             Root menu controller
│   ├── DeckSelectPanel.cs        Pre-game deck picking
│   ├── DeckEditorPanel.cs        Deck editor (element assignment)
│   └── PieceExaminePanel.cs      Ability reference + effects glossary browser
├── Editor/
│   └── AbilityEditorWindow.cs    Ability balance editor window
└── Wizard/
    ├── Abilities/
    │   ├── IPassiveAbility.cs          Interface
    │   ├── IActiveAbility.cs           Interface
    │   ├── Fire/
    │   │   ├── FirePawnPassive.cs       FirePawnActive.cs
    │   │   ├── FireRookPassive.cs       FireRookActive.cs
    │   │   ├── FireKnightPassive.cs     FireKnightActive.cs
    │   │   ├── FireBishopPassive.cs     FireBishopActive.cs
    │   │   ├── FireQueenPassive.cs      FireQueenActive.cs
    │   │   └── FireKingPassive.cs       FireKingActive.cs
    │   ├── Earth/
    │   │   ├── EarthPawnPassive.cs      EarthPawnActive.cs
    │   │   ├── EarthRookPassive.cs      EarthRookActive.cs
    │   │   ├── EarthKnightPassive.cs    EarthKnightActive.cs
    │   │   ├── EarthBishopPassive.cs    EarthBishopActive.cs
    │   │   ├── EarthQueenPassive.cs     EarthQueenActive.cs
    │   │   └── EarthKingPassive.cs      EarthKingActive.cs
    │   └── Lightning/
    │       ├── LightningPawnPassive.cs  LightningPawnActive.cs
    │       ├── LightningRookPassive.cs  LightningRookActive.cs
    │       ├── LightningKnightPassive.cs LightningKnightActive.cs
    │       ├── LightningBishopPassive.cs LightningBishopActive.cs
    │       ├── LightningQueenPassive.cs LightningQueenActive.cs
    │       └── LightningKingPassive.cs  LightningKingActive.cs
    ├── Data/
    │   ├── ElementDefinition.cs        ScriptableObject
    │   ├── AbilityDefinition.cs        ScriptableObject
    │   └── AbilityBalanceConfig.cs     Balance config SO + 36 param classes
    ├── Draft/
    │   ├── DraftManager.cs             Draft orchestrator
    │   ├── DraftUI.cs                  Draft screen UI
    │   └── DraftData.cs                Draft selections
    ├── Runtime/
    │   ├── ElementalPiece.cs           Per-piece element component
    │   ├── CooldownTracker.cs          Cooldown state
    │   ├── StatusEffect.cs             Status effect data
    │   ├── SquareEffect.cs             Square effect component
    │   ├── SquareEffectManager.cs      Effect manager singleton
    │   ├── AbilityExecutor.cs          Ability targeting mode
    │   ├── AbilityFactory.cs           Ability instance factory
    │   ├── FireVsEarthSetup.cs        Auto-assigns Fire/Earth to teams
    │   └── DeckBasedSetup.cs          Per-piece element setup from decks
    └── UI/
        ├── AbilityUI.cs                Ability button + cooldown
        ├── AbilityInfo.cs              Static ability/effect name/description lookups
        ├── AbilityModeUI.cs            Ability mode banner indicator
        ├── CheckBannerUI.cs            Check state red banner
        ├── GameOverUI.cs               Game over overlay + scene reset
        ├── PieceTooltipUI.cs           Mouse-over piece info tooltip
        ├── ElementParticleUI.cs        Element-colored particle effects on pieces
        ├── SquareEffectUI.cs           Square effect visuals
        └── ElementIndicatorUI.cs       Piece element tinting

Resources/
└── ChessIcons/                       Chess piece icon sprites for DeckEditorPanel
    ├── pawn.png
    ├── rook.png
    ├── knight.png
    ├── bishop.png
    ├── queen.png
    └── king.png
```
