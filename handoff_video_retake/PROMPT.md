# 맥 세션 시작 프롬프트 (아래 블록을 그대로 붙여넣기)

```
AI_FinalTerm 레포를 방금 클론했어. CODE-BLUE 보스전 PPO 발표용 플레이 영상 6편(1, 2, 3, 3-1, 4, 5)을 이 맥에서 녹화·절단·배치하는 작업이야. 직전에 Windows에서 영상을 잘못 만들어서 전부 재녹화하는 거고, 원인 진단과 절차는 이미 끝나 있어.

시작하기 전에 handoff_video_retake/HANDOFF.md 를 정독해. 단계→체크포인트→추론경로 매트릭스, 프로브 실측표, 캡처/선정/컷/검증 절차, macOS 전용 주의사항(화면기록 권한, Retina 크롭, 빌드 타깃 수정 위치)이 전부 거기 있어. 체크포인트는 handoff_video_retake/checkpoints/, 도구는 handoff_video_retake/tools/ 에 있어.

타협 불가 규칙:
1. 에피소드 선정은 반드시 "그 캡처 런 자체의 Player-0.log"로 한다. 다른 런 로그로 선정 금지 — 직전 실패의 원인.
2. 1·2번은 Eval210 임베디드 ONNX 경로(실시간), 3·3-1·4·5번은 mlagents-learn --inference trainer 경로. 같은 체크포인트도 경로에 따라 행동이 완전히 달라진다 (1M을 임베디드로 쓰면 망가짐 — 금지).
3. 3번 = 99K trainer (클리어 실패, 마크 없음), 3-1번 = 499K trainer (클리어 실패 + 마크대쉬·4칸쓸기 전부 노출, 10/10 재현 확인됨), 4번 = 1M trainer 45ep 중 boss_dead & mark_real≥1 & sweep 높은 것, 5번 = 같은 런 최단 클리어.
4. 컷은 원속도 그대로(배속/감속/보간 금지), 전 편 1920x1080 통일, 무음. 컷마다 시작/중간/끝 프레임 추출해서 육안 검증 — 이전/다음 에피소드 혼입 절대 금지. 3-1·4번은 마크대쉬가 화면에 실제로 보이는지 프레임으로 확인.
5. CODE-BLUE 원본/원격 수정 금지. 학습 재실행 금지(전부 inference). git push는 내 승인 후에만(commit은 자율).

사전 준비 확인부터 해줘: Unity 6000.3.10f1 + Mac Build Support, Python 3.10 venv + mlagents==1.1.0 + imageio-ffmpeg, ffmpeg, 터미널 화면기록 권한. 없으면 설치 안내하고 기다려. 준비가 되면 HANDOFF.md 3장 절차대로 진행해.

완료 기준: videos/ 에 01_no_dodge, 02_dodge_only, 03_play_but_fail, 03-1_fail_all_patterns, 04_clear_full_patterns, 05_fastest_clear 6편(1920x1080, 원속도) + videos/metadata/ 선정근거 + 구 영상 삭제 + README 영상 목록 갱신. 캡처 원본(mkv)과 런 로그는 지우지 말고 남겨둬.
```
