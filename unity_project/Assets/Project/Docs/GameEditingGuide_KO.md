# CODE BLUE — 게임 편집 가이드 (한글)

> **대상 독자:** 개발 이후 게임을 다듬는 디자이너·아티스트·프로그래머  
> **현재 상태:** 스크립트·프리팹·씬 구조 완성. 스프라이트·오디오 교체 및 맵 조정만 남음.

---

## 목차

1. [빠른 시작 — 즉시 플레이 확인](#1-빠른-시작--즉시-플레이-확인)
2. [씬 구조 이해](#2-씬-구조-이해)
3. [맵 편집 — Tilemap 사용법](#3-맵-편집--tilemap-사용법)
4. [벽과 장애물 배치](#4-벽과-장애물-배치)
5. [스프라이트 에셋 교체](#5-스프라이트-에셋-교체)
6. [애니메이션 설정](#6-애니메이션-설정)
7. [적 배치 및 스탯 조정](#7-적-배치-및-스탯-조정)
8. [아이템 배치](#8-아이템-배치)
9. [트리거 존 편집](#9-트리거-존-편집)
10. [UI 텍스트·색상 변경](#10-ui-텍스트색상-변경)
11. [보스 스탯 조정](#11-보스-스탯-조정)
12. [컷씬 메시지 편집](#12-컷씬-메시지-편집)
13. [씬 빌드 재실행 방법](#13-씬-빌드-재실행-방법)
14. [에셋 교체 체크리스트](#14-에셋-교체-체크리스트)
15. [알려진 제한 사항](#15-알려진-제한-사항)

---

## 1. 빠른 시작 — 즉시 플레이 확인

Unity 에디터에서 콘솔 에러가 없는 상태에서:

1. **File → Build Settings** — 씬 순서 확인:
   ```
   0: Assets/Project/Scenes/Entry.unity
   1: Assets/Project/Scenes/Stage01_ParkingLobby.unity
   2: Assets/Project/Scenes/Stage02_WardElevator.unity
   3: Assets/Project/Scenes/Boss01_Elevator.unity
   4: Assets/Project/Scenes/RooftopEndingWalk.unity
   5: Assets/Project/Scenes/EndingCredits.unity
   ```
2. `Entry` 씬 열기 → **Play** 버튼 → START 버튼 클릭으로 전체 플로우 확인

**플레이 조작:**
- 이동: `WASD` 또는 방향키
- 공격: 마우스 좌클릭
- 대시: 마우스 우클릭 (3칸, 3초 쿨다운)
- 게임오버 후 재시작: `R`

---

## 2. 씬 구조 이해

모든 게임플레이 씬의 최상위 오브젝트 구조:

```
00_System      ← GridManager, GameState(S01만), EventSystem, SceneLoader
01_Floor       ← Grid 컴포넌트 (Tilemap 컨테이너)
  └ FloorMap   ← Tilemap + TilemapRenderer (Sorting Layer: Floor)
02_Walls       ← GridObstacle 벽 프리팹 인스턴스
03_Props       ← 시각 전용 장식 (게임플레이 무관)
04_Enemies     ← EnemyAI 인스턴스
05_Items       ← GridItem(HealItem, KeyCard 등) 인스턴스
06_Triggers    ← 트리거 존 오브젝트
07_UI          ← Canvas (HP바, GameOver 패널 등)
08_Camera      ← Main Camera + CameraFollow
```

---

## 3. 맵 편집 — Tilemap 사용법

바닥 타일은 Unity Tilemap 시스템으로 관리됩니다. 각 씬의 `01_Floor/FloorMap` 오브젝트가 Tilemap입니다.

### Tile Palette로 타일 그리기

1. **Window → 2D → Tile Palette** 창 열기
2. Palette 드롭다운에서 팔레트가 없으면 **Create New Palette** → `FloorPalette` 이름으로 `Assets/Project/Art/Tiles/` 폴더에 생성
3. `Assets/Project/Art/Tiles/FloorTile_A.asset`, `FloorTile_B.asset` 을 팔레트로 드래그
4. Scene 뷰에서 `FloorMap` Tilemap 선택 → 팔레트에서 타일 선택 → 클릭/드래그로 페인팅

### 타일 삭제

- Tile Palette에서 지우개(Eraser) 선택 → 씬에서 클릭

### 체커보드 패턴 규칙

빌더는 `(x + y) % 2 == 0` 이면 FloorTile_A, 아니면 FloorTile_B를 사용합니다. 수동으로 타일을 배치할 때 이 규칙을 따르면 패턴이 유지됩니다.

### 좌표계 확인

GridManager의 `tileSize=1`, `worldOrigin=(0,0)`과 Tilemap의 `cellSize=(1,1,0)`이 1:1 대응합니다.  
타일 위치 = 그리드 셀 좌표 = 월드 정수 좌표.

---

## 4. 벽과 장애물 배치

### 벽 추가

1. `Assets/Project/Prefabs/System/Wall.prefab`을 씬의 `02_Walls` 하위로 드래그
2. Transform X, Y를 정수로 설정

> **중요:** 벽은 `GridObstacle` 컴포넌트를 가지며 `Awake`에 GridManager에 자동 등록됩니다. 리빌드 불필요.

### 차량 장애물 (Stage01)

2×3 크기의 벽 블록. `BoxPrototypeBuilder.PlaceVehicle()`이 자동 생성하며 수동으로 이동하려면 각 블록(`Vehicle_S01_01_0_0` 등)을 정수 단위로 이동합니다.

### 벽 삭제

Hierarchy에서 선택 후 Delete. GridManager가 `OnDestroy` 시 자동 해제합니다.

---

## 5. 스프라이트 에셋 교체

### 교체 파일 목록과 권장 사양

| 교체 대상 | 경로 | 권장 크기 | PPU | 피벗 |
|---------|------|----------|-----|------|
| 바닥 타일 A | `Assets/Project/Art/Tiles/FloorTile_A.png` | 32×32 | 32 | Center |
| 바닥 타일 B | `Assets/Project/Art/Tiles/FloorTile_B.png` | 32×32 | 32 | Center |
| 플레이어 | `Player.prefab` → SpriteRenderer | 32×32 | 32 | Bottom |
| 소형 적 | `SmallEnemy.prefab` → SpriteRenderer | 32×32 | 32 | Bottom |
| 중형 적 | `MediumEnemy.prefab` → SpriteRenderer | 48×48 | 32 | Bottom |
| 보스 | `Boss01_Elevator` 씬 → Boss_Elevator → SpriteRenderer | 64×64 | 32 | Center |
| 경고 타일 | `WarningTile.prefab` → SpriteRenderer | 32×32 | 32 | Center |
| 데미지 타일 | `DamageTile.prefab` → SpriteRenderer | 32×32 | 32 | Center |
| 안전 타일 | `SafeTile.prefab` → SpriteRenderer | 32×32 | 32 | Center |
| 회복 아이템 | `HealItem.prefab` → SpriteRenderer | 16×16 | 32 | Center |
| 키카드 | `KeyCard.prefab` → SpriteRenderer | 16×16 | 32 | Center |

### PNG 교체 방법

1. 기존 PNG 파일을 덮어쓰거나 Project 창에서 파일을 교체
2. Unity가 자동으로 재임포트

**바닥 타일 PNG 교체 시:** `FloorTile_A.asset` (`Tile` ScriptableObject)이 같은 PNG의 스프라이트를 참조하므로 Tilemap에 **즉시** 반영됩니다.

### Bottom Pivot 적용 후 FootHitbox 조정

Bottom Pivot 스프라이트로 교체하면 발 위치 기반 피격 판정이 맞지 않을 수 있습니다.  
`Player.prefab` 선택 → `FootHitbox` 자식 오브젝트 → Position Y를 `0` → `-0.5`로 변경.

---

## 6. 애니메이션 설정

현재 스프라이트는 정적 이미지입니다. 애니메이션을 추가하려면:

### 플레이어 애니메이션

1. `Assets/Project/Art/Player/` 폴더에 스프라이트 시트 임포트 (Sprite Mode: Multiple, PPU: 32)
2. Sprite Editor로 슬라이싱
3. **Window → Animation → Animator** 창 열기
4. `Player.prefab` 선택 → Animator 컴포넌트 추가
5. `Assets/Project/Animations/Player/` 폴더 생성 → Animator Controller 생성
6. Idle, Walk_Up/Down/Left/Right, Attack, Dash 클립 생성
7. `PlayerController.cs` (또는 별도 스크립트)에서 `Animator.SetTrigger()` / `SetFloat()` 호출

### 적 애니메이션

같은 방식으로 `SmallEnemy.prefab`, `MediumEnemy.prefab`에 Animator 추가.  
`EnemyAI.cs`에 `[SerializeField] private Animator anim;` 필드를 추가하고 이동/공격 시 트리거.

### 보스 애니메이션

`Boss_Elevator` GameObject에 Animator 추가.  
`ElevatorBossController.cs`의 각 Phase 진입 / 취약 윈도우 활성화 시점에 트리거 호출 추가.

> **현재 코드에는 Animator 참조가 없습니다.** 위 내용은 애니메이션 추가 시 가이드 방향이며, 실제 구현은 해당 스크립트 수정이 필요합니다.

---

## 7. 적 배치 및 스탯 조정

### 적 이동

1. Hierarchy의 `04_Enemies` 하위에서 적 선택
2. Inspector에서 Transform X, Y를 정수로 변경

### 적 추가

1. `Assets/Project/Prefabs/Enemy/SmallEnemy.prefab` 또는 `MediumEnemy.prefab`을 `04_Enemies` 하위로 드래그
2. 위치를 정수 좌표로 설정
3. `EnemyAI` 컴포넌트에서 `startActive = false` (트리거로 활성화할 경우)
4. `06_Triggers`의 `EnemyActivateTrigger` 또는 `WaveTrigger`에 적 추가

### 스탯 조정 (Inspector에서)

| 컴포넌트 | 필드 | 기본값 | 설명 |
|---------|------|--------|------|
| `EnemyHealth` | `maxHp` | 소형: 2, 중형: 5 | 최대 체력 |
| `EnemyAI` | `moveInterval` | 0.5초 | 한 칸 이동 주기 |
| `EnemyAI` | `detectionRange` | 12칸 | 플레이어 감지 거리 |
| `EnemyAttack` | `damage` | 1 | 공격 데미지 |
| `EnemyAttack` | `attackCooldown` | 0.8초 | 공격 쿨다운 |

---

## 8. 아이템 배치

아이템 프리팹은 `Assets/Project/Prefabs/Item/` 아래에 있습니다.

| 프리팹 | 효과 |
|--------|------|
| `HealItem.prefab` | HP +1 회복 |
| `KeyCard.prefab` | `GameState.hasKeycard = true` (Stage02 엘리베이터 통과 필수) |
| `AttackBuffItem.prefab` | 공격력 2배 버프 (일시적) |
| `SpeedBuffItem.prefab` | 이동 속도 증가 버프 (일시적) |

### 추가 방법

1. 프리팹을 `05_Items` 하위로 드래그
2. 위치를 정수 좌표로 설정
3. `GridItem` 컴포넌트에서 `itemType` 확인 (KeyCard는 반드시 `Keycard` 타입이어야 함)

---

## 9. 트리거 존 편집

모든 트리거는 `GridZoneTriggerBase`를 상속하며 `06_Triggers` 하위에 있습니다.

### 공통 Inspector 필드

| 필드 | 설명 |
|------|------|
| `size` (Vector2Int) | 트리거 존의 넓이 × 높이 (그리드 셀 단위) |
| `triggerOnce` | true = 최초 1회만 발동 |

트리거 위치는 GameObject Transform 위치 (정수 좌표, 존의 중심).

### EnemyActivateTrigger

- `enemies` 배열: `04_Enemies`에서 `EnemyAI` 컴포넌트 드래그
- 플레이어가 존에 진입하면 나열된 적이 활성화

### WaveTrigger

- `enemiesToEnable` 배열: `04_Enemies`에서 GameObject 드래그
- `EnemyActivateTrigger`와 달리 GameObject 직접 참조 (EnemyAI 아님)

### StageClearTrigger

- `nextSceneName`: Build Settings의 씬 이름과 정확히 일치해야 함 (예: `Stage02_WardElevator`)
- `requireKeycard`: true이면 `GameState.hasKeycard == true`일 때만 통과

### CutsceneTrigger (RooftopEndingWalk)

- `message`: 화면에 표시할 텍스트
- `showDuration`: 텍스트 표시 시간 (초)
- `lockPlayerDuringCutscene`: true이면 컷씬 중 이동 불가

### FinalEndingTrigger (RooftopEndingWalk)

- `endingSceneName`: 기본값 `EndingCredits`
- `delayBeforeLoad`: 씬 전환 전 대기 시간 (초)

---

## 10. UI 텍스트·색상 변경

### HP 바 텍스트

각 씬 `07_UI/Canvas/HPText` 오브젝트:
- `TextMeshProUGUI` 컴포넌트에서 폰트·색상 변경
- `HpTextUI` 스크립트가 런타임에 `"HP: {cur} / {max}"` 형식으로 자동 갱신

### 게임오버 패널

`07_UI/Canvas/GameOverPanel` → `GameOverText` TextMeshProUGUI에서 텍스트 변경.  
현재: `"GAME OVER\nPress R to Retry"`. 한글로 변경 가능.

### 보스 HP 패널 (Boss01만)

`07_UI/Canvas/BossHPPanel/BossHPText` TextMeshProUGUI 편집.

### Entry

`Canvas/TitleText`: 게임 타이틀 변경  
`Canvas/StartButton/Text`: 버튼 레이블 변경 (onClick 리스너는 수정 금지)  
`Canvas/BackgroundPanel`: Image 컴포넌트 Color로 배경색 변경

### EndingCredits

`Canvas/TitleText`, `Canvas/CreditText`, `Canvas/FinalText`: 직접 편집

---

## 11. 보스 스탯 조정

Boss01 씬(`Assets/Project/Scenes/Boss01_Elevator.unity`)의 `Boss_Elevator` 오브젝트:

### 변경 가능한 항목

| 컴포넌트 | 필드 | 기본값 | 설명 |
|---------|------|--------|------|
| `BossHealth` | `maxHp` | 40 | 보스 최대 체력 |
| `ElevatorBossController` | `vulnerableTime` | 4초 | 취약 윈도우 지속 시간 |
| `ElevatorBossController` | `maxHitsPerVulnerable` | 3 | 취약 중 최대 피격 허용 횟수 |
| `BossPatternCaster` | `warningDuration` | 1.5초 | 경고 타일 → 데미지 타일 전환 시간 |

### 변경 금지 항목 (기획서 고정값)

- `ElevatorBossController` Phase 전환 HP 임계값 (30/20/4)
- `BossPatternCaster` 패턴 레이아웃 코드 (N, Z, Stripe, CrossBombing 등)
- `patternOrigin` 위치 (반드시 `(0,0,0)` 유지)
- `nextSceneName` (반드시 `RooftopEndingWalk` 유지)

---

## 12. 컷씬 메시지 편집

`RooftopEndingWalk` 씬의 `06_Triggers` 하위:

| 오브젝트 | x 위치 | 기본 메시지 |
|---------|--------|-----------|
| `CutsceneTrigger_01` | -10 | `"...Stop..."` |
| `CutsceneTrigger_02` | -2 | `"That wasn't a monster."` |
| `CutsceneTrigger_03` | 6 | `"The memory returns."` |

편집 방법: 오브젝트 선택 → Inspector → `CutsceneTrigger` 컴포넌트 → `Message` 필드 수정

컷씬 위치(x 좌표) 조정: Transform X 값을 변경.  
`FinalEndingTrigger` (x≈14): `delayBeforeLoad` 필드로 EndingCredits 전환 딜레이 조정.

---

## 13. 씬 빌드 재실행 방법

맵을 크게 변경하거나 씬 구조를 초기화하려면:

1. Unity 메뉴 → **Project Tools → Create FloorTile Placeholders**  
   (FloorTile PNG + Tile asset + Prefab 재생성. 이미 있어도 안전하게 덮어씀)
2. **Project Tools → Build Box Prototype (All)**  
   모든 6개 씬을 한번에 재빌드

또는 특정 씬만:
- **Project Tools → Build Box Prototype → [2] Stage01_ParkingLobby** (번호는 씬명 참조)

> **주의:** 재빌드는 해당 씬의 기존 오브젝트 위에 추가 배치하며, 바닥 Tilemap은 덮어쓰기합니다.  
> 직접 배치한 커스텀 오브젝트(적, 아이템, 트리거 등)는 이름 중복 체크로 보존됩니다.  
> 완전 초기화가 필요하면 씬 파일 직접 삭제 후 재빌드.

---

## 14. 에셋 교체 체크리스트

스프라이트·오디오를 실제 에셋으로 교체할 때 확인 순서:

```
[ ] FloorTile_A.png / FloorTile_B.png 교체 → Tilemap 즉시 반영
[ ] Player.prefab SpriteRenderer.sprite 교체
    └ Bottom Pivot 시 FootHitbox Y 위치 0 → -0.5 로 변경
[ ] SmallEnemy.prefab sprite 교체
[ ] MediumEnemy.prefab sprite 교체
[ ] Boss01 씬 Boss_Elevator SpriteRenderer.sprite 교체
[ ] WarningTile.prefab sprite 교체 (권장 색: 노란색)
[ ] DamageTile.prefab sprite 교체 (권장 색: 빨간색)
[ ] SafeTile.prefab sprite 교체 (권장 색: 초록색)
[ ] HealItem.prefab sprite 교체
[ ] KeyCard.prefab sprite 교체
[ ] BGM 오디오 클립 → AudioSource (00_System에 추가)
[ ] SFX 클립 → PlayerCombat, EnemyAttack, GridItem, CutsceneTrigger 각 컴포넌트
[ ] UI 폰트 교체 → TextMeshPro 설정에서 Font Asset 변경
[ ] Entry TitleText 최종 게임 이름으로 변경
[ ] EndingCredits CreditText 팀 크레딧 작성
[ ] Build Settings Player Settings → Product Name, Icon 설정
```

---

## 15. 알려진 제한 사항

- **애니메이션 없음:** 모든 캐릭터가 정적 스프라이트. Animator Controller 추가 필요.
- **오디오 없음:** BGM·SFX 미연결. AudioSource 컴포넌트 및 클립 배치 필요.
- **FootHitbox 위치:** Bottom Pivot 스프라이트 사용 전까지 중심(0,0,0) 유지. 교체 후 -0.5로 조정.
- **FillWallCol 비대칭 지원 없음:** 열 방향 벽은 yMin/yMax가 아닌 ±yRange 방식. 비대칭 열은 PlaceWall()로 수동 배치.
- **Ward 코리도 적 트리거 미연결:** Stage02 복도 6명의 적은 트리거 없이 배치됨. EnemyActivateTrigger에 수동 연결 필요.
- **런타임 바닥 스폰 없음:** 바닥 타일은 빌더가 1회 배치. 플레이어 스폰 위치가 바닥 밖이면 카메라 배경색만 표시.
- **MediumEnemy 미작성 완료:** 중형 적 프리팹이 있지만 별도 패턴 없음. EnemyAI 기본 행동 사용.

---

*최종 업데이트: 2026-05-10. 현재 구현 기준 (Tilemap 마이그레이션 완료 버전)*
