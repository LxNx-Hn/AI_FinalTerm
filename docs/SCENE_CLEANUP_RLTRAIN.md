# RLTrain Scene Cleanup Notes

- RLTrain scene path: `Assets/Project/Scenes/Boss01_Elevator_RLTrain.unity`
- Source scene: `Assets/Project/Scenes/Boss01_Elevator.unity`
- Source scene was preserved and not edited directly.

## What was changed for RLTrain

- Scene was duplicated through Unity MCP into `Boss01_Elevator_RLTrain.unity`.
- `ElevatorBossController` gained an RL-only serialized flag, `skipIntroForTraining`.
- RL intro skip reproduces the code-defined post-intro battle entry state:
  - player control enabled
  - boss visible
  - boss HUD visible
  - boss battle starts without the intro dialogue/landing sequence

## Cutscene / battle-start state

- Code review result: this project currently moves the player forward by **one cell**, not two.
- Evidence:
  - `ElevatorBossController.IntroRoutine()` calls `MovePlayerOneCellForIntro()`
  - `MovePlayerOneCellForIntro()` computes `targetCell = playerOccupant.CurrentCell + moveDir`
- RLTrain skip mirrors that one-cell move in code.

## UI / audio / VFX handling

- Preserved:
  - `Player`
  - `Boss_Elevator`
  - `Grid` / `GridManager`
  - `BossPatternCaster`
  - `ElevatorBossController`
  - `BossHealth`
  - `PlayerHealth`
  - `PlayerController`
  - `PlayerCombat`
  - `GridMover`
  - `DamageTile.prefab`
  - `WarningTile.prefab`
  - existing boss VFX and warning/damage VFX references

- Not automatically removed:
  - HP UI roots
  - existing game-over UI
  - boss audio source / runtime SFX hooks

- Reason:
  - the scene is already battle-focused
  - preserving runtime references is safer for the first smoke-ready MVP
  - RL wrapper handles episode termination via scene reload before post-boss or death flows become the primary loop

## Manual checks still recommended

- Confirm the saved RLTrain scene has `skipIntroForTraining` enabled on `Boss_Elevator`.
- Confirm the active training start state in Play Mode matches the intended one-cell intro-advanced state.
- Confirm no unexpected UI overlay blocks agent visibility during smoke verification.
