# 발표 영상 6편 재녹화 핸드오프 (Windows → macOS)

작성: 2026-06-11, Windows 세션 (Claude Code). 이 문서 하나로 클론 직후 바로 작업을 시작할 수 있도록 진단 결과·프로브 데이터·절차를 전부 담았다.

## 0. 목표

발표용 플레이 영상 **6편**을 새로 녹화·절단·배치한다. 학습 진행 내러티브 순서:

| 번호 | 내용 | 결과 조건 |
|---|---|---|
| 1 | 회피도 할 줄 모르는 초기 정책 | player_dead, 짧은 생존, 경고타일에서 무력하게 피격 |
| 2 | 회피는 할 줄 아는 정책 (와리가리, 공격 거의 없음) | player_dead, 회피성공 다수, 보스피해 한 자릿수 |
| 3 | 플레이는 가능하지만 클리어 실패 | player_dead, 보스피해 ~18-25, 페이즈2 진입 |
| 3-1 | 클리어 실패지만 **패턴을 다 보는** 영상 | player_dead, 보스피해 ≥36, **페이즈2 4칸쓸기 + 페이즈3 마크대쉬(real/fake) 노출** |
| 4 | 클리어 성공 (인간처럼, 페이즈별 패턴 다 노출) | boss_dead, mark_real ≥ 1, sweep 높음, 회피 동작 보임 |
| 5 | 최단 클리어 | boss_dead, 해당 캡처 런 내 최단 survival |

**출력 스펙 (사용자 지시)**: 전 편 **1920x1080 통일**. **배속/감속 보정 금지(원속도 그대로)** — 편집은 사용자가 직접 한다. 무음, H.264 CRF 18, 캡처 fps 유지(60fps 권장). 구 영상 삭제는 사용자가 이미 승인했다.

## 1. 직전 시도(2026-06-11, Windows)가 실패한 원인 — 반드시 피할 것

1. **선정과 녹화가 다른 런이었다 (치명적)**. 에피소드 후보 선정은 `BossPPO_1M_VideoInfer_v2` 런 로그로 해놓고, 실제 화면 녹화는 새로 실행한 별개 런에서 했다. 에피소드는 확률적이라 같은 "E04"여도 내용이 완전히 다르다 (선정된 E04: 74.8s/sweep 344/mark 2+2 ↔ 녹화된 E04: 46.2s/sweep 45/mark 0+0). 그래서 최종 영상에 마크대쉬·4칸쓸기가 아예 없었다.
   → **선정은 반드시 "그 캡처 런 자체의 Player 로그"로 한다.**
2. **컷 시작점을 수동 fudge(+1.5s 등)로 보정**해서 앞에 이전 에피소드 꼬리가 겹쳐 들어갔다.
   → 컷 경계는 로그의 벽시계 마커 + **montage 육안 캘리브레이션**으로 잡고, 컷마다 시작/중간/끝 프레임을 추출해 검증한다.
3. 4·5번은 2.7배속 원본을 setpts/minterpolate로 1x 감속 변환했는데, 보정 오차가 2.7배로 증폭되고 프레임 보간 잔상도 생겼다.
   → 이번엔 **변환 없이 원속도 컷**만 한다.

증거: `reference/old_selection_report.md`(잘못된 선정 기록), `reference/old_finalize_manifest.json`(잘못 컷된 영상의 실측치 — 선정표와 비교하면 불일치가 보인다).

## 2. 핵심 발견 (이 매핑이 이 문서의 본체다)

### 2-1. 추론 경로에 따라 같은 체크포인트가 완전히 다르게 행동한다

- **embedded 경로**: Eval210 빌드에 ONNX를 내장(Sentis 추론, 실시간 1x). 정책이 **훨씬 무력해진다**. 1M조차 13~27초 만에 전부 사망(`results/VideoVerify_Isolated_PPO1M_5eps_20260611`). FR995는 진짜 "회피도 못 하는" 초기 모습.
- **trainer 경로**: `mlagents-learn --inference --initialize-from <run>` (torch 추론). 정책 본래 성능이 나온다. FR995조차 마스크+샘플링 덕에 회피를 곧잘 한다(= 1번용으로 부적합).
- trainer 경로는 `--time-scale 1`을 줘도 **실측 ~2.7배속**으로 돈다. `survival=`은 게임타임, `[BossRLReset] ... time=`은 벽시계. 신경 쓸 것 없음 — 원속도 컷이므로.

