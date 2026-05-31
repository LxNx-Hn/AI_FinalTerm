# Codex 재시작 가드레일

## 1. 목적

이 문서는 CODE-BLUE Unity ML-Agents PPO 프로젝트를 Codex로 재개할 때 반드시 지켜야 할 안전 규칙을 정의한다.

이전 Gemini/Antigravity 세션에서 Unity 자동화와 Git 원격 관리 사고가 발생했으므로, Codex는 자동 실행보다 검증과 단계적 승인을 우선한다.

## 2. 절대 보호 대상

### 로컬 원본 CODE-BLUE

예상 경로:

```text
C:\Users\KiKi\CODE-BLUE
```

이 폴더는 사용자 로컬 원본이다. 사용자의 수정사항이 있을 수 있으므로 절대 수정하지 않는다.

금지:

```powershell
git reset --hard
git clean -fd
git pull
git checkout .
git rebase
git merge
```

### GitHub 원격 CODE-BLUE

repo:

```text
https://github.com/LxNx-Hn/CODE-BLUE
```

이 repo는 팀 프로젝트 원격이다.

금지:

```powershell
git push
git push --force
git push --force-with-lease
git pull --rebase 후 push
```

Codex는 이 원격에 직접 쓰지 않는다.

## 3. Git 사용 원칙

작업 전 반드시 확인:

```powershell
git rev-parse --show-toplevel
git remote -v
git branch --show-current
git status --short
```

단, 로컬 원본 CODE-BLUE에서는 위 명령도 사용자가 허용한 경우에만 실행한다.

새 RL 프로젝트에서는 다음 조건을 만족해야 한다.

* `.git`이 없거나, 새 RL 전용 repo만 가리킨다.
* `origin`이 `LxNx-Hn/CODE-BLUE.git`이면 즉시 중단한다.
* 원격 push는 사용자 명시 승인 전 금지한다.

## 4. Unity 조작 원칙

금지:

* Unity Editor 강제 종료
* Unity Editor 백그라운드 자동 실행
* `Stop-Process`로 Unity 종료
* `Start-Process`로 Unity 자동 실행
* `EditorApplication.isPlaying = true`
* `EditorSceneManager.SaveOpenScenes()`
* `[InitializeOnLoad]` 기반 자동 실행
* Scene 자동 저장
* Component 자동 부착
* Package 자동 설치
* EXE 자동 빌드

허용:

* 파일 구조 읽기
* 문서 작성
* 코드 diff 제안
* 사용자가 직접 수행할 수동 절차 안내
* 사용자 승인 후 단일 파일 작성

## 5. MCP / Named Pipe 원칙

금지:

* Named Pipe 직접 접근
* MCP 우회 클라이언트 작성
* Python/C#으로 Unity command 강제 실행
* MCP 실패 후 우회 실행

MCP는 연결되더라도 read-only 확인부터 시작한다.

허용되는 확인:

* project path 읽기
* active scene 읽기
* hierarchy 읽기
* Inspector 값 읽기
* package manifest 읽기
* compile error 읽기

MCP가 실패하면 중단하고 원인만 보고한다.

## 6. 코드 재사용 원칙

Gemini가 작성한 코드는 직접 재사용하지 않는다.

재사용 금지:

* `Boss01PlayerAgent.cs`
* `AutoAttachMLAgents.cs`
* `ApplyMLAgents.cs`
* `AddMLAgentsComponents.cs`
* `mcp_client.py`
* `mcp_client2.py`
* 자동 실행/자동 저장/자동 Play 관련 스크립트

재사용 가능:

* 보스 패턴 분석
* reward 설계 방향
* observation/action 설계 방향
* scene cleanup 방향
* evaluation protocol

Codex는 Agent 코드를 새로 작성하고, 작은 단위로 검증한다.

## 7. 작업 순서

Codex는 다음 순서만 따른다.

1. 상태 확인
2. 문서 저장
3. 새 프로젝트에 `.git` 없음 확인
4. Unity version 확인
5. manifest/package 확인
6. 보스전 씬 존재 확인
7. 코드 작성 계획 제시
8. 사용자 승인
9. 단일 파일 작성
10. 사용자 수동 Unity 확인
11. 다음 단계 진행

## 8. 중단 조건

다음 중 하나라도 발생하면 즉시 중단한다.

* 현재 경로 불명확
* `.git`이 CODE-BLUE 원격을 바라봄
* 원본 CODE-BLUE와 작업 폴더가 혼동됨
* Unity compile error
* missing script/reference
* 자동 실행 Editor script 발견
* Codex가 작업 범위를 벗어나야 하는 상황
* 사용자 승인 없는 Git/Unity 조작이 필요해지는 상황

## 9. 보고 형식

각 단계 종료 시 다음을 보고한다.

```text
1. 수행한 작업
2. 읽은 파일
3. 생성/수정한 파일
4. 실행한 명령
5. 금지사항 위반 여부
6. 현재 위험 요소
7. 다음 단계
```

## 10. 핵심 원칙

실패하면 우회하지 않는다.
모르면 멈춘다.
자동화보다 원본 보호가 우선이다.
