# CODE BLUE — 완성 가이드 (vFinal-Lock-4 시점)

> **이 문서의 목적:** "지금 뭘 조작하고, 뭘 추가하면 게임이 끝나는가?"  
> 일반 편집 절차는 [GameEditingGuide_KO.md](GameEditingGuide_KO.md)를, 본문은 *완성까지 남은 결정·작업*에 집중합니다.

---

## 목차

1. [현재 게임 상태 한눈에](#1-현재-게임-상태-한눈에)
2. [조작법 (Player)](#2-조작법-player)
3. [에디터 운영 — 빌드 파이프라인](#3-에디터-운영--빌드-파이프라인)
4. [자산 교체 — 어떤 PNG를 어디에](#4-자산-교체--어떤-png를-어디에)
5. [애니메이션 추가 (현재 미구현)](#5-애니메이션-추가-현재-미구현)
6. [오디오·이펙트 (현재 미구현)](#6-오디오이펙트-현재-미구현)
7. [보스 밸런스 튜닝 위치](#7-보스-밸런스-튜닝-위치)
8. [씬 별 상세 체크리스트](#8-씬-별-상세-체크리스트)
9. [완성까지 남은 작업 목록](#9-완성까지-남은-작업-목록)
10. [Troubleshooting](#10-troubleshooting)

---

## 1. 현재 게임 상태 한눈에

| 영역 | 상태 |
|------|------|
| 그리드 시스템 (이동·충돌·점유) | ✅ 완성 |
| 플레이어 (이동·공격·대시·HP·버프) | ✅ 완성 |
| 일반 적 (소형·중형, 추적 AI, 데미지) | ✅ 완성 |
| 보스 — 엘리베이터 (vFinal-Lock-4) | ✅ 완성 |
| 패턴 시스템 (경고→피해 타일) | ✅ 완성 |
| 트리거 (활성화/스테이지 클리어/컷씬/엔딩) | ✅ 완성 |
| 아이템 (회복·공격·속도·키카드) | ✅ 완성 |
| 6 씬 빌드 (Menu → Stage01 → Stage02 → Boss01 → Rooftop → Credits) | ✅ 완성 |
| 플레이어 공격 히트박스 시각화 | ✅ 완성 (노란 깜빡임) |
| UI (HP / Boss HP / GameOver / Cutscene / Fade) | ✅ 완성 |
| **임포트 자산** (Evelyn idle / 아이템 3종) | ✅ 적용 |
| 캐릭터 애니메이션 | ⛔ 미구현 (idle 1프레임만) |
| 사운드·BGM | ⛔ 미구현 |
| Phase 전환 연출(rage flash 등) | ⛔ 미구현 (선택) |
| 보스 진입 컷씬 시각효과 | ⛔ placeholder만 (1.5s 대기) |
| Entry·EndingCredits 비주얼 | ⛔ 텍스트만 |

---

## 2. 조작법 (Player)

| 입력 | 동작 |
|------|------|
| `WASD` / 화살표 | 그리드 1칸 이동 |
| `좌클릭` | 전방 3×2 할퀴기 (0.5s 쿨다운) — **노란 사각형 깜빡임으로 범위 표시** |
| `우클릭` | 3칸 대시 (3.0s 쿨다운, 무적 프레임) |
| `R` | GAME OVER 시 현재 씬 재시작 |

**보스 등장 직후 1.5초간 입력 잠김** (포효 placeholder).

---

## 3. 에디터 운영 — 빌드 파이프라인

### 3-1. 6 씬 전체 재빌드

Unity 메뉴에서:
```
Project Tools → Build Box Prototype (All)
```
또는 `Project Tools → Build Box Prototype/{1~6}`로 개별 씬만.

### 3-2. 외부 트리거(스크립트 외부) 빌드

```
Temp/RunBoxPrototype.trigger  파일 생성 → Unity가 자동 빌드
Temp/ApplyImportedAssets.trigger 파일 생성 → 임포트 자산 다시 적용
```
PowerShell 예시:
```powershell
New-Item -ItemType File -Path "Temp/RunBoxPrototype.trigger" -Force
```

### 3-3. Placeholder 스프라이트 재생성

Unity 메뉴:
```
Project Tools → Create Placeholder Sprites
```
색상 PNG 10종을 `Assets/Project/Art/Placeholders/`에 생성 + 모든 프리팹에 자동 와이어링.

### 3-4. 임포트 자산 다시 적용

```
Project Tools → Apply Imported Assets
```
- 입력: `Assets/Project/Art/_Imported/Evelyn/...`, `_Imported/NewFolder/새 폴더/...`
- 출력: `Assets/Project/Art/Sprites/Characters`, `Assets/Project/Art/Sprites/Items`
- 적용 대상: Player.prefab, HealItem.prefab, SpeedBuffItem.prefab, AttackBuffItem.prefab

---

## 4. 자산 교체 — 어떤 PNG를 어디에

### 4-1. 즉시 효과 보는 자산 (PNG만 교체)

| 교체 대상 PNG | 적용 프리팹 / 에셋 | 권장 크기 / PPU |
|---------------|---------------------|------------------|
| `Assets/Project/Art/Sprites/Characters/Evelyn_Idle.png` | Player.prefab | 32×32 / PPU 32 |
| `Assets/Project/Art/Sprites/Items/Potion.png` | HealItem.prefab | 24~32 / PPU 32 |
| `Assets/Project/Art/Sprites/Items/Speed.png` | SpeedBuffItem.prefab | 24~32 / PPU 32 |
| `Assets/Project/Art/Sprites/Items/AttackBuff.png` | AttackBuffItem.prefab | 24~32 / PPU 32 |
| `Assets/Project/Art/Tiles/FloorTile_A.png` / `_B.png` | Tilemap (자동) | 32×32 / PPU 32 |
| `Assets/Project/Art/Placeholders/SmallEnemy_PH.png` | SmallEnemy.prefab | 32×32 |
| `Assets/Project/Art/Placeholders/MediumEnemy_PH.png` | MediumEnemy.prefab | 48×48 |
| `Assets/Project/Art/Placeholders/Boss_PH.png` | Boss01_Elevator 씬 BossSprite | 64×64 |
| `Assets/Project/Art/Placeholders/WarningTile_PH.png` | WarningTile.prefab (경고 노란) | 32×32 반투명 |
| `Assets/Project/Art/Placeholders/DamageTile_PH.png` | DamageTile.prefab (피해 빨강) | 32×32 반투명 |
| `Assets/Project/Art/Placeholders/SafeTile_PH.png` | SafeTile.prefab (안전 초록) | 32×32 반투명 |
| `Assets/Project/Art/Placeholders/AttackHitbox_PH.png` | AttackHitbox.prefab (공격 시각화) | 32×32 반투명 |
| `Assets/Project/Art/Placeholders/Wall_PH.png` | Wall.prefab | 32×32 |
| `Assets/Project/Art/Placeholders/KeyCard_PH.png` | KeyCard.prefab | 24×24 |

### 4-2. 외부 압축파일 자산 적용

`Assets/` 루트에 두면 됨:
```
Assets/Evelyn Sprite.zip     (캐릭터 스프라이트들)
Assets/새 폴더.zip           (아이템 아이콘 + RPG Maker 타일셋 .rar)
```
Unity 시작 시 자동 압축 해제 + Apply Imported Assets 메뉴로 적용.  
**.rar 안의 RPG Maker 타일셋은 추가 도구로 풀어 `Assets/Project/Art/Tiles/`에 직접 배치**해야 함.

---

## 5. 애니메이션 추가 (현재 미구현)

`Evelyn Sprite.zip`에는 walk(4프레임), attack, hurt, dead, pistol/axe/shotgun 변형이 있음.  
실제로 움직이게 하려면:

1. **스프라이트 시트 슬라이싱**  
   Inspector → Sprite Editor → Slice (Grid by Cell Size 또는 Automatic)

2. **Animator Controller 생성**  
   `Assets/Project/Animations/Player.controller` 신규 → 상태:
   - `Idle` (idle.png)
   - `Walk` (walk 1~4)
   - `Attack` (axe attack)
   - `Hurt` (hurt.png)
   - `Dead` (dead.png)

3. **Player.prefab에 Animator 컴포넌트 추가** + Controller 와이어

4. **PlayerController.cs / PlayerCombat.cs / PlayerHealth.cs** 에서 트리거 호출:
   ```csharp
   animator.SetFloat("Speed", isMoving ? 1f : 0f);
   animator.SetTrigger("Attack");
   animator.SetTrigger("Hurt");
   animator.SetBool("Dead", true);
   ```

`MoveBlend / AttackTrigger / HurtTrigger / DeadBool` 같은 파라미터 이름은 임의.

---

## 6. 오디오·이펙트 (현재 미구현)

`Assets/Project/Audio/` 폴더는 미생성. 필요한 클립:

| 카테고리 | 파일 예시 | 트리거 위치 |
|---------|-----------|-------------|
| BGM Stage | bgm_stage01.ogg | Stage01 진입 시 (`AudioManager` 신설 권장) |
| BGM Boss | bgm_boss.ogg | Boss01 진입 시 |
| SFX 공격 | sfx_attack.wav | PlayerCombat.Attack() |
| SFX 피격 | sfx_hurt.wav | PlayerHealth.TakeDamage() |
| SFX 대시 | sfx_dash.wav | PlayerDash 시작 |
| SFX 보스 포효 | sfx_boss_roar.wav | ElevatorBossController.IntroRoutine() |
| SFX 패턴 경고 | sfx_warning.wav | BossPatternCaster.CastCells (워닝 생성) |
| SFX 패턴 폭발 | sfx_explosion.wav | DamageTile 생성 시 |

`AudioSource` 컴포넌트를 GameObject에 추가하고 `audioSource.PlayOneShot(clip)` 호출.

---

## 7. 보스 밸런스 튜닝 위치

### 7-1. 인스펙터에서 (코드 수정 없음)

`Boss01_Elevator` 씬 → `Boss_Elevator` GameObject:

**BossHealth** 컴포넌트:
- `maxHp` (기본 40)

**ElevatorBossController** 컴포넌트:
- `vulnerableTime` (기본 2s — 명세 절반 적용)
- `maxHitsPerVulnerable` (기본 3)
- `introWaitSeconds` (기본 1.5s)
- `nextSceneName` (기본 RooftopEndingWalk)

**BossPatternCaster** 컴포넌트:
- `defaultWarningTime` (기본 0.8s)
- `defaultDamageTime` (기본 0.4s)

### 7-2. 코드 수정 영역

`Assets/Project/Scripts/Boss/ElevatorBossController.cs`:
- 페이즈 임계값: `EvaluatePhase()` (75% / 50% / 10%)
- HP>50% 기본공격 횟수: `BasicAttackLoop()` 의 `reps = 5`
- HP≤50% 기본공격 횟수: `reps = 3`
- 회차 사이 대기: `WaitForSeconds(0.15f)`
- 추적 이동 속도: `MoveBossSprite(bossCell, 0.2f)` 의 0.2초/칸
- Final 반복 횟수: `FinalPhaseRoutine()` 의 `for (int i=0; i<4; i++)`

`Assets/Project/Scripts/Boss/BossPatternCaster.cs`:
- 모든 패턴 셀 산출 함수 (벽2칸, 4밴드, 대각, N/Z, 마크돌진 등)

---

## 8. 씬 별 상세 체크리스트

### Entry
- ✅ 타이틀 텍스트 "CODE BLUE", START / QUIT 버튼
- 추가 권장: 로고 이미지, 배경, 버튼 hover 사운드

### Stage01_ParkingLobby
- ✅ 플레이어 스폰, 적 웨이브 트리거, HP UI, GameOver 패널
- ✅ StageClearTrigger → Stage02 로드 (키카드 불필요)
- 추가 권장: 환경 데코, BGM, 적 처치 시 효과

### Stage02_WardElevator
- ✅ 플레이어 스폰, 적 웨이브, KeyCard 픽업, 엘리베이터 트리거
- ✅ KeyCard 미보유 시 진입 차단 → 보유 시 Boss01 로드
- 추가 권장: 환경 데코, 키카드 강조 빛, BGM

### Boss01_Elevator (vFinal-Lock-4 핵심)
- ✅ 보스 (0,2) / 플레이어 (0,-2) / 7×7 아레나
- ✅ 1.5초 진입 컷씬 (입력 잠금)
- ✅ 기본공격 루프 (HP>50% 5회 / ≤50% 3회)
- ✅ Phase 1·2 첫 사이클 = 지정패턴 + 추가1
- ✅ Phase 3 첫 사이클 = MarkDash×4 + 추가1·2 중 1개
- ✅ Final = [강화찍기→랜덤 H/V 돌진]×4
- ✅ 일반 사이클: 추가1·2·3 중 서로 다른 2개 랜덤
- ✅ N/Z 획별 분리, 1-A 벽2칸 순환, 1-B 중앙 H→V
- ✅ 취약 윈도우 2초, 최대 3히트
- ✅ Boss HP UI 상단 중앙 표시
- 추가 권장: 보스 스프라이트 (현재 64×64 보라 사각형), 포효 애니메이션·사운드

### RooftopEndingWalk
- ✅ 전투/대시 비활성, 좌→우 이동만
- ✅ CutsceneTrigger 3개 ("...Stop...", "That wasn't a monster.", "The memory returns.")
- ✅ FinalEndingTrigger → EndingCredits
- 추가 권장: 옥상 배경, 황혼 톤, BGM

### EndingCredits
- ✅ "CODE BLUE" 크레딧 텍스트
- 추가 권장: 스태프 롤, 페이드아웃, 메뉴 복귀 버튼

---

## 9. 완성까지 남은 작업 목록

### Tier S — 출시 전 필수
- [ ] Player 캐릭터 워크 애니메이션 (4프레임 walk + idle)
- [ ] 보스 스프라이트 교체 (현재 placeholder 보라 64×64)
- [ ] BGM 6종 (메뉴, 스테이지×2, 보스, 옥상, 크레딧)
- [ ] SFX 8종 이상 (공격, 피격, 대시, 보스 포효, 경고, 폭발, 픽업, 처치)
- [ ] Entry 비주얼 강화

### Tier A — 게임성 향상
- [ ] 보스 진입 컷씬 시각효과 (스프라이트 줌인, 화면 흔들림)
- [ ] Phase 전환 시 화면 플래시 또는 보스 외형 변경
- [ ] 적 처치 파티클
- [ ] 데미지 숫자 팝업
- [ ] 옥상 배경 일러스트

### Tier B — 폴리싱
- [ ] N/Z 반대 획순 (명세 15-4·15-6 — 기본만 구현됨)
- [ ] 키 리바인딩 옵션
- [ ] 음량 슬라이더
- [ ] 다국어 지원
- [ ] 컷씬 텍스트 다듬기

---

## 10. Troubleshooting

### "검은 화면" / Canvas 안 보임
- 원인: UI 자식 GameObject가 RectTransform 없이 생성됨
- 해결: 이미 fix됨 (`FindOrCreateUIChild` 사용). 만약 재발 시 `Project Tools → Build Box Prototype (All)` 다시 실행.

### 보스 위치가 (0,2) 가 아님
- 원인: 기존 Player/Boss 인스턴스가 씬에 남아 PlacePrefab이 위치 갱신 안 함 (해결 완료)
- 검증: `git diff Assets/Project/Scenes/Boss01_Elevator.unity` 로 LocalPosition 확인

### 보스가 공격 안 받음
- 원인: 보스 GridOccupant가 occupant 사전에 등록되지 않음 (`blocksMovement=false`)
- 해결: `PlayerCombat.cs`가 `cachedBossCtrl.BossCell` 별도 검사 (이미 구현)

### 플레이어 공격 시각화 안 보임
- 확인: Player.prefab → PlayerCombat → `Hitbox Flash Prefab` 필드에 `AttackHitbox.prefab` 와이어
- 누락 시: BoxPrototypeBuilder 재실행 (자동 와이어링)

### 패턴 좌표가 이상함
- BossPatternCaster는 offset 좌표 (-3..3) 사용. PatternOrigin이 world (0,0)에 있어야 함.
- 검증: Boss01 씬 → `00_System / PatternOrigin` Transform = (0,0,0)

### 임포트 자산 적용 안 됨
- 원인: `_Imported` 폴더에 원본 PNG 누락 (zip 압축 해제 안 됨)
- 해결: `Assets/Evelyn Sprite.zip` 등을 다시 두고 Unity 재시작 → AutoRun 트리거 또는 메뉴 직접 실행

---

## 부록 — 코드 핵심 파일

| 파일 | 역할 |
|------|------|
| `Scripts/Boss/ElevatorBossController.cs` | 보스 전투 로직 전체 (Phase, 패턴, Vulnerable) |
| `Scripts/Boss/BossPatternCaster.cs` | 셀 좌표 산출 + 경고→피해 캐스팅 |
| `Scripts/Boss/BossHealth.cs` | 보스 HP/사망 |
| `Scripts/Player/PlayerCombat.cs` | 좌클릭 공격 + 히트박스 시각화 |
| `Scripts/Player/PlayerDash.cs` | 우클릭 대시 |
| `Scripts/Player/PlayerController.cs` | WASD 이동 |
| `Scripts/Grid/GridManager.cs` | 셀↔월드 변환, 점유자 사전 |
| `Scripts/Editor/BoxPrototypeBuilder.cs` | 6 씬 자동 생성 |
| `Scripts/Editor/PlaceholderSpriteCreator.cs` | placeholder 색상 PNG 생성·와이어 |
| `Scripts/Editor/ImportedAssetApplier.cs` | 외부 zip 자산 적용 |
| `Scripts/Editor/AutoRunPrototypeBuilder.cs` | 트리거 파일 감시 (외부 빌드) |