### 2-2. 단계 → 체크포인트 × 경로 확정 매트릭스

| 영상 | 체크포인트 | 경로 | 에피소드 수 | 선정 기준 |
|---|---|---|---|---|
| 1 | FR995 (995 step) | **embedded** (Eval210) | 10~12 | 짧은 무력사(10~25s), 공격·회피 거의 없음 |
| 2 | 99K | **embedded** (Eval210) | 10~12 | 생존 40~73s, 회피성공 ≥15, 보스피해 ≤12 |
| 3 | 99K | **trainer** | 8~10 | 전 에피소드가 3번 프로필(아래 표). 생존 적당하고 공격 활발한 것 |
| 3-1 | **499K** | **trainer** | 8~10 | **전 에피소드가 3-1 프로필!** mark_real ≥ 2 & sweep ≥ 1100 & 사망 |
| 4 | 1M | **trainer** | **45** | boss_dead & mark_real ≥ 1 & sweep ≥ 150 & 회피 보임 (~10-20% 확률이라 45ep 필요) |
| 5 | 1M (4와 같은 런) | **trainer** | (같은 런) | boss_dead 중 최단 survival |

백업: `checkpoints/ppo999k`(999K)는 ~93% 클리어 / ~7%는 보스 HP 6까지 가서 사망 — 499K 영상이 화면상 마음에 안 들 때의 3-1 대안.

### 2-3. trainer 경로 프로브 실측 (headless, 이 패키지의 `probe_logs/`에 원본 로그 있음)

**FR995-trainer** (1번용으로 부적합 — 회피를 너무 잘함):

```
E01 63.3s dmg7 esc28/1 | E02 42.9s dmg5 | E03 62.3s dmg7 | E05 17.5s dmg2 | ... 전부 player_dead
```

**99K-trainer = 3번 프로필** (10/10 사망, 페이즈2 깊숙이, 마크는 못 봄):

```
ep  reason       surv    dmg hpL  esc    sweep mark
E01 player_dead  126.4s  21  39   43/2   1172  0+0
E03 player_dead  141.9s  23  37   55/2   1317  0+0
E05 player_dead  185.5s  24  36   71/2   1678  0+0
E10 player_dead   42.9s   5  55   15/2    397  0+0   ← 짧은 예외
(나머지도 103~158s, dmg 18~25, mark 0+0)
```

**499K-trainer = 3-1 프로필 확정** (10/10 사망인데 마크대쉬까지 노출):

```
ep  reason       surv    dmg hpL  esc    sweep mark(r+f)
E01 player_dead  151.9s  36  24   50/1   1281  0+2
E03 player_dead  141.9s  36  24   38/0   1175  2+2
E07 player_dead  133.6s  40  20   39/1   1065  3+2
E08 player_dead  152.5s  41  19   48/0   1291  4+4   ← 최다 마크
E10 player_dead  185.5s  43  17   53/0   1489  4+1   ← 최장+최다피해
(10ep 중 9ep에서 mark 노출, 보스 HP 17~24까지)
```

**999K-trainer** (백업): 14ep 중 13 클리어, E05만 사망(54.2s, dmg 54, hpL 6, mark 2+2, sweep 170). 클리어 에피소드 중에도 mark 노출형 존재(E06: 75.6s sweep 345 mark 0+4, E07: 62.3s mark 1+3).

**1M-trainer** (기존 `BossPPO_1M_VideoInfer_v2` 30ep 분포): 전부 boss_dead. mark 노출형 ~3/30 (E04 74.8s sweep 344 mark 2+2 / E05 mark 0+4 / E06 mark 3+1). 최단형은 40~44s에 sweep ~22, mark 0 — 그래서 4번용 45ep 캡처가 필요하다.

