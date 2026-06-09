# PPO 1M 클리어 최종 보고서

## 1 게임 소개

- 게임명: CODE BLUE
- 장르: 2D 격자 기반 액션 보스전
- 전투 방식: 경고 타일 회피 후 근접 공격
- 이동 구조: 7x7 격자 기반 이동
- 플레이어 목표: 보스 HP 60 감소 및 보스 처치
- 플레이어 입력
- 이동: none, up, down, left, right
- 공격: no attack, attack
- 강화학습 적용 목적
- 사람과 동일한 관측 및 조작 조건에서 보스전을 클리어하는 에이전트 학습
- 사용 기준: visible observation, action spec [5,2], PPO-only

## 2 보스 소개

- 보스전 구조: 단일 보스 HP 기반 페이즈 전환
- 보스 HP: 60
- 페이즈 전환 기준
- Phase 2: HP 42 이하
- Phase 3: HP 24 이하
- Final: HP 6 이하
- 기본 공격
- 인접 시 플레이어 방향 회전
- 경고 타일 생성
- 이후 타격
- 보스 패턴
- 1페이즈 X자 패턴
- 1페이즈 NZ 패턴 및 역순 패턴
- 1페이즈 사각형 2칸 돌진 패턴
- 1페이즈 십자 돌진 패턴
- 2페이즈 시계방향 4줄 돌진 패턴
- 3페이즈 표식 돌진 패턴
- 최종 찍고 돌진 4회 패턴
- 학습 난점
- 짧은 목숨
- 강한 피격 패널티
- 회피 후 공격권 회복 필요
- 패턴 중 현재 보스 위치 타격 필요

## 3 마르코프 결정 과정

- 플레이어 MDP
- Player State
- 플레이어 위치
- 플레이어 방향
- 플레이어 체력
- 공격 가능 범위
- 주변 warning tile
- 주변 damage tile
- 최근 위험 타일 정보
- 보스 상대 위치
- 보스 visible 여부
- Player Action
- Branch 0 move
- none
- up
- down
- left
- right
- Branch 1 attack
- no attack
- attack
- Player Transition
- 플레이어 이동
- 플레이어 공격
- 피격 및 사망
- 불가능 이동은 action mask로 차단
- hidden target 공격은 action mask로 차단
- 보스 MDP
- Boss State
- 보스 위치
- 보스 방향
- 보스 HP
- 현재 phase
- 현재 pattern state
- warning tile
- damage tile
- MarkATK real visible cue
- MarkATK fake visible cue
- Phase2 sweep history
- Boss Transition
- 보스 패턴 진행
- warning tile에서 damage tile로 전환
- HP 기준 phase 전환
- dash 중 현재 보스 위치 갱신
- MarkATK real fake cue 생성 및 소멸
- 보스 HP 감소
- 통합 Reward
- 보스 HP 감소 보상
- 보스 처치 보상
- 피격 패널티
- 사망 패널티
- 위험 타일 패널티
- 공격 시도 보상 제거
- 실제 적중 기반 보상
- Episode termination
- 보스 사망
- 플레이어 사망
- 제한 시간 도달

## 4 보상 설계

- 초기 보상 설계
- 공격 시도 보상 존재
- 보스 HP 감소 보상 존재
- 피격 및 사망 패널티 존재
- 위험 타일 패널티 존재
- 문제점
- 생존 중심 local optimum
- 공격 시도 보상 farming 가능성
- 이동 중 공격 miss 증가
- 수정된 보상 설계
- 공격 시도 자체 보상 제거
- 보스 HP 감소 시 보상
- 성공 hit 기반 보상
- hidden target 및 empty-space hit 차단
- player hit 및 death penalty 유지
- 현재 main 기준
- BossDamagePerHp: +0.10
- SuccessfulHitBonusReward: +0.08
- BossKillReward: +5.0
- PlayerHitPenalty: -2.0
- PlayerDeathPenalty: -8.0
- WarningTilePenalty: -0.10
- DamageTilePenalty: -0.20
- fast-clear reward는 이번 main에는 미적용
- 향후 개선 후보
- boss_dead 발생 시 remaining time 기반 fast-clear bonus

## 5 사용한 강화학습 기법

- ML-Agents PPO
- PPO 선택 이유
- discrete action 학습 가능
- policy gradient 기반 안정적 학습
- 보스전 순차 의사결정 문제에 적합
- Action masking
- 불가능 이동 차단
- hidden target 공격 차단
- off-lane 공격 차단
- stale target 공격 차단
- Observation engineering
- 기본 193차원 observation
- MarkATK real visible cue 추가
- MarkATK fake visible cue 추가
- sweep history 추가
- 최종 438차원 observation
- Long-run training
- 50K 단기 학습 한계
- 1M 장기 학습에서 local optimum 탈출

## 6 실험 환경

- Unity ML-Agents
- PPO config: ml-agents-config/boss_ppo_target_gate_markatk_sweep_1m_c84b1a4.yaml
- max steps: 1,000,000
- no-graphics headless training
- action spec: [5,2]
- observation size: 438
- 사용 run-id: BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1
- 사용 commit: 9448e2b
- checkpoint 및 ONNX export
- final ONNX: results/BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1/BossPlayer.onnx
- final checkpoint: results/BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1/BossPlayer/BossPlayer-1000001.pt
- final exported checkpoint ONNX: results/BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1/BossPlayer/BossPlayer-1000001.onnx
- TensorBoard event: results/BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1/BossPlayer/events.out.tfevents
- 학습 로그: results/BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1/run_logs/Player-0.TRAINING-1M.log

## 7 학습 결과

