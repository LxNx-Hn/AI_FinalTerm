# CODE BLUE — Box Prototype Editing Guide

This document explains how to modify the editable box prototype created by `BoxPrototypeBuilder`.  
Target readers: designer, programmer, artist joining after the first-pass build.

---

## 1. Running the Builder

Open Unity. With no errors in the Console:

1. **First run only:** `Project Tools → Bootstrap Missing Scenes` — verifies the generated scenes (Entry, RooftopEndingWalk, EndingCredits).
2. **First run only:** `Project Tools → Create FloorTile Placeholders` — creates `FloorTile_A/B` placeholder sprites and prefabs under `Assets/Project/Prefabs/Tiles/`.  
3. `Project Tools → Build Box Prototype (All)` — populates all 6 scenes with floor tiles, walls, enemies, triggers, and UI.

You can also rebuild a single scene:  
`Project Tools → Build Box Prototype → [2] Stage01_ParkingLobby` etc.

> **Important:** Running the builder on an already-built scene is additive for most objects (it skips existing ones by name) but will regenerate floor tiles. It is safer to run per-scene when you only want to refresh one area.

---

## 2. Scene Overview & Build Order

| # | Scene file | Role |
|---|------------|------|
| 0 | `Entry` | Title + START/QUIT buttons |
| 1 | `Stage01_ParkingLobby` | Parking area, first enemies, no keycard needed |
| 2 | `Stage02_WardElevator` | Hospital ward, keycard puzzle, elevator exit |
| 3 | `Boss01_Elevator` | 7×7 arena elevator boss fight |
| 4 | `RooftopEndingWalk` | Non-combat walk, three cutscene beats |
| 5 | `EndingCredits` | Credits screen |

Build Settings already lists them in this order (Boss02 is disabled).

---

## 3. Scene Hierarchy Conventions

Every gameplay scene uses this top-level object structure:

```
00_System      — GridManager, GameState (Stage01 only), EventSystem, SceneLoader
01_Floor       — FloorTile_A / FloorTile_B instances
02_Walls       — GridObstacle wall instances
03_Props       — Visual-only decorations (no gameplay)
04_Enemies     — All enemy prefab instances
05_Items       — Pickups (Heal, Keycard, etc.)
06_Triggers    — All trigger zone GameObjects
07_UI          — Canvas, HUD, GameOver panel
08_Camera      — Main Camera + CameraFollow
```

Keep objects inside the right parent so the hierarchy stays readable.

---

## 4. Moving and Editing Floor Tiles

Floor tiles are placed at integer world positions (1 unit = 1 grid cell). `GridManager` defaults to `worldOrigin=(0,0)` and `tileSize=1`, so world position equals cell coordinate.

- **Move a tile:** Select it in the Scene view and drag or type exact integers in the Transform X/Y fields.  
- **Add tiles:** Drag `FloorTile_A` or `FloorTile_B` from `Assets/Project/Prefabs/Tiles/` into `01_Floor`. Position at an integer.  
- **Remove tiles:** Select and delete. This does not affect any gameplay logic.  
- **Checkerboard pattern:** Tiles alternate A/B based on `(x + y) % 2 == 0` — if you move a tile, swap the prefab type if you care about the pattern.

Floor tiles have no colliders. Only `GridObstacle` (on Wall prefabs) blocks movement.

---

## 5. Placing and Moving Walls

Wall prefabs are under `Assets/Project/Prefabs/System/Wall.prefab`.  
Each wall registers itself as a blocked cell in `GridManager` on `Awake`.

- **Move a wall:** Translate to the new integer position in the Inspector.  
- **Add a wall:** Drag the `Wall` prefab into `02_Walls`, position at an integer.  
- **Remove a wall:** Delete the GameObject. GridManager deregisters on destroy.  

> Walls do NOT need to be rebuilt when you move them — they self-register at runtime.

---

## 6. Adjusting Enemy Positions and Counts

All enemies live under `04_Enemies`. Enemy prefabs:
- `Assets/Project/Prefabs/Enemy/SmallEnemy.prefab`
- `Assets/Project/Prefabs/Enemy/MediumEnemy.prefab`

To **move** an enemy: translate its position to any integer grid cell inside the floor area.

