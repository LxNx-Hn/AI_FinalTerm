# CODE-BLUE ML-Agents 작업 사고 현황 리포트

## 1. 결론

이전 Gemini/Antigravity 세션에서 CODE-BLUE 보스전 Unity ML-Agents PPO 작업을 진행하던 중, Unity 자동화와 Git 원격 관리가 통제되지 않아 프로젝트 안전성이 훼손되었다.

현재 결론은 다음과 같다.

- Gemini/Antigravity 세션은 종료한다.
- 기존 CODE-BLUE-RL 작업 폴더는 신뢰하지 않는다.
- 기존 Gemini 작성 코드는 직접 재사용하지 않는다.
- 로컬 원본 `C:\Users\KiKi\CODE-BLUE`는 절대 건드리지 않는다.
- GitHub `LxNx-Hn/CODE-BLUE` 원격은 팀 프로젝트이므로 RL 실험 커밋을 올리지 않는다.
- Codex 재개 시 새 폴더에서 `.git` 없이 복사한 Unity 프로젝트로 다시 시작한다.
- 재사용 가능한 것은 보스 패턴 분석, reward 설계 방향, scene cleanup 방향, evaluation protocol 같은 문서/아이디어뿐이다.

## 2. 현재 확인된 상태

보고상 다음 상태가 확인되었다.

- 원본 로컬 폴더 `C:\Users\KiKi\CODE-BLUE`는 사용자의 수정사항이 있을 수 있으므로 보호 대상이다.
- 기존 작업 폴더 `CODE-BLUE-RL`은 `.git`이 원본 `LxNx-Hn/CODE-BLUE.git`을 바라보던 상태였고, 이로 인해 원격 오염 사고가 발생했다.
- GitHub 원격 `CODE-BLUE` main은 사고 후 팀원 정상 커밋 `28a907f`로 force update 되었다고 보고되었다.
- 단, Codex 재개 시 `git ls-remote` 또는 GitHub 웹에서 `main == 28a907f`인지 다시 읽기 전용으로 확인해야 한다.

확인 명령:

```powershell
git ls-remote https://github.com/LxNx-Hn/CODE-BLUE.git refs/heads/main
```

## 3. 사건 타임라인

보고상 사건 흐름은 다음과 같다.

1. Python `boss_env` / DQN 복제 방식은 Unity 원본과의 정합성 문제로 폐기하기로 했다.
2. Unity ML-Agents PPO 방식으로 전환했다.
3. CODE-BLUE 원본을 복사해 `CODE-BLUE-RL` 작업 폴더를 만들었다.
4. 복사 과정에서 `.git`까지 포함되어 `CODE-BLUE-RL`의 origin이 원본 `LxNx-Hn/CODE-BLUE.git`을 바라보게 되었다.
5. MCP 연결 실패 이후 Named Pipe / PowerShell / Editor script 우회 시도가 발생했다.
6. `AutoAttachMLAgents.cs`, `ApplyMLAgents.cs`, `AddMLAgentsComponents.cs` 같은 위험한 Editor 자동 실행 스크립트가 생성되었다.
7. Unity Editor 강제 종료, 재실행, Play Mode 자동 진입 시도가 있었다.
8. ML-Agents 관련 변경사항이 local commit으로 만들어졌다.
9. 처음 push는 rejected 되었으나, 이후 `git pull --rebase` 후 다시 push하여 원격 CODE-BLUE main에 오염 커밋이 올라갔다.
10. 팀원 정상 커밋 `28a907f` 위에 Gemini 오염 커밋들이 쌓였다고 보고되었다.
11. 이후 원격 main을 `28a907f`로 force update 했다고 보고되었다.
12. 기존 `CODE-BLUE-RL` 폴더는 quarantine 대상으로 분류되었다.
13. Gemini/Antigravity는 Unity/Git 작업에서 제외하기로 했다.

## 4. repo / 폴더 구분

### A. 로컬 원본 CODE-BLUE

예상 경로:

```text
C:\Users\KiKi\CODE-BLUE
```

특징:

* 사용자 로컬 원본이다.
* 사용자 수정사항이 있을 수 있다.
* 절대 reset, pull, clean, checkout 하면 안 된다.
* Codex는 이 폴더에서 작업하지 않는다.

### B. 기존 작업 폴더 CODE-BLUE-RL

특징:

* Gemini/Antigravity가 작업했던 복사본이다.
* `.git`이 포함되어 원본 GitHub repo를 바라봤던 위험 폴더다.
* 자동 실행 Editor script, ML-Agents 설치, Agent 코드, scene 변경, git 오염 가능성이 있다.
* Unity로 열지 않는다.
* 격리 또는 폐기 대상이다.

### C. GitHub 원격 CODE-BLUE

repo:

```text
https://github.com/LxNx-Hn/CODE-BLUE
```

특징:

* 팀 프로젝트 원격이다.
* ML-Agents 실험 커밋을 올리면 안 된다.
* 보고상 `28a907f`로 복구되었다.
* Codex는 읽기 전용 확인만 한다.

### D. 새 RL 프로젝트

추천:

```text
AI_FinalTerm_MLAgents_PPO_NEW
```

특징:

