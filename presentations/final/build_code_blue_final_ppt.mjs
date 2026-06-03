import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";

const ROOT = path.resolve(path.dirname(new URL(import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, "$1")), "../..");
const OUT_DIR = path.join(ROOT, "presentations", "final");
const PREVIEW_DIR = path.join(OUT_DIR, "previews");
const PPTX_PATH = path.join(OUT_DIR, "CODE_BLUE_RL_FINAL.pptx");

function runtimeArtifactToolPath() {
  const home = process.env.USERPROFILE || process.env.HOME;
  return path.join(
    home,
    ".cache",
    "codex-runtimes",
    "codex-primary-runtime",
    "dependencies",
    "node",
    "node_modules",
    "@oai",
    "artifact-tool",
    "dist",
    "artifact_tool.mjs",
  );
}

const artifact = await import(pathToFileURL(runtimeArtifactToolPath()).href);
const { Presentation, PresentationFile } = artifact;

const W = 1280;
const H = 720;
const colors = {
  bg: "#F7F4ED",
  ink: "#1F2933",
  muted: "#667085",
  line: "#D6D0C4",
  red: "#C44536",
  blue: "#2F6FAD",
  green: "#317A55",
  gold: "#B8842C",
  charcoal: "#2B2F36",
  panel: "#FFFFFF",
};

function rect(slide, x, y, w, h, fill = "#FFFFFF00", line = "#FFFFFF00", width = 0) {
  return slide.shapes.add({
    geometry: "rect",
    position: { left: x, top: y, width: w, height: h },
    fill,
    line: { fill: line, width, style: "solid" },
  });
}

function text(slide, value, x, y, w, h, options = {}) {
  const shape = rect(slide, x, y, w, h, options.fill || "#FFFFFF00", options.line || "#FFFFFF00", options.lineWidth || 0);
  shape.text = value;
  shape.text.fontSize = options.size || 24;
  shape.text.color = options.color || colors.ink;
  shape.text.bold = Boolean(options.bold);
  shape.text.typeface = options.face || "Malgun Gothic";
  shape.text.alignment = options.align || "left";
  shape.text.verticalAlignment = options.valign || "top";
  shape.text.insets = options.insets || { left: 8, right: 8, top: 4, bottom: 4 };
  return shape;
}

function line(slide, x, y, w, color = colors.line) {
  rect(slide, x, y, w, 2, color, color, 0);
}

function bulletList(slide, items, x, y, w, options = {}) {
  const size = options.size || 24;
  const gap = options.gap || 42;
  items.forEach((item, index) => {
    const cy = y + index * gap + 9;
    rect(slide, x, cy, 10, 10, options.dot || colors.red, options.dot || colors.red, 0);
    text(slide, item, x + 22, y + index * gap, w - 22, gap, {
      size,
      color: options.color || colors.ink,
      bold: options.boldIndexes?.includes(index),
      insets: { left: 0, right: 4, top: 0, bottom: 0 },
    });
  });
}

function title(slide, kicker, claim) {
  text(slide, kicker, 54, 34, 260, 28, { size: 16, bold: true, color: colors.red });
  text(slide, claim, 54, 64, 1000, 54, { size: 34, bold: true });
  line(slide, 54, 126, 1170);
}

function footer(slide, n) {
  text(slide, "CODE BLUE PPO 1M", 54, 666, 240, 24, { size: 13, color: colors.muted });
  text(slide, String(n).padStart(2, "0"), 1164, 660, 70, 28, { size: 16, color: colors.muted, align: "right" });
}

function metric(slide, label, value, x, y, w, accent) {
  rect(slide, x, y, w, 92, colors.panel, colors.line, 1);
  text(slide, value, x + 16, y + 16, w - 32, 34, { size: 28, bold: true, color: accent });
  text(slide, label, x + 16, y + 54, w - 32, 24, { size: 15, color: colors.muted });
}