**embedded 경로 분포**는 `reference/old_embedded_candidate_report.md` 참조 (FR995: 5~44s 무력사 / 99K: 10~73s 회피만 dmg≤12 / 499K: 25~68s dmg 6~9).

### 2-4. 게임 지식 (선정 판단용)

- 보스 HP **60** (= dmg_dealt 59~60에 사망 직전/사망). `survival`은 게임타임 초.
- 페이즈2 = 4칸쓸기(sweep): 로그 `phase2_sweep_history_active_steps`. 페이즈3 = 마크대쉬(MarkATK): `markatk_real_spawn_count` / `markatk_fake_spawn_count`. 보스 HP가 ~24 이하로 내려가야 마크가 나온다 (실측: hpL 17~24 도달 에피소드에서 마크 관측).
- 에피소드 경계 마커(벽시계): `[BossRLReset] queue_reload ... time=X`(에피소드 종료 순간), `[BossRLReset] after_scene_loaded ... time=Y`(다음 에피소드 화면 시작). `tools/parse_episodes.py`가 다 파싱한다.

## 3. macOS 작업 절차

### 3-0. 사전 준비

1. Unity **6000.3.10f1** + **Mac Build Support (Mono)** 모듈.
2. Python **3.10** venv: `pip install mlagents==1.1.0 imageio-ffmpeg` (torch는 의존성으로 설치됨; Apple Silicon OK).
3. ffmpeg (`brew install ffmpeg` 또는 imageio-ffmpeg 동봉 바이너리 — tools가 자동 탐색).
4. **시스템 설정 → 개인정보 보호 → 화면 기록**: 터미널(또는 Claude Code 실행 앱)에 권한 부여. 안 하면 캡처가 검은 화면이 된다.
5. 캡처 중 **집중 모드(방해금지) ON**, `caffeinate -dis &`로 슬립 방지.
6. 체크포인트 스테이징 (trainer `--initialize-from`용):
   ```bash
   for name in ppo99k:BossPlayer-99957 ppo499k:BossPlayer-499996 ppo999k:BossPlayer-999873 ppo1m:BossPlayer-1000001; do
     dir=results/VideoStage_${name%%:*}/BossPlayer
     mkdir -p "$dir"
     cp handoff_video_retake/checkpoints/${name%%:*}/${name##*:}.pt "$dir/checkpoint.pt"
   done
   ```

### 3-1. 빌드 2종 (macOS 타깃)

에디터 빌드 스크립트 2곳이 Windows 타깃 하드코딩이라 **macOS 분기 추가가 필요**하다 (빌드 인프라 수정은 허용 범위):

- `unity_project/Assets/Project/Scripts/Editor/RLTrainBatchBuild.cs:63` — `BuildTarget.StandaloneWindows64` → macOS면 `BuildTarget.StandaloneOSX`, 출력 경로 `builds/macos/BossPPO_RLTrain.app`
- `unity_project/Assets/Project/Scripts/Editor/RLEval210BatchBuild.cs:194` — 동일 (출력 `builds/macos/BossPPO_RLTrain_Eval210.app`)

빌드 커맨드 (배치모드, Unity 앱 경로는 설치 위치에 맞게):

```bash
"/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -quit -projectPath "$PWD/unity_project" \
  -executeMethod RLTrainBatchBuild.Build -logFile logs/build_rltrain_mac.log
```

Eval210 임베디드 빌드는 모델 인자를 받는다 (1번·2번용으로 **2회**: FR995, 99K):

```bash
... -executeMethod RLEval210BatchBuild.Build \
  -evalModelSource "$PWD/handoff_video_retake/checkpoints/fr995/BossPlayer-995.onnx" ...
```

(`.onnx.data` 외부 가중치 파일은 빌드 스크립트가 같은 폴더에서 자동으로 복사한다 — onnx와 같은 디렉터리에 두기만 하면 됨.)

