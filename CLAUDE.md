# CLAUDE.md — WizardChess2 Project Instructions

## Project Overview

WizardChess2 is a Unity-based chess game (currently standard chess, wizard mechanics planned). The Unity project lives under `Wizard Chess/`. All custom game logic is in `Wizard Chess/Assets/Scripts/`.

## Architecture Reference (READ FIRST)

**Before scanning source files, always read `ARCHITECTURE.md` at the project root.** It contains a complete class reference, data flow diagrams, and dependency maps. Use it to locate the correct file and understand relationships before opening any `.cs` file. Only scan source directly if the architecture doc doesn't answer the question or you need line-level detail.

## Keeping ARCHITECTURE.md Up To Date

Whenever you make changes that affect project architecture, you **must** update `ARCHITECTURE.md` before finishing the task. This includes:

- Adding, removing, or renaming a class or file
- Adding or changing public methods or properties on any class
- Changing class responsibilities or relationships
- Adding new enums, constants, or data structures
- Changing the data flow or event sequence between classes

Small internal refactors (renaming private variables, fixing a bug inside a method body) do not require an update.

## Project Structure

```
WizardChess2/
├── CLAUDE.md                          # This file
├── ARCHITECTURE.md                    # Class reference & architecture (read first)
├── Wizard Chess/                      # Unity project root
│   ├── Assets/
│   │   ├── Scripts/                   # All custom C# game logic (11 files)
│   │   ├── Scenes/                    # Board.unity (single scene)
│   │   ├── Prefabs/                   # Piece and UI prefabs
│   │   ├── Art Assets/                # Visuals, materials, audio
│   │   ├── Free Low Poly Chess Set/   # 3D chess piece models
│   │   ├── OUTLINED Icons Pack Vol1/  # UI icons
│   │   ├── 2DxFX/                     # Visual effects library
│   │   ├── Programming Assets/        # DOTween tweening library
│   │   └── Resources/                 # Runtime-loaded resources
│   ├── ProjectSettings/               # Unity config
│   └── Packages/                      # Package manifest
```

## Key Conventions

- **Engine:** Unity (C#, MonoBehaviour-based)
- **Animation:** DOTween (`DG.Tweening`) for all piece/camera movement
- **Constants:** Use `ChessConstants` static class — never use magic numbers for piece types, colors, or board size
- **Piece types:** PAWN=1, ROOK=2, KNIGHT=3, BISHOP=4, QUEEN=5, KING=6
- **Colors:** BLACK=1, WHITE=2
- **Board indexing:** 0-based, x=column (a-h), y=row (1-8). White starts at y=6-7, Black at y=0-1
- **Tags:** `"Board"` (squares), `"Piece"` (chess pieces), `"GM"` (GameMaster object)
- **Move validation:** All moves go through `PieceMove.createPieceMoves()` → `filterIllegalMoves()` → `HashSet` lookup via `checkMoves()`
- **Board state:** `BoardState` is the single source of truth for piece positions. Always update it via `GameMaster.UpdateBoardState()` / `RegisterPiece()` / `RemovePieceFromBoardState()`

## Task Management

This project uses Claude Code task tracking. When working on multi-step features or bugs:

1. Create tasks with `TaskCreate` for each distinct piece of work
2. Mark tasks `in_progress` when starting, `completed` when done
3. Use `addBlockedBy` / `addBlocks` for dependencies between tasks
4. Check `TaskList` after completing a task to find the next one

## Testing

- Unity Test Framework is installed (`com.unity.test-framework 1.1.33`) but no tests exist yet
- When adding tests, place them in `Wizard Chess/Assets/Tests/` (EditMode and PlayMode subfolders)
- Prioritize testing chess rules logic (`BoardState`, `PieceMove` move generation, `filterIllegalMoves`)

## Build & Run

- Open `Wizard Chess/` in Unity Hub
- Main scene: `Wizard Chess/Assets/Scenes/Board.unity`
- Camera controls: Keys 1 (White view), 2 (Black view), 3 (Top-down)
- No CLI build pipeline configured — use Unity Editor to build

## Known Incomplete Features

- Pawn promotion: auto-promotes to Queen (needs UI picker)
- `ChessMove.undoMove()`: stub, not implemented
- Game over: logs to console only (no UI)
- No new game / reset functionality
- No "wizard" mechanics yet — standard chess only
