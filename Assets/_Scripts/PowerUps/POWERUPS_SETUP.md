# Power-Ups — Unity Setup Guide

Two power-ups: **Puppy** (auto-place one correct puppy) and **Bulb** (guided-deduction
hint that previews/commits X marks). Each has 5 chances, a green count badge, and a
fake rewarded-ad refill. Chances persist via PlayerPrefs.

All C# is in `Assets/Scripts/PowerUps/`. No scene/prefab files were edited — wire the
following in the Editor.

---

## 1. Persistent managers (Loading / bootstrap scene)

Put these in the **first-loaded scene** (e.g. `Loading.unity`). Both are singletons with
`DontDestroyOnLoad`, so they carry into the gameplay scene.

1. Empty GameObject **`PowerUpManager`** → add `PowerUpManager` component.
   - `Default Chances` = **5**.
2. Empty GameObject **`AdService`** → add `DummyRewardedAdService` component.
   - Leave `Ad Panel` empty for now (assign in step 4 if the FakeAdPanel lives in the
     same persistent scene; otherwise see the note at the end).

> PlayerPrefs key reset (for testing a fresh save): delete keys
> `powerup_chances_PuppyReveal` and `powerup_chances_Hint`, or call
> `PlayerPrefs.DeleteAll()` from a temporary editor script.

---

## 2. Power-up controllers (gameplay scene)

In the gameplay scene (the one with `GameManager`, the grid, the HUD):

1. Empty GameObject **`PuppyRevealPowerUp`** → add `PuppyRevealPowerUp` component. No fields.
2. Empty GameObject **`HintController`** → add `HintController` component. Fields wired in step 5.

`PuppyRevealPowerUp` needs nothing else — it calls `GameManager.Instance.RevealCorrectPuppy()`.

---

## 3. Bottom Power-Up Bar (gameplay scene UI Canvas)

On your gameplay Canvas, create a bottom bar with two buttons.

For **each** button (Puppy, Bulb):

```
PuppyButton  (Button + Image=puppy.png  ← Assets/Art/Sprites/UI/puppy.png)
  └─ CountBadge      (Image = green circle, anchored top-right)
       └─ CountText  (TextMeshPro - Text, e.g. "5")
BulbButton   (Button + Image=light.png  ← Assets/Art/Sprites/UI/light.png)
  └─ CountBadge
       └─ CountText
```

On each Button GameObject add a **`PowerUpButton`** component:
- `Power Up` → drag the matching controller (`PuppyRevealPowerUp` or `HintController`).
- `Count Text` → the badge's `CountText` (TMP).
- `Empty Glyph` → `+` (default).
- `Press Punch Scale` / `Duration` → defaults are fine.

The badge auto-refreshes from `PowerUpManager.OnChancesChanged` and on enable. At 0 it
shows `+`; tapping then opens the ad.

> Do NOT add an `onClick` listener in the Inspector — `PowerUpButton` wires its own.

---

## 4. Fake Ad Panel

Create a disabled fullscreen overlay (own Canvas or a child panel that sorts on top):

```
FakeAdPanel  (full-screen dark Image)        ← add FakeAdPanel component, start DISABLED
  ├─ CountdownText  (TextMeshPro - Text)
  └─ CloseButton    (Button, label "Claim")
```

On the `FakeAdPanel` component:
- `Root` → the FakeAdPanel object itself (or leave empty; it defaults to its own GameObject).
- `Countdown Text` → `CountdownText`.
- `Close Button` → `CloseButton`.
- `Ad Duration` → 3.

Then assign this `FakeAdPanel` to **`DummyRewardedAdService.Ad Panel`** (step 1, item 2).

---

## 5. Hint overlay (gameplay scene UI)

Create a hint overlay — a dim backdrop with an Apply button, a Cancel button, and a
tutorial text line. Start it **disabled**:

```
HintOverlay  (semi-transparent dark Image, raycast target ON so it blocks taps)  ← DISABLED
  ├─ TutorialText  (TextMeshPro - Text)
  ├─ ApplyButton   (Button, label "Apply")
  └─ CancelButton  (Button, label "Cancel")
```

On the **`HintController`** component (step 2):
- `Hint Overlay` → `HintOverlay`.
- `Apply Button` → `ApplyButton`.
- `Cancel Button` → `CancelButton`.
- `Tutorial Text` → `TutorialText`.
- `Elimination Message` → shown while previewing X marks around a puppy (default fine).
- `Deduction Message` → shown when the hint outlines where a puppy must go (default fine).
- `Stagger` → 0.04 (cascade speed for the X marks).

> The hint is **progressive**: each press handles the next revealed puppy (newest →
> oldest) that still has unmarked row/column/diagonal cells, previewing X's that Apply
> commits. Once every puppy's cells are marked (or none is revealed), the hint switches to
> **deduction mode** — it outlines the cell where a puppy must go (from the solver), and
> **Apply places the puppy on that cell** (Cancel dismisses without placing). Wire both
> buttons normally; the script handles the per-mode behaviour.

> The dim backdrop's `Image` should have **Raycast Target** on so the player can't tap the
> grid behind it. Grid input is also locked in code via `GameManager.InputLocked`, so the
> board is double-protected during a hint. Apply/Cancel sit above the backdrop.

The board dimming, gold focus glow, and ghost X marks are driven on the grid cells
themselves (not this overlay) — the overlay is just the backdrop + buttons + text.

---

## 6. Cell prefab — optional gold focus glow

The Bulb hint glows the focused puppy gold. `Cell` will use an optional
`Highlight Overlay` renderer; if you don't add one, it falls back to tinting the existing
white overlay gold (works, just less distinct).

To add the nicer version, on the **Cell prefab**:
1. Add a child SpriteRenderer (a soft circle/glow sprite), sized a bit larger than the cell,
   sorting order **above** the zone overlay but **below** the puppy.
2. Assign it to `Cell.Highlight Overlay`.
3. Set its starting alpha to 0 (the script controls visibility).

`Hint Focus Color`, `Hint Dim Amount`, and `X Preview Alpha` on `Cell` are tunable.

---

## Notes
- A real ad SDK later: implement `IRewardedAdService` and swap it in; `PowerUpButton`
  only depends on `DummyRewardedAdService.Instance` today — change that reference point.
- If `FakeAdPanel` must be reachable from the persistent `DummyRewardedAdService` but lives
  in the gameplay scene, either (a) keep the whole ad UI in the persistent scene, or
  (b) have `DummyRewardedAdService` find the panel on scene load. Simplest: keep the ad UI
  in the persistent scene.