function twoColumn(slide, leftTitle, leftItems, rightTitle, rightItems) {
  text(slide, leftTitle, 70, 154, 500, 30, { size: 22, bold: true, color: colors.blue });
  bulletList(slide, leftItems, 74, 202, 500, { size: 21, gap: 38, dot: colors.blue });
  text(slide, rightTitle, 678, 154, 500, 30, { size: 22, bold: true, color: colors.red });
  bulletList(slide, rightItems, 682, 202, 500, { size: 21, gap: 38, dot: colors.red });
}

function table(slide, headers, rows, x, y, widths, rowH = 42) {
  let cx = x;
  headers.forEach((h, i) => {
    rect(slide, cx, y, widths[i], rowH, colors.charcoal, colors.charcoal, 0);
    text(slide, h, cx + 8, y + 8, widths[i] - 16, rowH - 10, { size: 17, bold: true, color: "#FFFFFF" });
    cx += widths[i];
  });
  rows.forEach((row, r) => {
    cx = x;
    row.forEach((cell, c) => {
      rect(slide, cx, y + rowH * (r + 1), widths[c], rowH, r % 2 === 0 ? "#FFFFFF" : "#F0ECE3", colors.line, 1);
      text(slide, cell, cx + 8, y + rowH * (r + 1) + 8, widths[c] - 16, rowH - 10, { size: 16, color: colors.ink });
      cx += widths[c];
    });
  });
}

function bar(slide, label, value, max, x, y, w, accent) {
  text(slide, label, x, y, 260, 26, { size: 16, color: colors.ink });
  rect(slide, x + 270, y + 6, w, 14, "#E3DDD1", "#E3DDD1", 0);
  rect(slide, x + 270, y + 6, Math.max(4, (value / max) * w), 14, accent, accent, 0);
}