To **add** an enemy:
1. Drag the prefab into `04_Enemies`.
2. Set world position (integer X, Y).
3. In the `EnemyAI` component, set `startActive = false` if the enemy should wait for a trigger.
4. Add it to the nearest `EnemyActivateTrigger` or `WaveTrigger` in `06_Triggers`.

To **remove** an enemy:
1. Delete the GameObject.
2. Also remove it from any trigger's enemy list in the Inspector.

Enemy stats (HP, damage, detection range, move interval) are on `EnemyHealth` and `EnemyAI` — adjust in the Inspector.

---

## 7. Working with Trigger Zones

All triggers extend `GridZoneTriggerBase`. Fields visible in the Inspector:

| Field | Effect |
|-------|--------|
| `size` (Vector2Int) | Width × Height of the trigger zone in grid cells |
| `triggerOnce` | If true, fires only the first time the player enters |

Trigger positions are in world space (integer units). The zone is centered on the GameObject position.

### EnemyActivateTrigger
- **Enemies:** drag `EnemyAI` components from the enemy GameObjects in `04_Enemies`.
- When the player enters the zone, all listed enemies become active.

### WaveTrigger
- **Enemies To Enable:** drag the enemy GameObjects (not EnemyAI) directly.
- Used for mid-combat waves; sets enemies active on entry.

### StageClearTrigger
- **Next Scene Name:** must match exactly the scene name in Build Settings (e.g. `Stage02_WardElevator`).
- **Require Keycard:** if true, the player must hold the keycard (`GameState.hasKeycard == true`) to pass.

### CutsceneTrigger (RooftopEndingWalk only)
- **Message:** the text that appears in the cutscene panel.
- **Show Duration:** seconds the panel stays on screen.
- **Lock Player During Cutscene:** freeze player movement while text is displayed.

### FinalEndingTrigger (RooftopEndingWalk only)
- **Ending Scene Name:** defaults to `EndingCredits`.
- **Delay Before Load:** seconds after the player steps in before the scene loads.

---

## 8. Player Prefab Reference

Prefab: `Assets/Project/Prefabs/Player/Player.prefab`  
Child: `FootHitbox` — the actual damage-detection point. Keep it at local position `(0, 0, 0)` now; move to `(0, -0.5, 0)` after Bottom Pivot art assets arrive.

Key tunable fields:

| Component | Field | Current value | Notes |
|-----------|-------|---------------|-------|
| `GridMover` | `moveDuration` | 0.23 s | Time per one-cell move |
| `PlayerDash` | `dashMoveDuration` | 0.20 s | Time for 3-cell dash |
| `PlayerDash` | `dashCooldown` | 3.0 s | Dash recharge time |
| `PlayerHealth` | `maxHp` | 3 | Player hit points |
| `PlayerCombat` | `attackDamage` | 1 | Damage per swing |

> In `RooftopEndingWalk`, `PlayerCombat` and `PlayerDash` are disabled in the scene (not on the prefab). Do not touch the prefab to disable them.

---

## 9. Item Placement

Items live under `05_Items`. Available prefabs under `Assets/Project/Prefabs/Item/`:

| Prefab | Effect on pickup |
|--------|-----------------|
| `HealItem.prefab` | Restores 1 HP |
| `KeyCard.prefab` | Sets `GameState.hasKeycard = true` |

To add an item, drag the prefab into `05_Items` and position it on an integer cell.  
`GridItem` auto-picks up when the player occupies the same cell.  
For `KeyCard`, verify `GridItem.itemType == Keycard` (set via the Inspector or `ItemType` enum index 3).

---

## 10. Boss Arena (Boss01_Elevator)

### What to change freely
- Player start position (currently `(-3, -3, 0)`)
- Camera `orthographicSize` on `Main Camera` (currently 5)
- Boss HP: `BossHealth.maxHp` (default 40)
- Vulnerable window: `ElevatorBossController.vulnerableTime` (default 4 s)
- Max hits per vulnerable: `ElevatorBossController.maxHitsPerVulnerable` (default 3)
- Pattern prefabs: `BossPatternCaster.warningTilePrefab`, `damageTilePrefab`, `safeTilePrefab`

### What NOT to change (locked)
- Phase thresholds in `ElevatorBossController` (design document locked values)
- Pattern layout code in `BossPatternCaster` (N, Z, Stripe3, CrossBombing, etc.)
- `patternOrigin` position — must stay at `(0, 0, 0)` (arena center)
- `nextSceneName` — must stay `"RooftopEndingWalk"`

