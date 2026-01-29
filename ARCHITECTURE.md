# ARCHITECTURE.md — WizardChess2 Class Reference

> **Last updated:** 2026-01-29
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
8. [Test System](#test-system)
9. [File Map](#file-map)

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
│           draft phase, game end, tooltip UI, AI opponent     │
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

  ┌─────────────────────────────┐
  │       AI Opponent           │
  │  ChessAI (MonoBehaviour)    │
  │  AIEvaluation (static)      │
  │  AIMatchPanel (Menu UI)     │
  └─────────────────────────────┘

  ┌─────────────────────────────────────┐
  │       Online Multiplayer            │
  │  PhotonConnectionManager (singleton)│
  │  NetworkGameController (RPC sync)   │
  │  OnlineMatchPanel (Menu UI)         │
  └─────────────────────────────────────┘
```

---

## Scene Flow

```
MainMenu Scene                           Board Scene
┌──────────────────┐                    ┌──────────────────┐
│ Title Screen     │                    │ GameMaster.Start()│
│   Play →         │                    │   MatchConfig set?│
│   Play vs AI → ──┤                    │   YES → DeckBased │
│   Play Online → ─┤                    │          Setup    │
│   Manage Decks → │                    │   NO  → FireVs   │
│   Examine Pieces→│                    │          EarthSetup│
│   Quit           │                    │                  │
│                  │  LoadScene("Board")│   isOnlineMatch? │
│ Deck Select:     │───────────────────>│   YES → AddComp  │
│   P1 picks deck  │                    │   <NetworkGame   │
│   P2 picks deck  │<───────────────────│    Controller>   │
│   Start Match    │  LoadScene("Menu") │                  │
│                  │                    │   isAIMatch?     │
│ AI Match Panel:  │  LoadScene("Board")│   YES → AddComp  │
│   Pick difficulty│───────────────────>│        <ChessAI> │
│   Pick deck      │                    │   NO  → 2-player │
│   Start Match    │                    │                  │
│                  │                    │ Game Over UI:    │
│ Online Match:    │  PhotonNetwork     │   Rematch / Menu │
│   Select deck    │  .LoadLevel("Board")│  Online: Menu   │
│   Find/Create/   │───────────────────>│  only (no rematch│
│   Join Room      │                    │  same opponent)  │
│   Wait for opp.  │                    │                  │
└──────────────────┘                    └──────────────────┘
```

- **MainMenu scene** (`Assets/Scenes/MainMenu.unity`): Title screen with 6 panels (Title, DeckSelect, DeckEditor, PieceExamine, AIMatch, OnlineMatch) plus a Settings overlay. Managed by `MainMenuUI`.
- **Board scene** (`Assets/Scenes/Board.unity`): Chess gameplay. `GameMaster.Start()` checks `MatchConfig` to decide setup mode.
- **Cross-scene data**: `MatchConfig` static class holds `DraftData`, `useDeckSystem`, AI settings (`isAIMatch`, `aiDifficulty`, `aiColor`), and online settings (`isOnlineMatch`, `localPlayerColor`, `roomCode`) between scene loads.
- **Photon persistence**: `PhotonConnectionManager` singleton survives scene loads via `DontDestroyOnLoad`. Connection is maintained from menu through gameplay.

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
     5. Capture? TryCapture() → **King safety guard** (kings cannot be captured)
                              → [NEW] passive.OnBeforeCapture() (can prevent)
                              → takePiece() → **King safety net** → pieceTaken() → BoardState.RemovePiece()
                              → [NEW] passive.OnAfterCapture()
                              → [NEW] passive.OnPieceCaptured() (defender)
     6. [NEW] Check for multi-step move (e.g., Lightning Knight double-jump)
        6a. If double-jump: ExecuteDoubleJump() → MultiStepMoveController.ExecuteSteps()
            → Step 1: Animate to intermediate L-jump square
            → Step 2: Animate to final destination
            → Callback: EndTurn()
        6b. Else: PieceMove.movePiece() → DOTween animation
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
  4. NotifyTurnStart() — tick cooldowns (own color), status effects (opponent color)
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
                             → Creates SquareEffectManager, AbilityExecutor, MultiStepMoveController
                             → Creates GameLogUI (scrollable move/event log)
2. Pieces fall onto squares  → Square.OnTriggerEnter()
3. PieceMove.setIntitialPiece() → Registers with BoardState, generates initial moves
```

---

## Class Reference — Core Chess

### GameMaster
**File:** `Scripts/GameMaster.cs`
**Inherits:** `MonoBehaviour`
**Tag:** `"GM"`
**Role:** Central orchestrator — handles input, game loop, turn management, UI, draft phase, ability mode, game state evaluation, and AI opponent integration.

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
| `multiStepController` | `MultiStepMoveController` | **[NEW]** Orchestrates multi-step move animations |
| `turnNumber` | `int` | **[NEW]** Current turn counter |
| `isDraftPhase` | `bool` | **[NEW]** Blocks gameplay during draft |
| `networkController` | `NetworkGameController` | **[NEW]** Online multiplayer controller (null if offline) |
| `inGameMenuUI` | `InGameMenuUI` | **[NEW]** In-game pause menu (resign, draw, exit) |
| `isSetupComplete` | `bool` | **[NEW]** Set by DeckBasedSetup/FireVsEarthSetup when elements applied; gates input and RPC processing |

#### Public Methods
| Method | Signature | Description |
|--------|-----------|-------------|
| `RegisterPiece` | `(PieceMove piece, int x, int y)` | Register piece with BoardState at init |
| `UpdateBoardState` | `(PieceMove piece, int fromX, int fromY, int toX, int toY)` | Update BoardState after a move |
| `RemovePieceFromBoardState` | `(int x, int y)` | Remove captured piece from BoardState |
| `selectPiece` | `(Transform t, PieceMove piece)` | Select a piece and generate its moves |
| `takePiece` | `(PieceMove p)` | Execute a capture (includes king safety net - refuses to take kings) |
| `EndTurn` | `()` | **[NEW]** Swap turn, tick effects/cooldowns, evaluate state |
| `EnterAbilityMode` | `(PieceMove piece)` | **[NEW]** Enter ability targeting mode |
| `TryCapture` | `(PieceMove attacker, PieceMove defender) → bool` | **[NEW]** Capture with passive hooks. **King safety guard** at start blocks king captures (returns false). |
| `ExecuteDoubleJump` | `(PieceMove, KnightMoveData, Square, int, int)` | **[NEW]** Execute Lightning Knight double-jump via MultiStepMoveController |

#### Private/Public Utility Methods
| Method | Description |
|--------|-------------|
| `deSelectPiece()` | **[CHANGED: now public]** Clear selection state and hide UI |
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
| `IsKingInCheck` | `(int kingColor) → bool` | Is king in check? |
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

#### Static Fields
| Field | Type | Description |
|-------|------|-------------|
| `DefaultArcHeight` | `float` | **[NEW]** Height of parabolic arc for piece movement (default 0.5 units) |
| `DebugMoveValidation` | `bool` | **[NEW]** Enable console logging of move rejections |

#### Move Generation Pipeline (createPieceMoves)
```
1. MoveRejectionTracker.Clear() — start fresh tracking
2. Generate pseudo-legal moves (King/Queen/Bishop/Knight/Rook/Pawn)
   - Track BlockedByFriendlyPiece, BlockedByPiecePath rejections
3. [NEW] passive.ModifyMoveGeneration() — add/remove moves based on element
   - Track ElementalPassiveBlocked rejections
4. [NEW] Filter squares blocked by SquareEffectManager
   - Track SquareEffectBlocked rejections
5. [NEW] FilterProtectedCaptures() — remove captures blocked by passive abilities
   - Track CaptureProtected, AttackerCannotCapture rejections
6. filterIllegalMoves() — remove moves that leave king in check
   - Track WouldLeaveKingInCheck rejections
7. If DebugMoveValidation: log all rejections to console
```

#### Animation Methods (Parabolic Arc & Multi-Step)
| Method | Signature | Description |
|--------|-----------|-------------|
| `AnimateWithArc` | `(Vector3 dest, float duration, float arcHeight) → Tween` | **[NEW]** Animate with parabolic arc using DOPath CatmullRom |
| `AnimateToSquare` | `(Square dest, float duration) → Tween` | **[NEW]** Animate to square with arc (calls AnimateWithArc) |
| `AnimateToSquareCoroutine` | `(Square dest, float duration) → IEnumerator` | **[NEW]** Coroutine wrapper for animation |
| `UpdateBoardStateOnly` | `(int toX, int toY, Square sq)` | **[NEW]** Update board state without animation |
| `MovePieceAnimated` | `(int toX, int toY, Square sq, float dur, Action) → IEnumerator` | **[NEW]** Full animated move with state update |

**Arc Animation:** All piece movements (including castling rook) use `AnimateWithArc()` to create a parabolic path that clears other pieces, preventing 3D model collision during moves.

#### Promotion Methods
| Method | Description |
|--------|-------------|
| `PromoteTo(int)` | Promote pawn: update piece type, swap mesh+material, recalculate attacks, re-init elemental abilities |
| `SwapMesh(int)` | Load promotion prefab from `Resources/PromotionPrefabs/` and swap mesh, collider, material, and re-apply element tint |
| `GetPromotionPrefabPath(int, int)` | Static map: (piece type, color) → Resources path (`PromotionPrefabs/QueenDark`, `PromotionPrefabs/QueenLight`, etc.) |

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
**Role:** Keyboard-driven camera control with DOTween transitions. Keys: 1 (White), 2 (Black), 3 (Top-down). Camera switching is disabled in online mode (`MatchConfig.isOnlineMatch`).

---

### PieceUI
**File:** `Scripts/PieceUI.cs`
**Role:** Canvas overlay that follows a 3D piece and displays its icon as a UI Image. Auto-detects piece type changes (e.g. pawn promotion) and swaps the icon sprite.

| Field / Method | Description |
|----------------|-------------|
| `target` | Transform of the 3D piece this UI follows |
| `isPieceUI` | Whether this instance tracks a piece (vs selection indicator) |
| `color` | `'W'` or `'B'` — the piece color |
| `spriteLookup` | Static dictionary mapping `"{color}_{pieceType}"` → `Sprite`, built at startup from all PieceUI instances |
| `Start()` | Caches Image, PieceMove; registers sprite in shared lookup |
| `Update()` | Follows target position; detects piece type changes and swaps Image sprite |

**Promotion icon swap:** At startup, each PieceUI registers its Image sprite keyed by `(color, pieceType)`. When `PieceMove.piece` changes (promotion), PieceUI detects the mismatch and looks up the new sprite from the dictionary.

### BoardUI, OutofBounds, PieceCheck
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

| Method | Signature | Description |
|--------|-----------|-------------|
| `GetTextOverride` | `(int elementId, int pieceType) → AbilityTextOverride` | Get text override for element+piece, or null |

**Structure:** Each element container (e.g., `FireAbilityParams`) holds 12 `[System.Serializable]` param classes (passive + active for each of 6 piece types), plus 6 `AbilityTextOverride` fields (one per piece type) for editable ability names/descriptions. Each param class has `[Tooltip]` annotations and default values matching the original hardcoded values.

**Supporting classes** (all in same file):
- `CooldownConfig` — cooldowns per piece type with `Get(int pieceType)` accessor
- `FireAbilityParams`, `EarthAbilityParams`, `LightningAbilityParams` — element containers with `GetTextOverride(int pieceType)` accessor
- `AbilityTextOverride` — serializable text overrides (passiveName, passiveDescription, activeName, activeDescription)
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
| `hasUsedStoneShield` | `bool` | Earth King once-per-game flag |

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
| `OnTurnStart(int)` | Tick cooldowns (own turn) and status effects (opponent turn) |

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
| `GetBlockingEffectName(x, y) → string` | **[NEW]** Human-readable name of blocking effect |
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

#### MoveStep
**File:** `Scripts/Wizard/Runtime/MoveStep.cs`
**Inherits:** Plain C# class
**Role:** Data structure representing one step in a multi-step move sequence. Used by `MultiStepMoveController` to orchestrate sequential animations for abilities like Lightning Knight double-jump, Chain Strike, Tempest, etc.

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `MoveStepType` | Type of step (MoveTo, Capture, Custom) |
| `Piece` | `PieceMove` | Piece being moved |
| `Destination` | `Square` | Target square for MoveTo steps |
| `CaptureTarget` | `PieceMove` | Victim for Capture steps |
| `CustomAction` | `Action` | Arbitrary action for Custom steps |
| `IntermediateSquare` | `Square` | Optional: waypoint for multi-leg moves |
| `RecordInHistory` | `bool` | Whether to record this step in move history |
| `Duration` | `float` | Animation duration override |

| Static Factory | Description |
|----------------|-------------|
| `MoveTo(piece, dest, recordHistory)` | Create a move step |
| `Capture(attacker, victim)` | Create a capture step |
| `Custom(action, waitDuration)` | Create a custom action step |

#### MultiStepMoveController
**File:** `Scripts/Wizard/Runtime/MultiStepMoveController.cs`
**Inherits:** `MonoBehaviour` (attached to GameMaster object)
**Role:** Orchestrates sequential animations for multi-step moves and abilities. Provides visible delays between steps (especially for AI) so players can follow the action.

| Property | Type | Description |
|----------|------|-------------|
| `IsExecutingMultiStep` | `bool` | True while executing, blocks input |
| `aiStepDelay` | `float` | Delay between AI steps (0.5s) |
| `playerStepDelay` | `float` | Delay between player steps (0.15s) |
| `defaultAnimationDuration` | `float` | Default animation duration (0.4s) |
| `CurrentPiece` | `PieceMove` | Piece currently being moved |

| Method | Description |
|--------|-------------|
| `Init(GameMaster)` | Initialize with GameMaster reference |
| `ExecuteSteps(List<MoveStep>, bool isAI, Action)` | Execute step sequence with optional completion callback |
| `CreateDoubleMove(piece, intermediate, final)` | Helper for knight double-jump |
| `CreateSingleMove(piece, dest)` | Helper for single animated move |

**Usage Pattern:**
1. Ability `Execute()` builds `List<MoveStep>` describing the sequence
2. Calls `multiStepController.ExecuteSteps(steps, onComplete)`
3. Controller executes steps sequentially with animations and delays
4. `onComplete` callback triggers when all steps finish (e.g., `EndTurn()`)

#### KnightMoveData
**File:** `Scripts/Wizard/Abilities/Lightning/LightningKnightPassive.cs`
**Inherits:** Plain C# class
**Role:** Metadata for knight moves distinguishing single L-jumps from double-jumps (Lightning Knight passive).

| Property | Type | Description |
|----------|------|-------------|
| `FinalDestination` | `Square` | Final landing square |
| `IntermediateSquare` | `Square` | L-jump waypoint (null for standard moves) |
| `IsDoubleJump` | `bool` | True if this is an extended double-jump |

**Static Lookup:** `LightningKnightPassive.GetMoveData(piece, x, y)` returns `KnightMoveData` for the destination, or null if not a Lightning Knight move.

#### MoveRejectionTracker
**File:** `Scripts/Wizard/Runtime/MoveRejectionTracker.cs`
**Inherits:** Static class + supporting types
**Role:** Tracks why moves are rejected during move validation. Used for debugging (console logs) and UI feedback (tooltips explaining why a square is invalid).

| Enum Value | Description |
|------------|-------------|
| `None` | Move is valid |
| `OutOfBounds` | Square outside 8x8 board |
| `BlockedByFriendlyPiece` | Friendly piece on target |
| `BlockedByPiecePath` | Piece blocks sliding path |
| `ElementalPassiveBlocked` | Elemental passive removed this move |
| `SquareEffectBlocked` | Fire/Wall/etc. blocks entry |
| `CaptureProtected` | Defender's passive prevents capture |
| `AttackerCannotCapture` | Attacker's passive prevents capture |
| `WouldLeaveKingInCheck` | Move leaves own king in check |
| `CastlingKingMoved` | King has moved, can't castle |
| `CastlingRookMoved` | Rook has moved or missing |
| `CastlingPathBlocked` | Pieces between king and rook |
| `CastlingThroughCheck` | King passes through attacked square |
| `CastlingInCheck` | King currently in check |
| `EnPassantNotAvailable` | No en passant target |
| `PawnCannotCaptureForward` | Pawn can only capture diagonally |

| Static Method | Description |
|---------------|-------------|
| `Clear()` | Clear all rejections (call at start of move generation) |
| `AddRejection(x, y, reason, details)` | Track a rejected square |
| `GetRejection(x, y) → MoveRejection` | Get rejection for a square |
| `GetExplanation(x, y) → string` | Human-readable explanation |
| `GetFriendlyText(reason) → string` | Convert reason to user text |
| `HasRejections() → bool` | Whether any rejections tracked |
| `RejectionCount → int` | Number of rejected squares |
| `CurrentRejections → Dictionary` | Read-only copy of all rejections |

**Usage:** `PieceMove.createPieceMoves()` calls `Clear()` at start, then `AddRejection()` at each validation stage. `MoveExplanationUI` queries `GetExplanation()` when hovering invalid squares.

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
| `isAIMatch` | `bool` | **[NEW]** Whether this match is against the AI |
| `aiDifficulty` | `int` | **[NEW]** AI difficulty: 0=Easy, 1=Medium, 2=Hard |
| `aiColor` | `int` | **[NEW]** AI player color (default: BLACK) |
| `isOnlineMatch` | `bool` | **[NEW]** Whether this is an online multiplayer match |
| `localPlayerColor` | `int` | **[NEW]** Local player's color in online mode (WHITE or BLACK) |
| `roomCode` | `string` | **[NEW]** Room code for private online matches (null for random) |

| Method | Description |
|--------|-------------|
| `Clear()` | Reset to defaults (including AI and online fields) |

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
**Role:** Root controller for MainMenu scene. Creates Canvas + EventSystem at runtime. Manages 6 panels: Title, DeckSelect, DeckEditor, PieceExamine, AIMatch, OnlineMatch, plus a SettingsUI overlay. Title panel has 7 buttons (Play Match, Play vs AI, Play Online, Manage Decks, Examine Pieces, Settings, Quit). Provides show/hide navigation and static UI helper methods.

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
**Role:** In-game ability button display. Shows ability name, cooldown, piece icon (tinted by element), and triggers ability mode. Pre-loads all 6 piece icon sprites from `Resources/ChessIcons/` at startup via `PieceIndexHelper.GetIconResourcePath()`. Updates the icon sprite in `ShowAbilityForPiece()` based on `piece.piece`, so the icon automatically reflects promotion. Creates the icon Image at runtime if not assigned via Inspector.

#### MoveExplanationUI
**File:** `Scripts/Wizard/UI/MoveExplanationUI.cs`
**Inherits:** `MonoBehaviour` (attached to GameMaster object)
**Role:** Displays tooltips explaining why moves are invalid. When a piece is selected and the player hovers over an invalid square, shows a tooltip with the rejection reason after a short delay (0.3s).

| Field/Property | Type | Description |
|----------------|------|-------------|
| `showDelay` | `float` | Hover delay before showing tooltip (default 0.3s) |
| `tooltipOffset` | `Vector2` | Offset from mouse cursor |

| Method | Description |
|--------|-------------|
| `ShowExplanation(x, y)` | Force-show explanation for a square |
| `HideTooltip()` | Hide the tooltip |

**UI Creation:** Creates its own Canvas (ScreenSpaceOverlay) with Panel + TextMeshPro at runtime via `CreateTooltipUI()`. No prefab required.

**Data Source:** Queries `MoveRejectionTracker.GetExplanation(x, y)` to get human-readable rejection reasons populated during `PieceMove.createPieceMoves()`.

#### SquareEffectUI
**File:** `Scripts/Wizard/UI/SquareEffectUI.cs`
**Inherits:** `MonoBehaviour`
**Role:** Tints square material based on active effect type (fire=red, stone=brown, lightning=blue).

#### ElementIndicatorUI
**File:** `Scripts/Wizard/UI/ElementIndicatorUI.cs`
**Inherits:** `MonoBehaviour`
**Role:** Tints piece material based on assigned element. Provides `ReapplyTint()` for re-tinting after material swap (e.g. pawn promotion).

#### AbilityInfo
**File:** `Scripts/Wizard/UI/AbilityInfo.cs`
**Inherits:** Static class
**Role:** Lookup tables for ability names and descriptions. Maps (elementId, pieceType) to passive name, passive description, active name, active description, and element display name. All four text getter methods check `AbilityBalanceConfig.Instance` text overrides first, falling back to hardcoded defaults if no override is set. Also provides square effect names/descriptions/colors (`GetSquareEffectName`, `GetSquareEffectDescription`, `GetSquareEffectColor`) and status effect names/descriptions/colors (`GetStatusEffectName`, `GetStatusEffectDescription`, `GetStatusEffectColor`). Used by `PieceTooltipUI`, `PieceExaminePanel`, and other UI components.

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
**Role:** Mouse-over tooltip showing piece icon, piece info, element, passive/active abilities, cooldown status, status effects with descriptions, and current square effect info. Attached to GameMaster object. Creates its own UI panel at runtime as a child of the scene Canvas. Pre-loads all 6 piece icon sprites from `Resources/ChessIcons/` at startup. Uses raycasting to detect hovered pieces. Caches by both piece reference and piece type so the tooltip rebuilds after promotion. Status effects (Stunned, Singed) and square effects (Fire, Stone Wall, Lightning Field) show full descriptions from `AbilityInfo`.

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

---

### AI Opponent System

#### ChessAI
**File:** `Scripts/AI/ChessAI.cs`
**Inherits:** `MonoBehaviour`
**Role:** Main AI controller. Detects when it's the AI's turn, collects candidate moves and abilities, scores them by difficulty level, and executes the best one. Added to GameMaster object at runtime when `MatchConfig.isAIMatch` is true.

| Field | Type | Description |
|-------|------|-------------|
| `difficulty` | `int` | 0=Easy, 1=Medium, 2=Hard |
| `aiColor` | `int` | AI player color (default: BLACK) |
| `isThinking` | `bool` | Prevents multiple coroutines |
| `thinkDelay` | `float` | Seconds before executing (Easy:1.0, Medium:0.8, Hard:0.6) |

| Method | Signature | Description |
|--------|-----------|-------------|
| `Init` | `(GameMaster, int difficulty, int color)` | Initialize AI with references and settings |
| `IsAITurn` | `() → bool` | Returns true if it's the AI's turn (used by GameMaster to block input) |

**Difficulty Levels:**
- **Easy (0):** Capture-only scoring + heavy randomness (0-200). Safety filter penalizes hanging pieces. Picks random from top 5 candidates. Does not use abilities.
- **Medium (1):** Full positional evaluation (material + PST + center + check + development) + slight randomness (0-20). Best scoring candidate. Uses abilities with -30 penalty.
- **Hard (2):** Full evaluation + hanging piece analysis (recapture awareness) + minimal randomness (0-5). Best scoring candidate. Uses abilities aggressively.

**Turn Flow:**
1. `Update()` detects `gm.currentMove == aiColor`
2. `ThinkAndMove()` coroutine: wait delay → collect moves → collect abilities (Medium+) → pick best → execute
3. `ExecuteMove()`: regenerate moves, try capture, move piece, end turn
4. `ExecuteAbility()`: enter ability mode, execute on target, end turn (fallback to normal move on failure)

#### AIEvaluation
**File:** `Scripts/AI/AIEvaluation.cs`
**Inherits:** Static class
**Role:** Static utility for board evaluation. Provides piece values, piece-square tables, move scoring, and ability scoring. All methods are stateless.

| Method | Signature | Description |
|--------|-----------|-------------|
| `GetPieceValue` | `(int pieceType) → int` | Material value in centipawns (P=100, N=320, B=330, R=500, Q=900, K=10000) |
| `GetPositionalBonus` | `(int pieceType, int x, int y, int color) → float` | Piece-square table lookup; flips y for White |
| `ScoreMove` | `(PieceMove, Square, BoardState, SquareEffectManager, int turnNumber) → float` | Full move scoring: capture + MVV-LVA + singed + positional + center + check + development |
| `ScoreMoveWithHangingAnalysis` | `(PieceMove, Square, BoardState, SquareEffectManager, int turnNumber) → float` | Extends ScoreMove with trade analysis and hanging piece penalties |
| `ScoreAbilityUse` | `(PieceMove, Square, BoardState, SquareEffectManager) → float` | Heuristic ability scoring: direct target value + AoE proximity bonus |

#### AIMatchPanel
**File:** `Scripts/Menu/AIMatchPanel.cs`
**Inherits:** `MonoBehaviour`
**Role:** Menu panel for AI match setup. Player selects difficulty (Easy/Medium/Hard) and their deck from 9 slots. AI gets a random element deck. Sets `MatchConfig` and loads Board scene.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Init` | `(MainMenuUI)` | Store menu reference |
| `Open` | `(DeckSaveData)` | Reset state, build UI |

**Data Flow:**
```
MainMenuUI → AIMatchPanel.Open() → Player selects difficulty + deck
→ OnStartMatch() → MatchConfig (isAIMatch=true, aiDifficulty, aiColor, useDeckSystem=true, draftData)
→ SceneManager.LoadScene("Board")
→ GameMaster.Start() → AddComponent<ChessAI>().Init()
→ ChessAI.Update() detects AI turn → ThinkAndMove()
```

---

### Online Multiplayer System

Architecture: Single PhotonView on GameMaster, master-client authority, RPC-based move sync (4 integers per move). Uses Photon PUN 2.

#### PhotonConnectionManager
**File:** `Scripts/Network/PhotonConnectionManager.cs`
**Inherits:** `MonoBehaviourPunCallbacks`
**Role:** DontDestroyOnLoad singleton managing Photon connection lifecycle. Handles connect, disconnect, room creation/joining, and random matchmaking. Survives scene loads.

| Property/Field | Type | Description |
|----------------|------|-------------|
| `Instance` | `static PhotonConnectionManager` | Singleton accessor |
| `State` | `ConnectionState` | Disconnected/Connecting/ConnectedToMaster/JoiningRoom/InRoom/Error |
| `LastError` | `string` | Last error message |

| Method | Signature | Description |
|--------|-----------|-------------|
| `ConnectToPhoton` | `()` | Connect using PhotonServerSettings |
| `CreateRoom` | `(string code)` | Create private room (IsVisible=false), MaxPlayers=2 |
| `JoinRoom` | `(string code)` | Join room by name |
| `JoinRandomRoom` | `()` | Random matchmaking; auto-creates on fail |
| `Disconnect` | `()` | Clean disconnect |

| Event | Signature | Description |
|-------|-----------|-------------|
| `OnConnectedToMasterEvent` | `Action` | Connected to master server |
| `OnJoinedRoomEvent` | `Action<string>` | Joined a room (room name) |
| `OnOpponentJoinedEvent` | `Action` | Second player entered room |
| `OnConnectionErrorEvent` | `Action<string>` | Connection error |
| `OnOpponentLeftEvent` | `Action` | Opponent left room |

#### NetworkGameController
**File:** `Scripts/Network/NetworkGameController.cs`
**Inherits:** `MonoBehaviourPunCallbacks` (requires PhotonView)
**Role:** In-game move synchronization via RPCs. Handles color assignment (master=White), deck reconstruction from Photon player properties, camera setup, and disconnect handling. Attached to GameMaster at runtime. Uses fixed ViewID (901) for deterministic RPC routing on both clients. RPC coroutines wait for `gm.isSetupComplete` before processing to prevent race conditions with element setup.

| Property | Type | Description |
|----------|------|-------------|
| `LocalColor` | `int` | Local player's assigned color |

| Method | Signature | Description |
|--------|-----------|-------------|
| `Init` | `(GameMaster)` | Assign color, set camera, rebuild draft data |
| `IsRemotePlayerTurn` | `() → bool` | True when it's the remote player's turn |
| `SendMove` | `(int fromX, int fromY, int toX, int toY)` | RPC move to opponent |
| `SendAbility` | `(int pieceX, int pieceY, int targetX, int targetY)` | RPC ability to opponent |
| `SendResign` | `()` | **[NEW]** RPC resign notification to opponent |
| `SendDrawOffer` | `()` | **[NEW]** RPC draw offer to opponent |
| `SendDrawResponse` | `(bool accepted)` | **[NEW]** RPC draw accept/decline to opponent |

**RPCs:**
- `RPC_ReceiveMove(fromX, fromY, toX, toY)` → waits for `isSetupComplete`, then executes remote move via coroutine (triggers passive hooks)
- `RPC_ReceiveAbility(pieceX, pieceY, targetX, targetY)` → waits for `isSetupComplete`, then executes ability directly (bypasses validation, trusts sender)
- `RPC_ReceiveResign()` → **[NEW]** calls `InGameMenuUI.OnOpponentResigned()`
- `RPC_ReceiveDrawOffer()` → **[NEW]** calls `InGameMenuUI.ShowDrawOffer()`
- `RPC_ReceiveDrawResponse(bool)` → **[NEW]** calls `InGameMenuUI.OnDrawResponseReceived()`

**Data Flow:**
```
OnlineMatchPanel → PhotonConnectionManager.JoinRandomRoom/CreateRoom/JoinRoom
→ Both players join room → deck stored as Photon custom property
→ Master calls PhotonNetwork.LoadLevel("Board")
→ GameMaster.Start() → AddComponent<NetworkGameController>().Init()
   → Assigns colors (master=White)
   → Rebuilds DraftData from both players' custom properties
   → Sets camera to local player's perspective
→ DeckBasedSetup/FireVsEarthSetup sets gm.isSetupComplete = true
→ Local move → GameMaster.Update() (blocked until isSetupComplete) → networkController.SendMove() → RPC to opponent
→ Remote move → RPC_ReceiveMove() → WaitUntil(isSetupComplete) → ExecuteRemoteMove (passive hooks fire)
→ Remote ability → RPC_ReceiveAbility() → WaitUntil(isSetupComplete) → Execute directly (skip validation)
→ Disconnect → OnPlayerLeftRoom() → opponent wins
```

#### OnlineMatchPanel
**File:** `Scripts/Menu/OnlineMatchPanel.cs`
**Inherits:** `MonoBehaviour`
**Role:** Menu panel for online matchmaking. Deck selection from 9 slots, then Find Match (random), Create Room (private code), or Join Room (by code). Shows waiting overlay with room code and cancel button. When 2 players join, master starts the game via `PhotonNetwork.LoadLevel`.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Init` | `(MainMenuUI)` | Store menu reference |
| `Open` | `(DeckSaveData)` | Reset state, ensure PhotonConnectionManager, connect, build UI |

---

#### GameOverUI
**File:** `Scripts/Wizard/UI/GameOverUI.cs`
**Inherits:** `MonoBehaviour`
**Role:** Displays a fullscreen dark overlay with result panel when the game ends (checkmate, stalemate, draw). Shows result text ("CHECKMATE!" / "STALEMATE!" / "DRAW!"), detail text ("White wins the game" etc.), and two buttons: "Rematch" (reloads Board scene keeping MatchConfig) and "Main Menu" (clears MatchConfig, loads MainMenu scene). Online matches redirect Rematch to Main Menu. Main Menu always disconnects from Photon. Watches `GameMaster.currentGameState` for terminal states. Attached to the GameMaster object.

| Method | Description |
|--------|-------------|
| `OnRematchClicked()` | Reloads Board scene (offline) or returns to menu (online) |
| `OnMainMenuClicked()` | Disconnects Photon, clears MatchConfig, loads MainMenu scene |

#### InGameMenuUI
**File:** `Scripts/Wizard/UI/InGameMenuUI.cs`
**Inherits:** `MonoBehaviour`
**Role:** In-game pause menu accessible via Escape key or bottom-right button. Provides Resign (with confirmation), Offer a Draw (network-synced in online mode, immediate in local/AI), Settings (opens SettingsUI overlay), Exit to Main Menu (disconnects Photon, clears MatchConfig), and Resume. Blocks board input while open via `IsMenuOpen` property checked by `GameMaster.Update()` and `PieceMove.OnMouseDown()`. Attached to the GameMaster object.

| Property/Field | Type | Description |
|----------------|------|-------------|
| `IsMenuOpen` | `bool` | Whether the pause menu overlay is visible (blocks input) |

| Method | Description |
|--------|-------------|
| `OpenMenu()` | Show pause menu, exit ability mode if active, deselect piece |
| `CloseMenu()` | Hide pause menu and confirm dialog |
| `ShowDrawOffer()` | Show draw offer popup to receiving player (called by RPC) |
| `OnDrawResponseReceived(bool)` | Handle opponent's draw response (called by RPC) |
| `OnOpponentResigned()` | Set game state to local win, close popups (called by RPC) |

---

#### SettingsUI
**File:** `Scripts/Wizard/UI/SettingsUI.cs`
**Inherits:** `MonoBehaviour`
**Role:** Reusable settings panel with resolution selector (left/right arrow cycling through `Screen.resolutions`) and fullscreen/windowed toggle. Created at runtime as a fullscreen overlay. Used by both `MainMenuUI` (title screen Settings button) and `InGameMenuUI` (pause menu Settings button). Applies changes immediately via `Screen.SetResolution()` and `Screen.fullScreenMode`.

| Method | Description |
|--------|-------------|
| `Init(Canvas, Action)` | Create UI on canvas, register close callback |
| `Show()` | Refresh state and show overlay |
| `Hide()` | Hide overlay |
| `IsVisible` | Whether the settings overlay is currently active |

---

#### GameLogUI
**File:** `Scripts/Wizard/UI/GameLogUI.cs`
**Inherits:** `MonoBehaviour`
**Role:** Scrollable in-game log panel on the right side of the screen. Displays moves, captures, abilities, check/checkmate/stalemate events as they happen. Uses a singleton pattern with static `Log()` methods so any class can add entries. Auto-scrolls to the latest entry. Attached to the GameMaster object at startup.

| Method | Description |
|--------|-------------|
| `Init(GameMaster)` | Store reference, create UI, set singleton instance |
| `static Log(string)` | Add a plain text entry to the log |
| `static LogMove(int turn, int color, string msg)` | Add a turn-numbered entry (e.g. "1. Knight E4") |
| `static LogPieceMove(int turn, int color, PieceMove, int toX, int toY)` | Log a piece moving to a square |
| `static LogCapture(int turn, int color, PieceMove attacker, PieceMove victim, int toX, int toY)` | Log a capture (e.g. "1. Knight x Pawn E4") |
| `static LogAbility(int turn, int color, PieceMove, int toX, int toY)` | Log an ability use |
| `static LogEvent(string)` | Log a game event with no turn prefix (check, checkmate, etc.) |
| `static ShortPieceName(int pieceType)` | Returns "Pawn", "Rook", etc. |
| `static SquareName(int x, int y)` | Returns "A1", "E4", etc. |

---

### Editor Tools

#### AbilityEditorWindow
**File:** `Scripts/Editor/AbilityEditorWindow.cs`
**Inherits:** `EditorWindow`
**Role:** Custom IMGUI-based editor window for viewing and balancing all 36 wizard chess abilities. Opens via menu: **WizardChess > Ability Balance Editor**.

**Layout:**
- Left panel: Save button, 3 color-coded element tabs (Fire=red-orange, Earth=brown-gold, Lightning=blue), 6 piece type buttons
- Right panel: Passive ability (editable name, editable description, editable params), Active ability (editable name, cooldown, editable description, editable params)
- Text fields show hardcoded defaults when config override is empty; "Reset" button clears override back to default
- Uses `SerializedObject`/`SerializedProperty` for proper undo and dirty support
- Auto-creates `AbilityBalanceConfig` asset in `Resources/` if none found

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
| `EarthKingPassive.cs` | `EarthKingPassive` | Passive | **Stone Shield** — Once per game, survives capture and destroys attacker |
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
| `LightningQueenActive.cs` | `LightningQueenActive` | Active (CD:7) | **Tempest** — Push enemies within 3 sq two squares away (kings immune) |
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

### MoveStepType
**File:** `Scripts/Wizard/Runtime/MoveStep.cs`

| Value | Description |
|-------|-------------|
| `MoveTo` | Animate piece to destination square |
| `Capture` | Capture a piece (remove from board) |
| `Custom` | Execute arbitrary action (effects, pushes) |

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
GameMaster ──────► MultiStepMoveController [NEW] (multi-step move orchestration)

PieceUI ─────────► PieceMove               (detects piece type changes for icon swap)

PieceMove ───────► GameMaster              (gm reference for state access)
PieceMove ───────► BoardState              (via gm.boardState for legality checks)
PieceMove ───────► Square                  (move targets, current position)
PieceMove ───────► DOTween                 (animation)
PieceMove ───────► ElementalPiece          [NEW] (optional elemental component)
PieceMove ───────► AbilityFactory          [NEW] (re-create abilities on promotion)
PieceMove ───────► Resources.Load          [NEW] (promotion mesh prefabs)

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
PieceTooltipUI ──► PieceIndexHelper       [NEW] (piece icon resource paths)
PieceTooltipUI ──► Canvas                 [NEW] (runtime UI creation)

AbilityUI ───────► GameMaster             [NEW] (selected piece, ability mode)
AbilityUI ───────► PieceIndexHelper       [NEW] (piece icon resource paths)

ElementParticleUI ► PieceMove             [NEW] (element info via elementalPiece)
ElementParticleUI ► ParticleSystem        [NEW] (runtime particle creation)

AbilityInfo ─────► ChessConstants         [NEW] (element/piece type constants)
AbilityInfo ─────► AbilityBalanceConfig   [NEW] (text overrides for ability names/descriptions)

AbilityModeUI ───► GameMaster             [NEW] (ability mode state)
AbilityModeUI ───► AbilityInfo            [NEW] (ability names)
AbilityModeUI ───► Canvas                 [NEW] (runtime UI creation)

CheckBannerUI ──► GameMaster             [NEW] (game state: check detection)
CheckBannerUI ──► Canvas                 [NEW] (runtime UI creation)

GameOverUI ─────► GameMaster             [NEW] (game state: game over detection)
GameOverUI ─────► Canvas                 [NEW] (runtime UI creation)
GameOverUI ─────► SceneManager           [NEW] (scene reload for New Game)

InGameMenuUI ───► GameMaster             [NEW] (game state, input blocking)
InGameMenuUI ───► NetworkGameController  [NEW] (resign/draw RPCs)
InGameMenuUI ───► PhotonConnectionManager [NEW] (disconnect on exit)
InGameMenuUI ───► MatchConfig            [NEW] (clear on exit)
InGameMenuUI ───► Canvas                 [NEW] (runtime UI creation)
InGameMenuUI ───► SceneManager           [NEW] (load MainMenu on exit)
InGameMenuUI ───► GameLogUI              [NEW] (log resign/draw events)
InGameMenuUI ───► SettingsUI             [NEW] (settings panel)

SettingsUI ─────► Canvas                 [NEW] (runtime UI creation)
SettingsUI ─────► Screen                 [NEW] (resolution/fullscreen control)

MainMenuUI ──────► SettingsUI            [NEW] (settings panel)

GameLogUI ──────► GameMaster             [NEW] (turn number, game state)
GameLogUI ──────► Canvas                 [NEW] (runtime UI creation)
GameLogUI ──────► ChessConstants         [NEW] (piece type names)

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
ChessAI ─────────► GameMaster             [NEW] (game state, move execution)
ChessAI ─────────► AIEvaluation           [NEW] (move scoring)
ChessAI ─────────► BoardState             [NEW] (piece queries, attack maps)
ChessAI ─────────► AbilityExecutor        [NEW] (ability execution)
ChessAI ─────────► GameLogUI              [NEW] (move/ability logging)
ChessAI ─────────► MultiStepMoveController [NEW] (animated double-jumps)
ChessAI ─────────► LightningKnightPassive  [NEW] (double-jump detection)

MultiStepMoveController ► GameMaster       [NEW] (game state access)
MultiStepMoveController ► PieceMove        [NEW] (animation methods)
MultiStepMoveController ► MoveStep         [NEW] (step data)
MultiStepMoveController ► DOTween          [NEW] (animation)

MoveStep ────────► PieceMove               [NEW] (piece reference)
MoveStep ────────► Square                  [NEW] (destination reference)

LightningKnightPassive ► KnightMoveData    [NEW] (move metadata tracking)
LightningPawnActive ──► MultiStepMoveController [NEW] (chain strike animation)
LightningQueenActive ─► MultiStepMoveController [NEW] (tempest push animation)
FireRookActive ───────► MultiStepMoveController [NEW] (inferno line animation)
FireKingActive ───────► MultiStepMoveController [NEW] (backdraft animation)
AIEvaluation ────► BoardState             [NEW] (position queries, attack maps)
AIEvaluation ────► ChessConstants         [NEW] (piece types, element IDs)
AIMatchPanel ────► MainMenuUI             [NEW] (navigation)
AIMatchPanel ────► MatchConfig            [NEW] (sets AI match config)
AIMatchPanel ────► DraftData              [NEW] (builds player + AI draft)
AIMatchPanel ────► SceneManager           [NEW] (loads Board scene)
MainMenuUI ──────► AIMatchPanel           [NEW] (panel management)
MainMenuUI ──────► OnlineMatchPanel      [NEW] (panel management)
OnlineMatchPanel ► PhotonConnectionManager [NEW] (connection lifecycle)
OnlineMatchPanel ► MatchConfig            [NEW] (sets online match config)
OnlineMatchPanel ► PhotonNetwork          [NEW] (scene load, custom properties)
NetworkGameController ► GameMaster        [NEW] (game state, move execution)
NetworkGameController ► BoardState        [NEW] (piece lookup for remote moves)
NetworkGameController ► PhotonView        [NEW] (RPC communication)
NetworkGameController ► GameLogUI         [NEW] (logging remote moves)
NetworkGameController ► CameraMove        [NEW] (set initial camera perspective)
PhotonConnectionManager ► PhotonNetwork   [NEW] (Photon PUN 2 API)
GameMaster ──────► NetworkGameController  [NEW] (online move sync, turn blocking)
GameOverUI ──────► PhotonConnectionManager [NEW] (disconnect on menu return)
CameraMove ──────► MatchConfig            [NEW] (blocks switching in online mode)
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

## Test System

### Overview

373 EditMode unit tests covering core chess rules, board state consistency, wizard systems, and all 36 elemental abilities. Tests use the Unity Test Framework (NUnit) and run entirely in the Editor without loading scenes.

### Assembly Definition Chain

```
WizardChess.Tests.EditMode.asmdef
  └── references: WizardChess, UnityEngine.TestRunner, UnityEditor.TestRunner
       └── WizardChess.asmdef (Assets/Scripts/)
            └── references: DOTween.Modules, PhotonUnityNetworking, PhotonRealtime
```

- `WizardChess.asmdef` puts all game scripts into a named assembly so the test assembly can reference them.
- The test asmdef uses `overrideReferences: true` with `nunit.framework.dll` precompiled reference.
- `defineConstraints: ["UNITY_INCLUDE_TESTS"]` ensures test code only compiles when the test framework is active.

### Test Helpers

#### ChessBoardBuilder
**File:** `Tests/EditMode/TestHelpers/ChessBoardBuilder.cs`
**Role:** Constructs a minimal Unity hierarchy for testing without loading a scene.

Creates:
- GameMaster GameObject with `BoardState`, `SquareEffectManager`, `AbilityExecutor`
- 8 row GameObjects (`boardRows[0..7]`)
- 64 Square children (8 per row) with correct `x`, `y` coordinates
- LineRenderer and PieceUI stubs (needed by `deSelectPiece`)

| Method | Description |
|--------|-------------|
| `Build()` | Create the full board hierarchy |
| `PlacePiece(type, color, x, y) → PieceMove` | Place a standard piece |
| `PlaceElementalPiece(type, color, x, y, element) → PieceMove` | Place a piece with elemental abilities |
| `GetSquare(x, y) → Square` | Get square reference |
| `GenerateMoves(piece) → List<Square>` | Generate and filter legal moves |
| `Cleanup()` | `DestroyImmediate` all GameObjects (call in `[TearDown]`) |

**Properties:** `BoardState`, `SEM` (SquareEffectManager), `GM` (GameMaster)

#### TestExtensions
**File:** `Tests/EditMode/TestHelpers/TestExtensions.cs`
**Role:** Static assert helpers for move validation.

| Method | Description |
|--------|-------------|
| `AssertContainsMove(moves, x, y, msg)` | Assert move list includes coordinate |
| `AssertDoesNotContainMove(moves, x, y, msg)` | Assert move list excludes coordinate |
| `AssertMoveCount(moves, expected, msg)` | Assert exact move count |

### Test Categories

#### Core Chess (12 files, ~145 tests)
| File | Coverage |
|------|----------|
| `BoardStateTests.cs` | Piece placement, removal, attack maps, check detection |
| `BoardStateSyncTests.cs` | PieceMove↔BoardState position agreement after moves/captures |
| `PawnMoveTests.cs` | Forward moves, captures, double move, blocking |
| `RookMoveTests.cs` | Sliding moves, blocking, capture inclusion |
| `KnightMoveTests.cs` | L-shape moves, corner cases, jump over pieces |
| `BishopMoveTests.cs` | Diagonal moves, blocking, capture |
| `QueenMoveTests.cs` | Combined rook+bishop moves |
| `KingMoveTests.cs` | Adjacent moves, cannot move into check |
| `CastlingTests.cs` | Kingside/queenside, conditions preventing castling |
| `EnPassantTests.cs` | En passant capture and target management |
| `CheckDetectionTests.cs` | Check by each piece type, checkmate, stalemate, capture/block/pin resolution |
| `EnPassantCheckTests.cs` | En passant + check interactions (illegal ep, ep resolving check, ep execution) |

#### Wizard Systems (4 files, ~45 tests)
| File | Coverage |
|------|----------|
| `CooldownTrackerTests.cs` | Cooldown start, tick, reset, ready state |
| `StatusEffectTests.cs` | Stunned/Singed application, tick, expiry |
| `SquareEffectTests.cs` | Fire/StoneWall/LightningField creation, HP, blocking |
| `ElementalPieceTests.cs` | Status effects, immunities, turn start behavior |

#### Ability Tests (18 files, ~180 tests)
One file per element-piece combination, testing both passive and active abilities:

| Element | Files |
|---------|-------|
| Fire | `FirePawnTests`, `FireRookTests`, `FireKnightTests`, `FireBishopTests`, `FireQueenTests`, `FireKingTests` |
| Earth | `EarthPawnTests`, `EarthRookTests`, `EarthKnightTests`, `EarthBishopTests`, `EarthQueenTests`, `EarthKingTests` |
| Lightning | `LightningPawnTests`, `LightningRookTests`, `LightningKnightTests`, `LightningBishopTests`, `LightningQueenTests`, `LightningKingTests` |

### Test Conventions

- Every test file uses `ChessBoardBuilder` in `[SetUp]` and calls `Cleanup()` in `[TearDown]`.
- Both kings must be placed for move generation (check detection requires king references).
- `LogAssert.Expect(LogType.Error, ...)` is used where `SquareEffect.RemoveEffect()` calls `Destroy()` in edit mode (which logs an error but still functions).
- Tests avoid hardcoding `AbilityBalanceConfig` parameter values since the ScriptableObject asset loads via `Resources.Load` in tests.

---

## File Map

```
Scripts/
├── WizardChess.asmdef         Assembly definition for game scripts
├── GameMaster.cs              Core orchestrator (modified — conditional DeckBasedSetup)
├── BoardState.cs              Board state manager (modified)
├── PieceMove.cs               Per-piece logic (modified — arc animation, rejection tracking)
├── Square.cs                  Board square (modified)
├── ChessMove.cs               Move recording (modified)
├── ChessConstants.cs          Constants + enums (modified)
├── CameraMove.cs              Camera control
├── PieceUI.cs                 UI overlay
├── BoardUI.cs                 World-to-2D utility
├── OutofBounds.cs             Board boundary detection
├── PieceCheck.cs              Piece collision handling
├── AI/
│   ├── ChessAI.cs                AI opponent controller (3 difficulty levels)
│   └── AIEvaluation.cs           Static evaluation: material, positional, ability scoring
├── Network/
│   ├── PhotonConnectionManager.cs DontDestroyOnLoad Photon connection singleton
│   └── NetworkGameController.cs   In-game RPC move/ability sync, color assignment
├── Menu/
│   ├── DeckSlot.cs               Single deck definition
│   ├── DeckSaveData.cs           All 9 deck slots container
│   ├── DeckPersistence.cs        JSON save/load utility
│   ├── MatchConfig.cs            Cross-scene static data bridge (modified — AI + online fields)
│   ├── PieceIndexHelper.cs       Piece index → type/label/icon mapping
│   ├── MainMenuUI.cs             Root menu controller (modified — AI + online match buttons)
│   ├── AIMatchPanel.cs           AI match setup panel (difficulty + deck selection)
│   ├── OnlineMatchPanel.cs       Online match setup panel (matchmaking + deck selection)
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
    │   ├── SquareEffectManager.cs      Effect manager singleton (modified — GetBlockingEffectName)
    │   ├── AbilityExecutor.cs          Ability targeting mode
    │   ├── AbilityFactory.cs           Ability instance factory
    │   ├── MoveStep.cs                 Multi-step move data class
    │   ├── MultiStepMoveController.cs  Multi-step move orchestrator
    │   ├── MoveRejectionTracker.cs     Move rejection tracking for debug/UI (NEW)
    │   ├── FireVsEarthSetup.cs         Auto-assigns Fire/Earth to teams
    │   └── DeckBasedSetup.cs           Per-piece element setup from decks
    └── UI/
        ├── AbilityUI.cs                Ability button + cooldown
        ├── AbilityInfo.cs              Static ability/effect name/description lookups
        ├── AbilityModeUI.cs            Ability mode banner indicator
        ├── CheckBannerUI.cs            Check state red banner
        ├── GameLogUI.cs                Scrollable in-game move/event log
        ├── GameOverUI.cs               Game over overlay + scene reset
        ├── InGameMenuUI.cs             In-game pause menu (resign, draw, settings, exit)
        ├── MoveExplanationUI.cs        Invalid move tooltip with rejection reasons (NEW)
        ├── SettingsUI.cs               Resolution and display mode settings panel
        ├── PieceTooltipUI.cs           Mouse-over piece info tooltip
        ├── ElementParticleUI.cs        Element-colored particle effects on pieces
        ├── SquareEffectUI.cs           Square effect visuals
        └── ElementIndicatorUI.cs       Piece element tinting

Resources/
├── ChessIcons/                       Chess piece icon sprites for DeckEditorPanel
│   ├── pawn.png
│   ├── rook.png
│   ├── knight.png
│   ├── bishop.png
│   ├── queen.png
│   └── king.png
└── PromotionPrefabs/                 Prefabs for pawn promotion (mesh + material swap)
    ├── QueenDark.prefab
    ├── QueenLight.prefab
    ├── RookDark.prefab
    ├── RookLight.prefab
    ├── BishopDark.prefab
    ├── BishopLight.prefab
    ├── KnightDark.prefab
    └── KnightLight.prefab

Tests/
└── EditMode/
    ├── WizardChess.Tests.EditMode.asmdef   Test assembly definition
    ├── TestHelpers/
    │   ├── ChessBoardBuilder.cs            Board hierarchy builder for tests
    │   └── TestExtensions.cs               Assert helpers for move validation
    ├── Core/
    │   ├── BoardStateTests.cs              Board state CRUD and attack maps
    │   ├── BoardStateSyncTests.cs          Position desync detection
    │   ├── PawnMoveTests.cs                Pawn move generation
    │   ├── RookMoveTests.cs                Rook move generation
    │   ├── KnightMoveTests.cs              Knight move generation
    │   ├── BishopMoveTests.cs              Bishop move generation
    │   ├── QueenMoveTests.cs               Queen move generation
    │   ├── KingMoveTests.cs                King move generation
    │   ├── CastlingTests.cs                Castling rules
    │   ├── EnPassantTests.cs               En passant rules
    │   ├── EnPassantCheckTests.cs         En passant + check interactions
    │   └── CheckDetectionTests.cs          Check, checkmate, stalemate, pins
    ├── Wizard/
    │   ├── CooldownTrackerTests.cs         Cooldown state machine
    │   ├── StatusEffectTests.cs            Status effect lifecycle
    │   ├── SquareEffectTests.cs            Square effect creation and behavior
    │   └── ElementalPieceTests.cs          Elemental piece component
    └── Abilities/
        ├── Fire/
        │   ├── FirePawnTests.cs            Scorched Earth + Flame Rush
        │   ├── FireRookTests.cs            Trail Blazer + Inferno Line
        │   ├── FireKnightTests.cs          Splash Damage + Eruption
        │   ├── FireBishopTests.cs          Burning Path + Flame Cross
        │   ├── FireQueenTests.cs           Royal Inferno + Meteor Strike
        │   └── FireKingTests.cs            Ember Aura + Backdraft
        ├── Earth/
        │   ├── EarthPawnTests.cs           Shield Wall + Barricade
        │   ├── EarthRookTests.cs           Fortified + Rampart
        │   ├── EarthKnightTests.cs         Tremor Hop + Earthquake
        │   ├── EarthBishopTests.cs         Earthen Shield + Petrify
        │   ├── EarthQueenTests.cs          Tectonic Presence + Continental Divide
        │   └── EarthKingTests.cs           Stone Shield + Sanctuary
        └── Lightning/
            ├── LightningPawnTests.cs       Energized + Chain Strike
            ├── LightningRookTests.cs       Overcharge + Thunder Strike
            ├── LightningKnightTests.cs     Double Jump + Lightning Rod
            ├── LightningBishopTests.cs     Voltage Burst + Arc Flash
            ├── LightningQueenTests.cs      Swiftness + Tempest
            └── LightningKingTests.cs       Reactive Blink + Static Field
```
