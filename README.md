# CODE-BLUE Boss Speedrun RL (Clean Reconstruction)

이 프로젝트는 `CODE-BLUE` 게임의 Boss01_Elevator 전투를 한 Python RL 환경을 기반으로 강화학습 에이전트를 훈련하는 프로젝트입니다. 이전 실험의 한계를 분석하고 극복하기 위해 원본 Unity 규칙을 한 "클린 재구현" 버전입니다.

## 1. 이전 실험 결과와 재구현 사유

### 1.1 이전 모델 성능 (Tabular Q-learning 30k) [Legacy]
- **Random baseline**: 
  - boss_kill_rate: 0.0
  - death_rate: 1.0
  - avg_boss_damage_dealt: 2.85 / 60
  - no_hit_damage_during_dash_count: 9
- **Tabular Q-learning (Natural Discovery)**:
  - boss_kill_rate: 0.0
  - death_rate: 1.0
  - avg_boss_damage_dealt: 27.87 / 60
  - damage_during_dash_count: 272
  - no_hit_damage_during_dash_count: 272
  - risky_trade_count: 97
- **해석**: 전통적인 Tabular Q-learning은 상태 공간 일반화 부족으로 Full Clear에 실패.

### 1.2 이전 모델 성능 (DQN 10k) - *Discarded Legacy Result*
- **결과**:
  - boss_kill_rate: 1.0 (100%)
  - best_clear_time_seconds: 29.6
  - avg_clear_time_seconds: 29.6
  - avg_boss_damage: 60.0 / 60
  - death_rate: 0.0
- **해석 (중요)**: 위 100% 성공 결과는 보스의 무작위 패턴(Phase 2, Phase 3, Fake Dash 등)이 비활성화된 채 "결정론적인 걷고 할퀴기" 패턴만을 100번 반복한 평타 전용(basic_rep only) 환경에서의 결과입니다. 보스 패턴 커버리지가 불완전(incomplete boss pattern coverage)했으므로 **최종 결과에서 폐기(discarded)합니다.** 메인 결과로 사용하지 않습니다.

### 1.3 클린 재구현 목적 및 진짜 일반화 평가 (Full-Pattern Randomized Environment)
- Unity 원본 게임의 코드(`ElevatorBossController.cs`)는 체력(70%, 40%, 10%)에 따라 복잡한 무작위 패턴(Hollow Corner, N/Z Stroke, Sweep, Mark Dash 등)을 구사합니다.
- 이를 파이썬 `boss_env.py`에 (`boss_director.py` 도입)하고 매 에피소드마다 시드를 변경하며 Full-Pattern 진짜 일반화 평가를 메인으로 수행합니다.
- (진행 중) Random.Range / Random.value 분기를 포함한 최종 메인 평가를 준비하고 있습니다.

---

## 2. Unity 원본 규칙 확인 결과

다음은 `C:\Users\KiKi\CODE-BLUE\Assets\Project\Scripts` 에 위치한 C# 소스코드를 직접 분석하여 확인한 사실입니다.

1. **Arena 좌표계**: Grid x(-3~3), y(-4~4).
2. **플레이어 시작 위치**: (0, -3) (컷씬 후 강제 위로 1칸 이동).
3. **보스 시작 위치**: (0, 1).
4. **컷씬 후 위치 보정 여부**: 강제로 위로 1칸 올라가는 `MovePlayerOneCellForIntro` 코루틴 존재.
5. **플레이어 이동 규칙**: GridMover 기준 상하좌우 방향.
6. **이동 성공 시 facing 변경 여부**: 변경됨.
7. **이동 실패 시 facing 유지 여부**: **[수정됨]** 초기 프롬프트에는 이동 실패 시 facing 유지라고 되어 있었지만, Unity 원본 코드 분석 결과 벽/장애물/arena 끝 때문에 이동에 실패해도 입력 방향으로 플레이어 facing은 변경되는 것으로 확인되어 **Unity 원본 기준으로 수정(이동 실패 시에도 입력 방향으로 facing 갱신)**했습니다.
8. **이동+공격 동시 입력 가능 여부**: `Update` 문에서 키보드 이동 입력과 마우스 클릭 공격이 동시에 감지됨.
9. **이동+공격 동시 입력 시 처리 순서**: **[확인 필요]** Unity 자체 Script Execution Order에 의존적이나, 논리적 자연스러움을 위해 본 환경에서는 `이동 처리 -> 공격 처리` 순으로 고정하여 구현함.
10. **PlayerCombat 공격 범위**: 전방 방향 기준 깊이 1~2칸, 폭 -1~1칸 (총 6칸).
11. **공격 쿨타임**: 0.5초.
12. **공격 판정 발생 타이밍**: 입력 프레임 즉시(`TakeDamage` 호출).
13. **보스 HP**: 60 (Scene Inspector `maxHp` 기준).
14. **플레이어 HP**: 3.
15. **보스 basic chase attack**: 1칸씩 플레이어를 쫓아가며 근접 후 즉시 스크래치 시전.
16. **MoveTowardPlayerFrontCell while-loop 또는 유사 추적 로직**: 확인 완료. 보스는 `while`문 내에서 `GetPlayerOffsetCell()`을 반복 호출하며 **매 칸 이동할 때마다 플레이어의 최신 좌표를 실시간으로 다시 겨냥**함.
17. **보스 dash / PatternEntryDash / MarkDash**: 존재 확인 완료.
18. **패턴모드 진입 시 Renderer OFF 여부**: 확정됨 (`SetBossVisible(false)` 다수 포진).
19. **보스 visible state와 hurtbox/damage state 차이**: `SetBossVisible`은 렌더러만 끄며 `BossHealth` 콜라이더는 비활성화하지 않으므로, 투명 상태에서도 여전히 피격 판정은 유지됨.
20. **warning tile / damage tile / hit 판정 timing**: 장판이 먼저 깔리고 설정된 시간 뒤 정확하게 데미지가 적용됨.

---

## 3. Unity Trace Replay 가이드
훈련 완료 후 생성된 `dqn_best_action_trace.json` 파일을 통해 Unity에서 에이전트의 움직임을 직접 시연할 수 있습니다.

1. `code_blue_boss_dqn_clean/unity_demo/CODE-BLUE-RL-Demo` 폴더 내에 Unity 원본을 복사합니다.
2. `results/logs/dqn_best_action_trace.json` 파일을 `unity_demo/CODE-BLUE-RL-Demo/Assets/StreamingAssets/` 로 복사합니다.
3. Unity Scene에서 `RLTraceReplayer` 스크립트를 빈 GameObject에 부착 후, 시연을 진행합니다.
