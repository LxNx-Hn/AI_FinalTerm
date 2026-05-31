# DO NOT TOUCH CODE-BLUE

## 1. 절대 보호 대상

다음 폴더는 사용자 원본 작업 폴더다.

```text
C:\Users\KiKi\CODE-BLUE
```

이 폴더에는 사용자의 기존 수정사항이 있을 수 있다.

절대 건드리지 않는다.

## 2. 금지 명령

이 폴더 또는 GitHub `LxNx-Hn/CODE-BLUE` 원격에 대해 다음 명령을 실행하지 않는다.

```powershell
git pull
git push
git push --force
git push --force-with-lease
git reset --hard
git clean -fd
git checkout .
git rebase
git merge
git commit
```

## 3. 금지 이유

이전 Gemini/Antigravity 세션에서 `CODE-BLUE-RL` 복사본에 `.git`이 포함되어 원본 `CODE-BLUE.git`으로 오염 커밋이 push되는 사고가 발생했다.

따라서 Codex는 다음을 우선 확인해야 한다.

```powershell
git remote -v
git rev-parse --show-toplevel
```

단, 이 확인도 작업 폴더가 원본 CODE-BLUE가 아닌지 먼저 확인한 뒤 수행한다.

## 4. 허용되는 확인

원격 상태는 아래 읽기 전용 명령으로만 확인한다.

```powershell
git ls-remote https://github.com/LxNx-Hn/CODE-BLUE.git refs/heads/main
```

이 명령은 로컬 원본 폴더를 수정하지 않는다.

## 5. 새 작업 폴더 원칙

새 RL 작업 폴더는 원본 CODE-BLUE와 분리한다.

복사 금지:

```text
.git
Library
Temp
Logs
obj
Builds
```

복사 가능:

```text
Assets
Packages
ProjectSettings
UserSettings
```

복사 후 반드시 `.git`이 없는지 확인한다.

## 6. 원격 push 금지

Codex는 `LxNx-Hn/CODE-BLUE`에 push하지 않는다.

RL 실험 코드는 다음 중 하나에만 저장한다.

* AI_FinalTerm repo
* 새 RL 전용 repo
* 로컬 작업 폴더

단, 원격 push는 사용자 명시 승인 후에만 수행한다.

## 7. 핵심 문장

CODE-BLUE는 팀 프로젝트 원본이다.
RL 실험 대상이지, RL 실험 repo가 아니다.
