# Manual Unity Setup Checklist

## 필수: Play 진입 전 Compile-Stable Gate 통과

Play Mode 진입 전 반드시 gate 절차를 통과해야 합니다.
컴파일/domain reload 중 Play에 진입하면 보스와 플레이어 모두 멈출 수 있습니다.

→ 자세한 절차: [EDITOR_SMOKE_STABLE_GATE.md](EDITOR_SMOKE_STABLE_GATE.md)

---

## Before Play

- Unity project 경로 확인: `C:/Users/Guest0000/Desktop/AI_FinalTerm0602/unity_project`
- Unity MCP 연결 확인 (coplay-mcp)
- Active scene: `Assets/Project/Scenes/Boss01_Elevator_RLTrain.unity`
- Console에 compile error 없음 확인
- `Boss_Elevator`에 `Skip Intro For Training` 체크 확인 (Inspector)
- Play Mode 꺼져 있음 확인 (`is_playing = false`)
- `is_compiling = false` 확인
- `is_changing = false` 확인

---

## Player 컴포넌트 확인

Hierarchy에서 `Player` 오브젝트를 선택하고 아래 컴포넌트가 모두 있는지 확인:

- `PlayerController`
- `PlayerCombat`
- `PlayerHealth`
- `GridMover`
- `GridOccupant`
- `BossRLInputBridge`
- `BossRLStateExtractor`
- `BossRLReward`
- `BossRLEpisodeResetter`
- `BossRLDebugLogger`
- `BossPlayerAgent`
- `Behavior Parameters`
- `Decision Requester`

Missing Script나 null reference가 있으면 즉시 중단.

---

## Behavior Parameters 설정

| 항목                     | 요구 값       |
|--------------------------|---------------|
| Behavior Name            | `BossPlayer`  |
| Behavior Type            | `Default`     |
| Space Type (Observation) | `Discrete`    |
| Vector Observation Size  | `193`         |
| Discrete Branch 0 Size   | `6`           |

현재 action space는 단일 Discrete branch입니다.

```text
0 WAIT
1 UP
2 DOWN
3 LEFT
4 RIGHT
5 ATTACK
```

---

## Decision Requester 설정

| 항목                         | 요구 값 |
|------------------------------|---------|
| Decision Period              | `5`     |
| Take Actions Between Decisions | `false` |

---

## Boss_Elevator 확인

- `skipIntroForTraining` = `true` (Inspector에서 "Skip Intro For Training" 체크박스)
- `BossHealth` 컴포넌트 존재
- `BossPatternCaster` 컴포넌트 존재 (`patternOrigin` 필드 null 아님)
- `ElevatorBossController` 컴포넌트 존재

---

## Grid / GridManager 확인

- `GridManager` 오브젝트 존재 및 Instance 접근 가능
- `Grid` 오브젝트 존재

---

## Scene Validate 기준

아래 항목이 없어야 함:
- Missing Script
- Broken Prefab
- Player 오브젝트의 Missing Reference
- Boss_Elevator 오브젝트의 Missing Reference

---

## Smoke 실행 절차

1. Compile-Stable Gate 통과 확인 ([EDITOR_SMOKE_STABLE_GATE.md](EDITOR_SMOKE_STABLE_GATE.md))
2. PowerShell에서 trainer 실행:

```powershell
.\scripts\run_editor_smoke.ps1
```

3. trainer 로그에 `Listening on port 5004` 확인
4. Unity Editor에서 Play 버튼 클릭
5. trainer 로그에 `Connected to Unity environment` 확인
6. 3~5 episode 확인 후 Play 중단

---

## 중단 조건

아래 중 하나라도 발생하면 즉시 Play 중단:

- Compile error 발생
- Missing Script 발견
- `Behavior Parameters` action spec 불일치 (단일 Discrete branch size 6이 아님)
- RLTrain scene이 active scene이 아님
- `skipIntroForTraining`이 false이고 인트로 cutscene이 실행됨
- `Academy.InitializeEnvironment NullReferenceException` 반복 발생
- trainer communicator timeout 발생
- `Time.timeScale = 0` 상태로 보스/플레이어 모두 멈춤

---

## 보호 대상

- `C:/Users/KiKi/CODE-BLUE` 원본 폴더 수정 금지
- GitHub `LxNx-Hn/CODE-BLUE` 수정 금지
