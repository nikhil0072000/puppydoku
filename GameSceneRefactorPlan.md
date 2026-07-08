# Game Scene Refactor Plan

Goal: restructure the Game scene scripts (functionality intact). PowerUps become Boosters,
HUD becomes GameSceneUI + GameLivesManager, ads get their own module, dead code is removed.

## 1. Boosters (replaces Assets/_Scripts/PowerUps → Assets/_Scripts/Boosters, namespace PuppyPuzzle.Boosters)

| Old | New | Notes |
|---|---|---|
| PowerUpType | BoosterType | Entries: Reveal (renamed from PuppyReveal — its saved count key changed to `powerup_chances_Reveal`), Hint |
| PowerUpBase | Booster | Abstract base. Now also owns the per-booster UI refs + config (see below) |
| PowerUpManager | BoosterController | Singleton. ALL booster functionality: unlock check, consume/refund, buy-with-coins, rewarded-ad fallback. Counts stay in EconomyHandler |
| PowerUpButton | BoosterUIController | One controller referencing all Boosters; drives every UI state |
| PuppyRevealPowerUp | RevealBooster | Logic unchanged |
| HintController | HintBooster | Logic unchanged (refund goes through EconomyHandler) |
| PuzzleSolver | → Assets/_Scripts/Gameplay/PuzzleSolver.cs | Gameplay utility, global namespace (like GameRules) |
| DummyRewardedAdService, FakeAdPanel | → Assets/_Scripts/Ads/, namespace PuppyPuzzle.Ads | Ad infra is not a booster concern |

### Booster base — serialized fields
Config: `boosterType` (via abstract Type), `unlockAtLevel`, `coinPrice`.
UI: `icn` (Image), `countHolder` (GameObject) + `boosterCountText`, `adIcon` (GameObject),
`coinsHolder` (GameObject) + `boosterCoinsPriceText`, `lockState` (GameObject),
`boosterUnlockNumberText`, `unlockState` (GameObject). Button on same GameObject (RequireComponent).

### UI state flow (BoosterUIController.Refresh per booster)
1. **Locked** (player level < unlockAtLevel): lockState ON, unlockState OFF, button disabled,
   boosterUnlockNumberText = unlock level.
2. **Unlocked, count > 0**: countHolder ON (count from EconomyHandler), coins/ad OFF.
   Click → consume 1 booster → activate (refund on failure).
3. **Unlocked, count == 0, coins ≥ price**: coinsHolder ON (price), count/ad OFF.
   Click → spend coins → use booster once (coins refunded if it couldn't act).
4. **Unlocked, count == 0, coins < price**: adIcon ON, count/coins OFF.
   Click → rewarded ad (stub) → on reward, use booster once.

Refresh triggers: OnEnable, EconomyHandler.OnBoosterChanged, EconomyHandler.OnCurrencyChanged.
Player level for unlocks: new `LevelLoader.CurrentNormalLevelNumber`.

## 2. Game scene UI

| Old | New |
|---|---|
| HUDManager (hearts via sprite-swap Image[]) | **GameSceneUI** — all UI visuals/operations |
| — | **GameLivesManager** — instantiates 3 LifeVisual prefabs, SetLives/SpendLife |
| — | **LifeVisual** — prefab script, `lifeActiveVisual`/`lifeLostVisual` GameObjects, MarkLost/MarkAlive |

GameSceneUI serialized fields: playerIcon, playerName (ProfileStore + ProfileIconsSO via
PopupCanvasManager), levelNumberText, progressText, dailyTimerText, coinsTxt, gemsTxt
(live from EconomyHandler), coinsPlusBtn + gemsPlusBtn (open Shop popup), settingsBtn
(opens Settings popup), livesManager (GameLivesManager).

GameManager now talks to GameSceneUI (same call sites as old HUDManager; hearts go through
GameLivesManager).

## 3. Additional cleanup (from analysis)

- **GameManager**: ~90 lines of dead commented code removed (old LoadLevelFromJson, HexToColor);
  references GameSceneUI; lives visuals via SpendLife/SetLives.
- **ShopPanel.cs deleted** — old placeholder superseded by ShopPopup + ShopController
  (it referenced PowerUpManager and the ad-refill flow).
- **PopupManager** kept (win/lose/daily/revive flow) — updated to PuppyPuzzle.Ads.
  Candidate for a later migration to the PopupCanvasManager system.
- **POWERUPS_SETUP.md deleted** (described the old wiring).
- NOT deleted (pending your earlier decision): SettingsPanel.cs, SettingsToogle.cs.

## 4. Manual Unity wiring after this refactor

1. Game scene: replace HUDManager component with GameSceneUI; assign all fields
   (player info, currency bar, buttons, progress/level/daily texts).
2. Create LifeVisual prefab (active + lost child visuals), assign to GameLivesManager
   (component near GameSceneUI) with a horizontal container.
3. Boosters bar: each booster button gets its RevealBooster/HintBooster component
   (UI refs per the field list) — BoosterUIController references both; BoosterController
   GameObject in scene (replaces PowerUpManager in the bootstrap scene).
4. AdService GameObject keeps DummyRewardedAdService + FakeAdPanel (namespace move only;
   scripts must be re-assigned since the files were renamed/moved).
5. Old scene objects to remove: PowerUpManager, PowerUpButton components.
