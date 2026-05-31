# Editor Smoke Stable Gate

## 목적

컴파일/domain reload가 끝나기 전에 trainer나 Play Mode에 진입하면 보스와 플레이어가
둘 다 멈출 수 있습니다. 이 문서는 그 진입을 막기 위한 운영 절차입니다.

이 절차는 코드가 아닌 운영 체크리스트입니다.
EditorApplication.isPlaying 자동화 스크립트를 생성하지 않습니다.

---

## Gate 절차 (필수)

### 1단계: 코드 수정 완료 확인

- Unity Editor를 열어 둔 상태에서 C# 파일을 저장한다.
- Unity 하단 상태 표시줄에서 컴파일 진행 표시(스피너)가 사라질 때까지 기다린다.
- Console에 compile error가 없는지 확인한다.

### 2단계: MCP editor state 3회 연속 확인

MCP `get_unity_editor_state`를 총 3회 호출한다. 각 호출 사이에 **최소 5초** 대기한다.

**3회 모두** 아래 조건을 만족해야 한다:

| 항목                  | 요구 값                              |
|-----------------------|--------------------------------------|
| `is_compiling`        | `false`                              |
| `is_changing`         | `false`                              |
| `is_playing`          | `false`                              |
| compile error         | 없음                                 |
| `active_scene`        | `Boss01_Elevator_RLTrain`            |
| scene validate        | 이상 없음                            |

하나라도 실패하면 추가로 10초 기다린 뒤 3회 확인을 다시 시작한다.

### 3단계: 최종 대기

3회 확인 모두 통과한 뒤 **5초** 더 대기한다.

### 4단계: trainer 실행

PowerShell에서 `run_editor_smoke.ps1`을 실행한다.

```powershell
.\scripts\run_editor_smoke.ps1
```

또는 특정 run-id 지정:

```powershell
.\scripts\run_editor_smoke.ps1 -RunId BossPPO_Smoke_001
```

trainer가 시작되고 로그에 아래 메시지가 출력될 때까지 기다린다:

```
Listening on port 5004. Start training by pressing the Play button...
```

### 5단계: Play Mode 진입

trainer가 Unity 연결 대기 상태가 된 것을 확인한 뒤 Unity Editor에서 Play 버튼을 누른다.

### 6단계: communicator 확인

Play 후 30초 이내에 trainer 로그에 아래 중 하나가 나타나야 한다:

```
Connected to Unity environment
```

이 메시지가 없거나 아래 오류가 나타나면 **즉시 중단**:

```
UnityEnvironmentException: A timeout was hit while waiting for the Unity environment
Communicator Error
Academy.InitializeEnvironment NullReferenceException
```

---

## 금지 사항

- 컴파일 중 Play 버튼 클릭 금지
- trainer 실행 전 Play 버튼 클릭 금지 (communicator 연결 없이 Play하면 Heuristic 모드로 실행됨)
- domain reload 중 Unity Editor 창 전환 반복 금지
- Console에 붉은 compile error가 있는 상태에서 trainer 실행 금지

---

## 알려진 위험 요소

### Academy.InitializeEnvironment NullReferenceException

씬 리로드(episode reset) 시 ML-Agents Academy와 scene 오브젝트 간 타이밍 race condition으로
발생할 수 있습니다. Unity 내부 이슈이므로 코드로 완전히 해결되지 않습니다.

완화 방법:
- 위 gate 절차를 따라 compile-stable 상태에서만 Play 진입
- 연속으로 발생하면 Play를 중지하고 trainer를 재시작한 뒤 gate 절차를 다시 수행

### skipIntroForTraining 미설정

`Boss_Elevator` 오브젝트에 `skipIntroForTraining = false`이면 intro cutscene이 실행되고
`CutsceneFreezeManager.Lock()`으로 `Time.timeScale = 0`이 됩니다.
결과적으로 보스와 플레이어가 둘 다 멈추는 현상이 발생합니다.

확인 방법: Unity Editor에서 `Boss_Elevator` 선택 → Inspector에서 `Skip Intro For Training` 체크 여부 확인.

---

## 참조 문서

- [MANUAL_UNITY_SETUP_CHECKLIST.md](MANUAL_UNITY_SETUP_CHECKLIST.md)
- [SCENE_CLEANUP_RLTRAIN.md](SCENE_CLEANUP_RLTRAIN.md)