- 50K 결과
- 클리어 0
- 낮은 boss damage
- in_range 약 10퍼센트
- 생존 중심 행동
- 1M 결과
- boss_dead 199회
- 전체 클리어율 22.3퍼센트
- 최근 100 episode 클리어율 80퍼센트
- boss_damage avg 40.8
- boss_damage max 59
- 평균 클리어 시간 70.0초
- 최속 클리어 시간 42.6초
- 최근 20 클리어 평균 60.5초
- 최근 100 episode hit_rate 97.3퍼센트
- 최근 100 episode in_range 27.5퍼센트
- player_hit 감소
- MarkATK observed 15,309회
- 무결성
- attack_out_of_range 0
- hidden hit 0
- off-lane hit 0
- stale hit 0
- fake marker mask leak 0
- next_band leak 0
- sweep_sequence_index leak 0
- 학습 추이 PNG
- docs/rl_final/figures/boss_damage_by_step.png
- docs/rl_final/figures/clear_rate_by_step.png
- docs/rl_final/figures/reward_by_step.png
- docs/rl_final/figures/hit_rate_by_step.png
- docs/rl_final/figures/in_range_by_step.png
- docs/rl_final/figures/clear_time_distribution.png
- docs/rl_final/figures/phase_milestone_timeline.png
- docs/rl_final/figures/integrity_leak_table.png
- 그래프 생성 근거
- Player log episode summary 892개 파싱
- boss_dead 199회 파싱
- training_status checkpoint reward 사용
- 수치 재검산 근거
- 전체 hit_rate 84.7퍼센트
- 최근 100 episode 기준 hit_rate 97.3퍼센트
- 최근 100 episode 기준 in_range 27.5퍼센트

## 8 영상 자료 설명

- 영상 1 초기 정책
- 학습 초반 정책
- 낮은 boss damage
- 피격 및 사망
- 공격권 회복 실패
- 용도: 단기 PPO 실패 사례
- 파일: videos/01_initial_policy.mp4
- 형식: 99K checkpoint 실제 gameplay 녹화
- 사용 checkpoint: BossPlayer-99957.onnx
- 녹화 길이: 45초
- 최종 클립 재사용 없음
- 영상 2 중기 규칙 학습
- 499K checkpoint 실제 gameplay
- boss_damage 3
- player_dead
- survival 25.0초
- normal_visible 2회 dash_current_overlap 1회
- 용도: 중기 규칙 학습과 부분 hit 사례
- 파일: videos/02_learning_progression.mp4
- 형식: 499K checkpoint 실제 gameplay 녹화
- 설명 슬라이드 사용 없음
- 영상 3 중기 패턴 학습
- 799K checkpoint 실제 gameplay 본캡처
- boss_damage 8
- player_dead
- survival 69.5초
- phase2 sweep history active steps 653
- normal_visible hit 8회
- 용도: 중기 패턴 회피와 공격권 회복 진행 사례
- 파일: videos/03_mid_pattern_learning.mp4
- 로그: results/VideoGameplay_PPO799K_mid_pattern_final_capture/run_logs/Player-0.log
- 영상 4 후기 2 3 페이즈 클리어
- 발표 핵심 영상 후보
- Phase2 Phase3 Final 포함
- visible target 기반 정상 타격
- leak 0 근거와 함께 사용
- 파일: videos/04_late_phase23_clear.mp4
- 영상 5 후기 꼼수성 클리어 후보
- root_visible_overlap
- dash_current_overlap
- warning edge 운영
- 패턴 스킵처럼 보이는 빠른 클리어 후보
- exploit 확정 표현 금지
- 파일: videos/05_late_clever_clear.mp4

## 9 LTS 브랜치와 main 비교

| 항목 | LTS | main |
|---|---|---|
| action spec | [6] | [5,2] |
| observation | 193 | 438 |
| reward | 강한 damage reward | hit-gated reward |
| kill reward | 강한 kill reward | boss_dead +5 |
| fast-clear reward | 존재 | 미적용 |
| 구조 | 단순 구조 | visible cue 충실도 높음 |
| exploit 차단 | 기본 수준 | hidden off-lane stale 차단 |
| 입력 조건 | 단일 discrete 입력 | 사람 입력 조건에 더 가까움 |

- 결론
- LTS는 빠른 클리어 중심 레시피
- main은 사람 조건 충실도 중심 레시피
- main도 1M에서 PPO-only 클리어 성공

## 10 결론 및 향후 개선

- PPO-only는 불가능하지 않음
- 50K 단기 학습에서는 생존 중심 local optimum 발생
- 1M long-run에서 클린 클리어 정책 학습 성공
- 현재 main은 사람과 동일한 입력 조건 및 visible observation 기반 성공
- 최근 100 episode 기준 클리어율 80퍼센트
- 전체 run 기준 boss_dead 199회
- 무결성 leak 0
- 향후 fast-clear reward 추가 가능
- BC/demo는 현재 필수 아님
- fast-clear reward는 별도 브랜치에서 실험 권장

## 근거 파일

- config: ml-agents-config/boss_ppo_target_gate_markatk_sweep_1m_c84b1a4.yaml
- observation: unity_project/Assets/Project/Scripts/RL/BossRLStateExtractor.cs
- reward: unity_project/Assets/Project/Scripts/RL/BossRLReward.cs
- scene action spec: unity_project/Assets/Project/Scenes/Boss01_Elevator_RLTrain.unity
- final result: results/BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1
- final status: results/BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1/run_logs/training_status.json
- final log: results/BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1/run_logs/Player-0.TRAINING-1M.log
- figure summary: docs/rl_final/figures/figure_generation_summary.json
- figure script: scripts/generate_rl_final_figures.py