The 7×7 arena floor spans cells `(-3,-3)` to `(3,3)`. Walls are at `±4`.

---

## 11. Editing the Cutscene Messages (RooftopEndingWalk)

Three `CutsceneTrigger` objects in `06_Triggers`:

| Name | Position | Default message |
|------|----------|-----------------|
| `CutsceneTrigger_01` | x = -10 | "...Stop..." |
| `CutsceneTrigger_02` | x = -2 | "That wasn't a monster." |
| `CutsceneTrigger_03` | x = 6 | "The memory returns." |

To edit: select the trigger → Inspector → `Message` field.  
To reposition a cutscene beat: move the trigger's X coordinate.  
`FinalEndingTrigger` at x = 14 loads `EndingCredits` after 1 second.

---

## 12. UI Elements

### HUD (Stage01, Stage02, Boss01)
- `HPText` — `HpTextUI` component auto-updates from `PlayerHealth.onHpChanged`
- `BuffStatusText` — currently a static label; wire a buff display script when ready
- `GameOverPanel` — hidden by default; `GameOverController` shows it on player death, R key reloads scene

### Boss HUD (Boss01 only)
- `BossHPPanel/BossHPText` — `BossHpTextUI` auto-updates from `BossHealth.onHpChanged`

### Entry
- `TitleText` — edit text directly in Inspector
- `StartButton/Text` — change label text only; do NOT rewire the onClick listener (it calls `SceneLoader.LoadScene("Stage01_ParkingLobby")`)
- `QuitButton` — calls `SceneLoader.QuitGame()`

### EndingCredits
- `TitleText` / `CreditText` / `FinalText` — all `TextMeshProUGUI`; edit directly in Inspector

---

## 13. Adding a New Scene (Not in This Prototype)

1. Create the scene file in `Assets/Project/Scenes/`.
2. Open `ProjectSettings/EditorBuildSettings.asset` or use `File → Build Settings` to add it.
3. Update the `nextSceneName` on the `StageClearTrigger` in the preceding scene.
4. For a new boss scene: copy `Boss01_Elevator` as a starting point.

---

## 14. Sorting Layers

Art assets should be placed on these layers (in draw order, back to front):

| Layer | Use |
|-------|-----|
| Background | Sky, background art |
| Floor | FloorTile_A / B |
| Props | Furniture, crates, visual-only decor |
| Items | Pickups |
| Characters | Player, enemies |
| Boss | Boss sprite |
| PatternWarning | Yellow warning tiles |
| Hazard | Red damage tiles |
| UI | Always on top |

---

## 15. Known Limitations of This Prototype

- **No runtime-spawned floors:** Floor tiles are placed once by the builder. Moving the player spawn to an area without floor tiles just shows the camera background color.
- **FillWallCol uses ±yRange:** The column fill helper takes an abs range, not `yMin/yMax`. If you need asymmetric column bounds, add wall objects manually.
- **ward corridor patrol enemies:** The 6 enemies in Stage02's corridor are placed but have no trigger assigned by default. Wire them to an `EnemyActivateTrigger` manually if you want them to respond to player entry.
- **Prefab missing warnings:** If a referenced prefab path doesn't exist (e.g., `MediumEnemy.prefab`), the builder creates an empty placeholder with the correct name instead of crashing. Replace with the real prefab when assets arrive.
- **FootHitbox position:** Currently at `(0,0,0)` (sprite center). Adjust to `(0,-0.5,0)` after bottom-pivot sprites are imported so damage detection aligns with feet.

---

## 16. Validation Checklist Before Each Playtest

```
[ ] No red errors in Console before entering Play Mode
[ ] Player starts at the correct position and can move immediately
[ ] First trigger in the scene activates enemies as expected
[ ] Stage clear trigger advances to the correct scene
[ ] Boss01: patterns fire, vulnerable window opens, R+click deals damage
[ ] RooftopEndingWalk: all 3 cutscenes display, FinalEndingTrigger loads credits
[ ] GameOver panel appears on player death, R key reloads
[ ] No lingering DamageTile objects after boss pattern ends
```

---

*Guide last updated: 2026-05-10. Corresponds to BoxPrototypeBuilder.cs phase implementation.*
