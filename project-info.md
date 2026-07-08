# Pawsdoku — Project Info

2D animal-themed sudoku-style puzzle game. Place puppies on a grid so that **no two puppies share a row, column, color zone, or touch diagonally**. Built in Unity.

- **Unity version:** 6000.3.9f1 (Unity 6), URP 17.3.0, Input System 1.18.0, Newtonsoft.Json, DOTween, TextMeshPro
- **Scenes:** `Assets/_Scenes/` — `Loading.unity` (bootstrap) → `Home.unity` (level map, tabs) → `Game.unity` (gameplay)
- **Scripts:** `Assets/_Scripts/` (56 files), organized by subsystem
- **Levels:** JSON in `Assets/StreamingAssets/Levels/` + `levels_manifest.json` (currently: Tutorial_01, Level_01, Level_77, Daily_01)
- **Theme asset:** `Assets/_ThemeData/PuppyThemeData.asset`

---

## Game Rules (`Core/GameRules.cs`)

A placement is valid iff:
1. Only one puppy per color zone (zone IDs map 1:1 to `ColorID` enum, 12 colors)
2. No two puppies in the same row
3. No two puppies in the same column
4. No puppy on any of the 4 diagonally-adjacent cells

`GetRuleRestrictedCells()` returns row + column + 4 diagonals of a puppy (used by Hint).

## Level JSON Format (`Core/LevelDataModel.cs`)

```json
{
  "levelId": "Level_01",          // prefix determines type: Level_/Daily_/Tutorial_/Event_
  "difficulty": "Easy",            // Easy | Medium | Hard
  "gridSize": 4,
  "levelType": "Normal",           // Tutorial | Normal | DailyChallenge | Event
  "displayName": null,
  "colorData": [[0,0,1,0], ...],   // zone/color IDs per cell (jagged int[][])
  "prePlaced": [{"x":3,"y":2}],    // given puppies
  "solution": [{"x":3,"y":2}, ...],// optional authored solution (validated if present)
  "winCondition": 4                // # puppies to win; -1 = auto (unique color count)
}
```

`Core/LevelData.cs` is a legacy ScriptableObject format, no longer used at runtime.

## Architecture

### Persistent singletons (created in Loading scene, DontDestroyOnLoad)
| Singleton | Responsibility |
|---|---|
| `LevelLoader` | Scans/loads StreamingAssets levels, level progression, scene transitions |
| `PowerUpManager` | Power-up chances (default 5 each), `OnChancesChanged` event |
| `DailyChallengeManager` | Daily state/timer, resets on date change |
| `ThemeManager` | Active `ThemeData`; `ThemeManager.Current`, `OnThemeChanged` |
| `SFXManager` | 10-slot SFX pool + BGM blending, `Play(SFXType)` |
| `VFXManager` | Pooled VFX, `Play(VFXType, pos)` |

### Game scene (per-level)
- **`GameManager`** (singleton) — game state, lives (3), win/lose. Statics: `LevelComplete`, `LevelFailed`, `InputLocked`. Key APIs: `LoadLevelFromJson()`, `TryPlacePuppy()`, `RevealCorrectPuppy()`, `TryGetNextSolutionCell()`, `ReviveAfterAd()`. Tracks `PlacementOrder` (givens first, newest last).
- **`GridManager`** — builds the visual grid from zone map; auto-scales to portrait width (~48% coverage). `GetCell(x,y)`, `GetZoneMap()`.
- **`InputManager`** — single tap = toggle X mark; **double tap = place puppy**; swipe = drag-toggle X marks (≥18 px). Respects `InputLocked`.
- **`Cell`** — X marks, zone overlay, `PlacePuppy()`, 8-step `ShowPermanentRedCross()` error sequence, hint glow/dim/X-preview APIs.
- **`PuzzleObject` / `PuppyAnimator` / `PuppyRegistry`** — puppy visuals; Wink on valid placement, Sad broadcast on invalid.
- **`HUDManager`** — hearts, progress X/total, level label, daily timer; theme-aware.
- **`PopupManager`** — win/lose/daily-result/tutorial-complete popups. Popups: VictoryPanel, DefeatPanel (Revive via ad), LifePanel, ShopPanel (stub), SettingsPanel, UserProfilePanel.
- **`GridAnimator`** — staggered diagonal intro animation (DOTween).

### Home scene
- **`NavigationManager`** — Home/Shop/DailyChallenge tabs (DOTween slide).
- **`HomeSceneUI`** — scrollable level map using GameVanilla `LevelButton` prefabs (from `_AlonesherAssets/PuzzleMatchKit`).

### Power-ups (`_Scripts/PowerUps/`, setup guide: `POWERUPS_SETUP.md`)
- **Puppy Reveal** — auto-places one valid puppy via `PuzzleSolver.TrySolve()` (backtracking + MRV).
- **Hint (Bulb)** — multi-step guided deduction: invalidation → elimination (X-preview forbidden cells of most recent puppy) → deduction (gold glow on next solution cell). Apply/Cancel, refunds on cancel.
- `PowerUpButton` UI with count badge; at 0 shows `+` and opens **`DummyRewardedAdService`** + `FakeAdPanel` (3s fake ad). Real ads not integrated.

### Editor tools (`_Scripts/Editor/`)
- **Level Editor**: `Window > PuppyPuzzle > Level Editor` — paint zones, pre-place puppies, save JSON.
- **Level Validator**: `Window > PuppyPuzzle > Level Validator` — validates all level JSONs.

## PlayerPrefs Keys

`TutorialCompleted`, `SavedLevelKey` (current normal level, stable key), `powerup_chances_Reveal`, `powerup_chances_Hint` (booster counts), `economy_Coins`, `economy_PawGems`, `DailyChallenge.StateDay/.LevelNumber/.Completed/.CompletionTime/.Percentile/.ElapsedSeconds`, `Settings.Music/.Sound/.Haptic`, `next_level` (legacy, level-button highlighting).

## Theming (`Theme/ThemeData.cs`)

ScriptableObject per theme: puppy prefab/animator/sprite, cell sprites + colors, heart sprites (full/broken halves), HUD hearts, background, optional SFX/VFX config overrides. Swap at runtime via `ThemeManager.SetTheme()`.

## Known Gaps / TODO

- Real rewarded ads not integrated (DummyRewardedAdService only)
- ShopPanel is a stub — no IAP/currency
- Daily percentile is fake (no backend); no cloud save
- `LevelType.Event` defined but unused; no pause menu
- Duplicate/stray level folders: root `StreamingAssets/Levels/` (Level_01, Level_02) and `Assets/Levels/Level_78.json` are outside the canonical `Assets/StreamingAssets/Levels/`
- `Assets/_Recovery/` contains recovered scenes (`0.unity`, `0 (1).unity`)
- `Assets/Test/` contains scratch test scripts

## Key Design Decisions

- Zone ID == ColorID (identity mapping); `colorData` is both zoning and palette.
- Optional hidden solution: if present, placements validated against it; otherwise solver/rules validate.
- Single tap never places — X mark only; double tap places (prevents accidental placement).
- Global `GameManager.InputLocked` gates all input during hints/popups.
- All motion via DOTween sequences linked to GameObjects (auto-cleanup).