`ProjectSettings.asset`의 `runInBackground: 1`이 커밋되어 있다 — 캡처 중 포커스를 잃어도 게임이 멈추지 않는다(직전 세션에서 0이라 trainer가 굶어 죽는 사고가 있었음).

### 3-2. 캡처 실행

**창 크기**: 맥북 내장 화면(논리 해상도 ~1512x982 등)에는 논리 1920x1080 창이 안 들어간다. **게임 창을 1280x720으로** 띄우고, Retina 픽셀 기준(2x면 2560x1440)으로 크롭한 뒤 컷 단계에서 1920x1080으로 스케일하면 깔끔하다. 외장 모니터가 있으면 1920x1080 창을 그대로 쓰면 된다.

**trainer 경로 캡처** (3, 3-1, 4+5번):

```bash
# 1) 화면 녹화 시작 (전체 화면; 장치 인덱스는 -list_devices로 확인)
ffmpeg -f avfoundation -capture_cursor 0 -framerate 60 -i "1:none" \
  -vf "format=yuv420p" -c:v libx264 -preset veryfast -crf 18 \
  -color_range tv -colorspace bt709 -color_primaries bt709 -color_trc bt709 \
  raw/capture_3_99k.mkv &
FFPID=$!
# 2) trainer-inference 실행 (게임 창이 뜸)
mlagents-learn ml-agents-config/boss_ppo_target_gate_markatk_sweep_1m_c84b1a4.yaml \
  --run-id VideoRetakeMac_03_99K --initialize-from VideoStage_ppo99k --inference \
  --env builds/macos/BossPPO_RLTrain.app --num-envs 1 --time-scale 1 \
  --width 1280 --height 720 --quality-level 5 \
  --target-frame-rate 60 --capture-frame-rate 0 --timeout-wait 60 \
  --env-args -popupwindow -screen-fullscreen 0 &
# 3) results/<run-id>/run_logs/Player-0.log 의 "[BossRL] EPISODE_END" 개수를 폴링,
#    목표 도달하면 trainer 프로세스 트리 kill → ffmpeg에 'q' 전송(또는 SIGINT)
```

대안 녹화: `screencapture -v -R<x,y,w,h> out.mov` (영역 지정 녹화, fps 고정 불가). ffmpeg avfoundation을 권장.

**embedded 경로 캡처** (1, 2번): Eval210.app을 해당 ONNX로 빌드한 뒤 직접 실행한다. trainer 불필요:

```bash
open -W는 쓰지 말고 직접: builds/macos/BossPPO_RLTrain_Eval210.app/Contents/MacOS/* \
  -popupwindow -screen-width 1280 -screen-height 720 -logFile results/VideoRetakeMac_01_FR995/run_logs/Player-0.log &
```

에피소드 카운트 폴링·종료 처리는 동일. **반드시 메타 json을 함께 기록**할 것 — tools가 이 계약을 따른다:

```json
{ "capture_name": "...", "run_id": "...", "recording_frame_rate": 60,
  "game_offset_seconds": <게임프로세스시작 - ffmpeg시작, 초>,
  "player_log": "<절대경로>", "raw_video": "<절대경로>" }
```

(`game_offset_seconds`는 두 프로세스 시작시각 차이. 어차피 montage 캘리브레이션으로 잔차를 잡으므로 ±1초 정확도면 충분하다.)

Windows에서 쓰던 캡처 스크립트 원본(`tools/capture_run.ps1`, `tools/capture_run_embedded.ps1`, `tools/run_all_captures.ps1`, `tools/probe_run.ps1`)을 참고해 zsh로 옮기면 된다 — 에피소드 폴링/정리/메타 기록 로직이 다 들어 있다.

**캡처 시간 예산** (trainer 2.7x 기준): 99K·499K 에피소드는 게임타임 2~3분 = 벽시계 45~70초/ep → 10ep ≈ 8~12분. 1M 45ep ≈ 15~20분. 1·2번(실시간)은 10ep ≈ 5~10분.

### 3-3. 선정 → 컷 → 검증