const slides = [
  {
    kicker: "제목",
    claim: "CODE BLUE 보스전 PPO 1M 클리어 결과",
    body: ["최종 run-id BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1", "PPO-only 1M long-run 기준", "visible observation 438", "action spec [5,2]", "무결성 leak 0 기준"],
  },
  {
    kicker: "게임 소개",
    claim: "격자 기반 보스전을 사람 입력 조건으로 학습",
    body: ["게임명 CODE BLUE", "2D 격자 기반 액션 보스전", "경고 타일 회피 후 공격", "이동 none up down left right", "공격 no attack attack", "목표 보스 HP 60 감소"],
  },
  {
    kicker: "보스 소개",
    claim: "HP 기반 페이즈가 학습 난도를 만든 구조",
    body: ["보스 HP 60", "Phase 2 HP 42 이하", "Phase 3 HP 24 이하", "Final HP 6 이하", "인접 시 방향 회전 후 경고 타일", "회피 후 공격권 회복 필요"],
  },
  {
    kicker: "보스 패턴",
    claim: "후반 패턴은 관측과 타격 타이밍을 동시에 요구",
    body: ["1페이즈 X자 패턴", "1페이즈 NZ 및 역순 패턴", "1페이즈 사각형 2칸 돌진", "1페이즈 십자 돌진", "2페이즈 시계방향 4줄 돌진", "3페이즈 MarkATK 표식 돌진", "Final 찍고 돌진 4회"],
  },
  {
    kicker: "MDP 정의",
    claim: "플레이어 MDP와 보스 MDP를 같은 decision step에서 결합",
    body: ["플레이어 MDP", "보스 MDP"],
  },
  {
    kicker: "Observation",
    claim: "193차원 기본 관측을 438차원 visible cue로 확장",
    body: ["기본 observation 193", "MarkATK real visible mask 추가", "MarkATK fake visible mask 추가", "Phase2 sweep history 추가", "최종 observation size 438", "내부 next_band와 sweep index 직접 노출 없음"],
  },
  {
    kicker: "Action",
    claim: "사람 조작에 가까운 [5,2] 분기 입력",
    body: ["Branch 0 move", "none up down left right", "Branch 1 attack", "no attack attack", "이동과 공격을 같은 decision에서 표현 가능", "불가능 이동과 비정상 공격은 mask로 차단"],
  },
  {
    kicker: "Reward",
    claim: "공격 시도 보상 제거 후 실제 적중 중심으로 정리",
    body: ["공격 시도 자체 보상 0", "보스 HP 감소 보상 +0.10 per HP", "성공 hit 보상 +0.08", "boss_dead 보상 +5.0", "player_hit 패널티 -2.0", "player_dead 패널티 -8.0", "fast-clear reward 미적용"],
  },
  {
    kicker: "PPO 기법",
    claim: "PPO와 action masking으로 순차 전투 정책 학습",
    body: ["ML-Agents PPO", "policy gradient 기반 안정적 학습", "discrete action 학습 가능", "hidden target 공격 차단", "off-lane stale target 공격 차단", "visible observation 중심 설계"],
  },
  {
    kicker: "단기 PPO 실패",
    claim: "50K에서는 생존 중심 local optimum 발생",
    body: ["클리어 0", "boss_damage 낮음", "in_range 약 10퍼센트", "공격권 회복 실패", "피격 및 사망 반복", "PPO-only 불가능 결론 아님"],
  },
  {
    kicker: "1M 결과",
    claim: "1M long-run에서 최근 100 episode 80퍼센트 클리어",
    metrics: true,
  },
  {
    kicker: "LTS vs main",
    claim: "LTS는 속도 중심 main은 사람 조건 충실도 중심",
    compare: true,
  },
  {
    kicker: "영상 1 초기 정책",
    claim: "초기 정책은 낮은 damage와 공격권 회복 실패를 보여줌",
    body: ["파일 videos/01_initial_policy.mp4", "99K checkpoint 실제 gameplay", "BossPlayer-99957.onnx", "낮은 boss damage", "피격 위험 장면", "공격권 회복 실패", "설명 슬라이드 사용 없음"],
  },
  {
    kicker: "영상 2 중기 정책",
    claim: "중기 정책은 부분 hit 이후 player_dead로 종료",
    body: ["파일 videos/02_learning_progression.mp4", "499K checkpoint 실제 gameplay", "BossPlayer-499996.onnx", "boss_damage 3", "survival 25.0초", "player_dead", "설명 슬라이드 사용 없음"],
  },
  {
    kicker: "영상 3 클린 클리어",
    claim: "발표 핵심 영상은 leak 0 근거와 함께 사용",
    body: ["파일 videos/03_clean_human_like_clear_all_patterns.mp4", "Phase2 Phase3 Final 포함", "visible target 기반 정상 타격", "exploit 없음", "attack_out_of_range 0", "hidden off-lane stale hit 0"],
  },
  {
    kicker: "영상 4 패턴 스킵성 플레이",
    claim: "패턴 스킵처럼 보이는 버그성 장면은 성공 근거와 분리",
    body: ["파일 videos/04_clever_valid_policy_behavior.mp4", "일부 패턴 생략처럼 보이는 빠른 클리어", "root_visible_overlap", "dash_current_overlap", "현재 leak 0 근거상 exploit 확정 아님", "bug case 명명 금지"],
  },
  {
    kicker: "영상 5 이상 행동 사례",
    claim: "성공 근거가 아니라 한계와 제외 사례로 배치",
    body: ["파일 videos/05_bug_or_abnormal_case_excluded.mp4", "실제 leak 확인 시 bug case", "leak 0이면 abnormal-looking but valid", "성공 결과로 사용하지 않음", "비교 및 주의점 설명용", "bug case 명명 금지"],
  },
  {
    kicker: "결론 및 향후 개선",
    claim: "PPO-only는 가능하며 fast-clear는 별도 브랜치 실험 대상",
    body: ["50K 단기 학습은 생존 중심 local optimum", "1M long-run에서 클린 클리어 정책 학습", "main은 visible observation과 사람 입력 조건 기반 성공", "BC/demo는 현재 필수 아님", "fast-clear reward는 별도 브랜치 권장"],
  },
];

const presentation = Presentation.create({ slideSize: { width: W, height: H } });