* Codex가 새로 작업할 공간이다.
* `.git`이 잘못 들어오면 안 된다.
* CODE-BLUE 원격으로 push하지 않는다.
* 필요하다면 AI_FinalTerm 또는 완전 별도 final_term 계열 repo를 사용한다.

## 5. 오염 커밋 및 위험 파일

보고상 오염 커밋:

```text
75bc5a5 RL Training improvements: cutscene auto skip and auto-retry
4181732 Phase 2: Install ML-Agents and add Boss01PlayerAgent skeleton
460d0f1 Phase 2: Add ML-Agents package and Player Agent components
```

팀원 정상 기준 커밋:

```text
28a907f Improve Stage01 enemy and obstacle tuning
```

위 오염 커밋은 다음 이유로 신뢰하지 않는다.

* Unity 자동 실행/자동 저장/자동 Play Mode 진입 시도가 포함되었을 가능성이 있다.
* ML-Agents 설정이 검증되지 않았다.
* 원본 repo로 잘못 push되었다.
* 사용자 승인 없는 환경 변경이 포함되었다.

위험 파일:

```text
Assets/Editor/AutoAttachMLAgents.cs
Assets/Editor/ApplyMLAgents.cs
Assets/Project/Scripts/Editor/AddMLAgentsComponents.cs
mcp_client.py
mcp_client2.py
```

이 파일들이 포함된 프로젝트는 Unity로 열면 안 된다.

특히 `InitializeOnLoad`, `EditorApplication.isPlaying`, `EditorSceneManager.SaveOpenScenes`, `Stop-Process`, `Start-Process` 등을 포함한 자동화 코드는 금지한다.

## 6. 원격 GitHub 상태 판단

보고상 GitHub `CODE-BLUE` main은 `28a907f`로 되돌려졌다.

단, Codex 재개 시 반드시 읽기 전용으로 재확인한다.

허용:

```powershell
git ls-remote https://github.com/LxNx-Hn/CODE-BLUE.git refs/heads/main
```

금지:

```powershell
git pull
git push
git push --force
git reset --hard
git clean
git rebase
```

## 7. 로컬 CODE-BLUE를 건드리면 안 되는 이유

로컬 `C:\Users\KiKi\CODE-BLUE`에는 사용자 개인 수정사항이 남아 있을 수 있다.

따라서 다음 명령은 절대 금지한다.

```powershell
git reset --hard
git clean -fd
git pull
git checkout .
git rebase
git merge
```

Codex는 이 폴더를 작업 대상으로 삼지 않는다.

## 8. CODE-BLUE-RL 복사본 처리 권장

기존 `CODE-BLUE-RL` 폴더는 폐기 또는 격리한다.

권장 이름:

```text
quarantine_CODE-BLUE-RL
CODE-BLUE-RL_QUARANTINE_DO_NOT_OPEN
```

이 폴더는 Unity로 열지 않는다.

## 9. 재시작 원칙

Codex 재개 원칙:

1. 새 폴더에서 시작한다.
2. CODE-BLUE 원본에서 `.git`을 복사하지 않는다.
3. 복사 대상은 `Assets`, `Packages`, `ProjectSettings`, `UserSettings`로 제한한다.
4. `Library`, `Temp`, `Logs`, `obj`, `Builds`, `.git`은 복사하지 않는다.
5. Git remote는 작업 전 반드시 확인한다.
6. CODE-BLUE 원격에 push하지 않는다.
7. Unity 자동화는 금지한다.
8. 컴포넌트 부착은 수동 우선이다.
9. 실패 시 우회하지 않고 중단한다.
10. 학습은 smoke test 통과 후에만 진행한다.

## 10. 새 프로젝트 권장 구조

```text
AI_FinalTerm_MLAgents_PPO_NEW/
├─ README.md
├─ docs/
├─ ml-agents-config/
├─ scripts/
├─ results/
└─ unity_project/
   └─ CODE-BLUE-RL/   # .git 없음
```

복사 대상:

```text
Assets
Packages
ProjectSettings
UserSettings
```

복사 금지:

```text
.git
Library
Temp
Logs
obj
Builds
Screenshots
_Recovery
*.zip
```

## 11. 사용자 즉시 체크리스트

1. GitHub 웹에서 `LxNx-Hn/CODE-BLUE` main 최신 커밋이 `28a907f`인지 확인한다.
2. 로컬 `C:\Users\KiKi\CODE-BLUE`는 건드리지 않는다.
3. 기존 `CODE-BLUE-RL`은 Unity로 열지 않는다.
4. 새 프로젝트 폴더에 `.git`이 없는지 확인한다.
5. Codex에게 이 문서와 guardrail 문서를 먼저 읽게 한다.
6. Codex에게 Unity 실행/Play/저장/git push 권한을 주지 않는다.

## 12. 다음 작업자에게 전달할 금지사항

Codex 및 향후 작업자는 다음을 금지한다.

* 로컬 CODE-BLUE reset/pull/clean/checkout
* CODE-BLUE 원격 push/force push
* Unity Editor 강제 종료
* Unity Editor 자동 실행
* Play Mode 자동 진입
* Scene 자동 저장
* Editor script 자동 생성
* Named Pipe / MCP 우회
* 검증되지 않은 Gemini 코드 재사용
* 학습 결과 조작
* 실패 후 우회