```bash
# 1) 캡처 런 "자체" 로그로 에피소드 표 출력
python handoff_video_retake/tools/parse_episodes.py results/VideoRetakeMac_31_499K/run_logs/Player-0.log

# 2) 캘리브레이션: E01의 wall_end(=queue_reload) 부근을 몽타주로 보고
#    실제 리셋 프레임과 예측치의 차이(calib)를 구한다 (캡처당 1회, 상수)
python handoff_video_retake/tools/montage.py raw/capture_31_499k.mkv <예측초-3> 6 check.png --step 0.25

# 3) 컷 (원속도, 1920x1080, 검증 프레임 동시 추출)
python handoff_video_retake/tools/cut_episode.py raw/capture_31_499k.json 8 videos/03-1_fail_all_patterns.mp4 \
  --calib <상수> --crop 2560:1440:0:52 --frames
```

**검증 체크리스트 (편당)**:
- start 프레임: 보스 HP 풀, 플레이어 스폰 위치 (이전 에피소드 잔상 금지)
- end 프레임: 사망/보스킬 장면 (다음 에피소드 혼입 금지)
- 3-1·4번: 로그의 마크 시점 부근 프레임에서 **마크대쉬가 실제로 화면에 보이는지** 확인 (`montage.py`로 해당 구간 추출)
- 전 편 해상도 1920x1080, fps 일정, 재생 끝까지 디코딩 OK

### 3-4. 산출물 배치

- `videos/01_no_dodge.mp4`, `02_dodge_only.mp4`, `03_play_but_fail.mp4`, `03-1_fail_all_patterns.mp4`, `04_clear_full_patterns.mp4`, `05_fastest_clear.mp4`
- 기존 `videos/01~05*.mp4`와 `videos/candidates_20260611/`은 삭제 (사용자 승인 완료)
- `videos/metadata/`에 편당 선정 근거(런 id, 에피소드 번호, 지표) 기록 + README의 영상 목록 갱신
- PPT(`presentations/`)는 건드리지 않는다 — 영상만

## 4. 절대 규칙 (프로젝트 공통)

- `CODE-BLUE` 원본 폴더/원격(`LxNx-Hn/CODE-BLUE`) 절대 수정 금지. 작업 repo는 `LxNx-Hn/AI_FinalTerm`만.
- 보스 난이도 하향 금지, `BossPatternCaster`/`DamageTile`/`GridMover` 핵심 로직 수정 금지. (빌드 스크립트의 macOS 타깃 추가는 게임 로직이 아니므로 허용.)
- 학습(트레이닝) 재실행 금지 — 이 작업은 전부 inference다.
- `git push`는 사용자 승인 후에만. commit은 자율 허용.
- 영상에 배속/감속/보간 등 시간축 변형 금지 (사용자가 직접 편집).

## 5. 이 패키지 구성

```
handoff_video_retake/
  HANDOFF.md                  ← 이 문서
  PROMPT.md                   ← 맥 Claude Code에 붙여넣는 시작 프롬프트
  checkpoints/                ← 필요한 체크포인트 전부 (git 추적; results/는 gitignore라 여기 복사)
    fr995/BossPlayer-995.{pt,onnx,onnx.data}      (1번, embedded용 onnx 포함)
    ppo99k/BossPlayer-99957.{pt,onnx,onnx.data}   (2번 embedded + 3번 trainer)
    ppo499k/BossPlayer-499996.{pt,onnx,onnx.data} (3-1번 trainer)
    ppo999k/BossPlayer-999873.pt                  (3-1 백업)
    ppo1m/BossPlayer-1000001.pt                   (4·5번 trainer)
  tools/
    parse_episodes.py / montage.py / cut_episode.py / ffmpeg_util.py  (크로스플랫폼)
    capture_run.ps1 / capture_run_embedded.ps1 / run_all_captures.ps1 / probe_run.ps1  (Windows 참고용)
  probe_logs/                 ← trainer 경로 프로브 원본 Player 로그 4종 (FR995/99K/499K/999K)
  reference/                  ← 직전 실패의 증거 (선정보고/매니페스트/embedded 후보표)
```