for (let i = 0; i < slides.length; i += 1) {
  const spec = slides[i];
  const slide = presentation.slides.add();
  rect(slide, 0, 0, W, H, colors.bg, colors.bg, 0);
  rect(slide, 0, 0, 18, H, i % 2 === 0 ? colors.red : colors.blue, i % 2 === 0 ? colors.red : colors.blue, 0);
  title(slide, spec.kicker, spec.claim);

  if (i === 0) {
    metric(slide, "boss_dead", "199회", 70, 160, 230, colors.red);
    metric(slide, "최근 100 episode", "80%", 330, 160, 250, colors.green);
    metric(slide, "최근 hit_rate", "97.3%", 610, 160, 230, colors.blue);
    metric(slide, "leak", "0", 870, 160, 170, colors.gold);
    bulletList(slide, spec.body, 74, 318, 1020, { size: 23, gap: 42 });
  } else if (spec.metrics) {
    metric(slide, "boss_dead", "199회", 70, 160, 220, colors.red);
    metric(slide, "전체 클리어율", "22.3%", 310, 160, 230, colors.green);
    metric(slide, "최근 100 클리어율", "80%", 560, 160, 240, colors.green);
    metric(slide, "boss_damage avg max", "40.8 / 59", 820, 160, 260, colors.blue);
    bar(slide, "최근 hit_rate", 97.3, 100, 90, 330, 400, colors.green);
    bar(slide, "최근 in_range", 27.5, 100, 90, 374, 400, colors.blue);
    bar(slide, "player_hit 감소 기준", 0.74, 2, 90, 418, 400, colors.red);
    bulletList(slide, ["평균 클리어 시간 70.0초", "최속 클리어 시간 42.6초", "MarkATK observed 15,309회", "attack_out_of_range 0", "hidden off-lane stale hit 0"], 820, 346, 420, { size: 20, gap: 38, dot: colors.gold });
  } else if (spec.compare) {
    table(slide, ["항목", "LTS", "main"], [
      ["action spec", "[6]", "[5,2]"],
      ["observation", "193", "438"],
      ["reward", "강한 damage reward", "hit-gated reward"],
      ["fast-clear", "존재", "미적용"],
      ["특징", "빠른 클리어 중심", "사람 조건 충실도 중심"],
      ["결론", "속도 레시피", "PPO-only 1M 성공"],
    ], 84, 166, [210, 390, 430], 50);
  } else if (i === 4) {
    twoColumn(slide, "플레이어 MDP", ["State 위치 방향 체력", "State 공격 가능 범위", "Action move [5]", "Action attack [2]", "Transition 이동 공격 피격", "Termination player_dead"], "보스 MDP", ["State 위치 방향 HP phase", "State pattern warning damage", "State MarkATK real fake cue", "Transition phase pattern 진행", "Transition HP 감소", "Termination boss_dead timeout"]);
  } else {
    bulletList(slide, spec.body, 80, 166, 1040, { size: spec.body.length > 6 ? 20 : 23, gap: spec.body.length > 6 ? 36 : 44 });
  }

  footer(slide, i + 1);
}

await fs.mkdir(OUT_DIR, { recursive: true });
await fs.mkdir(PREVIEW_DIR, { recursive: true });

for (let i = 0; i < presentation.slides.count; i += 1) {
  const slide = presentation.slides.getItem(i);
  const png = await presentation.export({ slide, format: "png", scale: 1 });
  const buffer = Buffer.from(await png.arrayBuffer());
  await fs.writeFile(path.join(PREVIEW_DIR, `slide-${String(i + 1).padStart(2, "0")}.png`), buffer);
}

const pptx = await PresentationFile.exportPptx(presentation);
await pptx.save(PPTX_PATH);

const manifest = {
  pptx: PPTX_PATH,
  slideCount: presentation.slides.count,
  previewDir: PREVIEW_DIR,
  generatedAt: new Date().toISOString(),
};
await fs.writeFile(path.join(OUT_DIR, "CODE_BLUE_RL_FINAL_manifest.json"), `${JSON.stringify(manifest, null, 2)}\n`, "utf8");
console.log(JSON.stringify(manifest, null, 2));
