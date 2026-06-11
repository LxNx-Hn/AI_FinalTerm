import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";

const SCRIPT_DIR = path.dirname(new URL(import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, "$1"));
const ROOT = path.resolve(SCRIPT_DIR, "../..");
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
  bg: "#F7F7F3",
  ink: "#202124",
  muted: "#5F6368",
  line: "#DADCE0",
  panel: "#FFFFFF",
  red: "#B3261E",
  blue: "#1A73E8",
  green: "#188038",
  amber: "#B06000",
  slate: "#3C4043",
  softRed: "#FCE8E6",
  softBlue: "#E8F0FE",
  softGreen: "#E6F4EA",
};

const assets = {
  gameWarning: path.join(ROOT, "presentations", "final", "assets", "source_images", "game_warning_tile.png"),
  bossClose: path.join(ROOT, "presentations", "final", "assets", "source_images", "boss_close_range.png"),
  v1: path.join(ROOT, "presentations", "final", "assets", "video_thumbnails", "01_initial_policy.png"),
  v2: path.join(ROOT, "presentations", "final", "assets", "video_thumbnails", "02_mid_rule_learning.png"),
  v3: path.join(ROOT, "presentations", "final", "assets", "video_thumbnails", "03_mid_pattern_learning.png"),
  v4: path.join(ROOT, "presentations", "final", "assets", "video_thumbnails", "04_late_phase23_patterns.png"),
  v5: path.join(ROOT, "presentations", "final", "assets", "video_thumbnails", "05_late_clever_clear.png"),
  bossDamage: path.join(ROOT, "docs", "rl_final", "figures", "boss_damage_by_step.png"),
  clearRate: path.join(ROOT, "docs", "rl_final", "figures", "clear_rate_by_step.png"),
  reward: path.join(ROOT, "docs", "rl_final", "figures", "reward_by_step.png"),
  hitRate: path.join(ROOT, "docs", "rl_final", "figures", "hit_rate_by_step.png"),
  inRange: path.join(ROOT, "docs", "rl_final", "figures", "in_range_by_step.png"),
  clearTime: path.join(ROOT, "docs", "rl_final", "figures", "clear_time_distribution.png"),
  phaseTimeline: path.join(ROOT, "docs", "rl_final", "figures", "phase_milestone_timeline.png"),
  integrity: path.join(ROOT, "docs", "rl_final", "figures", "integrity_leak_table.png"),
};

for (const [name, filePath] of Object.entries(assets)) {
  await fs.access(filePath).catch(() => {
    throw new Error(`필수 발표 이미지 없음: ${name} ${filePath}`);
  });
}

const imagePayloads = new Map();
for (const filePath of Object.values(assets)) {
  imagePayloads.set(filePath, await fs.readFile(filePath));
}

function rel(filePath) {
  return path.relative(ROOT, filePath).replaceAll("\\", "/");
}

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
  shape.text.fontSize = options.size || 22;
  shape.text.color = options.color || colors.ink;
  shape.text.bold = Boolean(options.bold);
  shape.text.typeface = options.face || "Malgun Gothic";
  shape.text.alignment = options.align || "left";
  shape.text.verticalAlignment = options.valign || "top";
  shape.text.insets = options.insets || { left: 8, right: 8, top: 4, bottom: 4 };
  return shape;
}

function image(slide, filePath, x, y, w, h, label) {
  slide.images.add({
    data: imagePayloads.get(filePath),
    contentType: "image/png",
    position: { left: x, top: y, width: w, height: h },
    alt: label || path.basename(filePath),
  });
  rect(slide, x, y, w, h, "#FFFFFF00", colors.line, 1);
  if (label) {
    text(slide, label, x, y + h - 30, w, 30, {
      size: 14,
      color: "#FFFFFF",
      fill: "#00000080",
      insets: { left: 10, right: 10, top: 5, bottom: 4 },
    });
  }
}

function title(slide, section, claim) {
  text(slide, section, 54, 32, 320, 28, { size: 15, bold: true, color: colors.red });
  text(slide, claim, 54, 62, 1070, 54, { size: 32, bold: true });
  rect(slide, 54, 126, 1170, 2, colors.line, colors.line, 0);
}

function footer(slide, n) {
  text(slide, "CODE BLUE PPO 발표자료", 54, 668, 280, 22, { size: 12, color: colors.muted });
  text(slide, String(n).padStart(2, "0"), 1168, 664, 56, 24, { size: 14, color: colors.muted, align: "right" });
}

function bulletList(slide, items, x, y, w, options = {}) {
  const size = options.size || 21;
  const gap = options.gap || 38;
  const dot = options.dot || colors.red;
  items.forEach((item, index) => {
    const yy = y + index * gap;
    rect(slide, x, yy + 10, 8, 8, dot, dot, 0);
    text(slide, item, x + 20, yy, w - 20, gap, {
      size,
      color: options.color || colors.ink,
      bold: options.boldIndexes?.includes(index),
      insets: { left: 0, right: 4, top: 0, bottom: 0 },
    });
  });
}

function chip(slide, value, x, y, w, fill, color = colors.ink) {
  rect(slide, x, y, w, 34, fill, fill, 0);
  text(slide, value, x + 8, y + 7, w - 16, 22, { size: 15, bold: true, color, align: "center" });
}

function metric(slide, label, value, x, y, w, accent) {
  rect(slide, x, y, w, 94, colors.panel, colors.line, 1);
  text(slide, value, x + 14, y + 15, w - 28, 34, { size: 27, bold: true, color: accent });
  text(slide, label, x + 14, y + 54, w - 28, 24, { size: 14, color: colors.muted });
}

function table(slide, headers, rows, x, y, widths, rowH = 42) {
  let cx = x;
  headers.forEach((header, i) => {
    rect(slide, cx, y, widths[i], rowH, colors.slate, colors.slate, 0);
    text(slide, header, cx + 8, y + 8, widths[i] - 16, rowH - 10, { size: 16, bold: true, color: "#FFFFFF" });
    cx += widths[i];
  });
  rows.forEach((row, r) => {
    cx = x;
    row.forEach((cell, c) => {
      rect(slide, cx, y + rowH * (r + 1), widths[c], rowH, r % 2 === 0 ? "#FFFFFF" : "#F3F4F4", colors.line, 1);
      text(slide, cell, cx + 8, y + rowH * (r + 1) + 7, widths[c] - 16, rowH - 8, { size: 15, color: colors.ink });
      cx += widths[c];
    });
  });
}

function drawFlow(slide, steps, x, y, w) {
  const gap = 16;
  const boxW = (w - gap * (steps.length - 1)) / steps.length;
  steps.forEach((step, i) => {
    const bx = x + i * (boxW + gap);
    rect(slide, bx, y, boxW, 74, i % 2 === 0 ? colors.softBlue : colors.softGreen, colors.line, 1);
    text(slide, step, bx + 10, y + 16, boxW - 20, 42, { size: 18, bold: true, align: "center", valign: "middle" });
  });
}

const videoRows = [
  ["1", "학습 초기", "videos/01_initial_policy.mp4", "199K checkpoint 실제 gameplay", "완료"],
  ["2", "중기 규칙 학습", "videos/02_learning_progression.mp4", "499K checkpoint warning tile 회피", "완료"],
  ["3", "중기 패턴 학습", "videos/03_mid_pattern_learning.mp4", "799K checkpoint phase2 sweep 대응 후 player_dead", "완료"],
  ["4", "후기 2 3 페이즈 패턴", "videos/04_late_phase23_patterns.mp4", "799K phase2 sweep 패턴 노출", "완료"],
  ["5", "후기 꼼수성 클리어", "videos/05_late_clever_clear.mp4", "1M PPO inference boss_dead", "완료"],
];

const slides = [
  {
    section: "제목",
    claim: "CODE BLUE 보스전 PPO 강화학습 발표자료",
    kind: "cover",
  },
  {
    section: "게임 소개",
    claim: "2D 격자 보스전을 사람 입력 조건으로 학습",
    image: assets.gameWarning,
    items: [
      "게임명 CODE BLUE",
      "보스명 Delulu the Dream Eater",
      "격자 기반 이동과 근접 공격으로 보스 HP 60 감소",
      "경고 타일 확인 후 회피와 재진입을 반복",
      "사람 입력과 같은 move 5개 attack 2개 분기 사용",
    ],
  },
  {
    section: "패턴 소개",
    claim: "보스 패턴은 경고 타일과 실제 피해 타일을 분리",
    kind: "patterns",
  },
  {
    section: "패턴 자료",
    claim: "후반 패턴은 visible cue와 sweep history 관측을 요구",
    kind: "patternPhotos",
  },
  {
    section: "플레이 영상",
    claim: "발표용 영상은 5개 슬롯으로 구성",
    kind: "videoTable",
  },
  {
    section: "MDP 정의",
    claim: "플레이어와 보스를 하나의 decision step에서 결합",
    kind: "mdp",
  },
  {
    section: "상태공간",
    claim: "상태는 위치 체력 위험 타일 visible cue 학습 이력으로 구성",
    kind: "stateSpace",
  },
  {
    section: "행동공간",
    claim: "행동은 MultiDiscrete [5,2]와 action mask로 제한",
    kind: "actionSpace",
  },
  {
    section: "PPO 기법",
    claim: "PPO는 정책 갱신 폭을 제한해 순차 전투 정책을 안정화",
    kind: "ppo",
  },
  {
    section: "보상 설계",
    claim: "공격 시도 보상은 제거하고 실제 적중과 생존 실패를 분리",
    kind: "reward",
  },
  {
    section: "학습 설계",
    claim: "1M long-run은 구조 변경 없이 timestep을 늘린 재현 실험",
    kind: "training",
  },
  {
    section: "학습 초기",
    claim: "199K 정책은 이동과 회피가 불안정하지만 실제 행동이 보임",
    kind: "videoSingle",
    image: assets.v1,
    items: [
      "영상 1 학습 초기",
      "199K checkpoint 실제 gameplay",
      "45초 구간 내 boss_dead 없음",
      "낮은 damage와 피격 위험 장면",
      "최종 정책 영상을 초기 정책으로 재라벨링하지 않음",
    ],
  },
  {
    section: "학습 중기",
    claim: "499K는 경고 타일 회피와 거리 규칙 단서가 보임",
    kind: "videoDouble",
  },
  {
    section: "학습 후기",
    claim: "후기 영상은 phase2 패턴 노출과 꼼수성 클리어를 분리",
    kind: "lateVideos",
  },
  {
    section: "학습 결과",
    claim: "1M long-run에서 최근 100 episode 80퍼센트 클리어",
    kind: "metrics",
  },
  {
    section: "결과 그래프",
    claim: "damage clear rate reward hit rate가 함께 개선",
    kind: "charts",
  },
  {
    section: "검증과 한계",
    claim: "leak 0 근거와 영상별 검증 범위를 같이 보고",
    kind: "integrity",
  },
  {
    section: "마무리",
    claim: "발표 흐름은 게임 소개에서 PPO 결과까지 직선으로 연결",
    kind: "closing",
  },
];

const presentation = Presentation.create({ slideSize: { width: W, height: H } });

for (let i = 0; i < slides.length; i += 1) {
  const spec = slides[i];
  const slide = presentation.slides.add();
  rect(slide, 0, 0, W, H, colors.bg, colors.bg, 0);
  rect(slide, 0, 0, 16, H, i % 2 === 0 ? colors.red : colors.blue, i % 2 === 0 ? colors.red : colors.blue, 0);
  title(slide, spec.section, spec.claim);

  if (spec.kind === "cover") {
    metric(slide, "run-id", "1M PPO", 74, 160, 210, colors.blue);
    metric(slide, "observation", "438", 304, 160, 190, colors.green);
    metric(slide, "action", "[5,2]", 514, 160, 190, colors.red);
    metric(slide, "leak", "0", 724, 160, 160, colors.amber);
    image(slide, assets.gameWarning, 930, 154, 270, 152, "게임 화면");
    drawFlow(slide, ["게임 소개", "MDP", "PPO", "보상 설계", "학습 결과"], 74, 350, 1080);
    bulletList(slide, [
      "마침표 없는 개조식 본문",
      "실제 영상과 로그 기반 수치 사용",
      "초기 중기 후기 5개 실제 플레이 영상 포함",
    ], 84, 470, 980, { size: 21, gap: 38, dot: colors.slate });
  } else if (spec.image) {
    image(slide, spec.image, 694, 158, 500, 282, "실제 플레이 캡처");
    bulletList(slide, spec.items, 78, 166, 560, { size: 22, gap: 42, dot: colors.blue });
    text(slide, rel(spec.image), 694, 452, 500, 28, { size: 13, color: colors.muted });
  } else if (spec.kind === "patterns") {
    image(slide, assets.bossClose, 76, 154, 500, 282, "보스 근접 상태");
    bulletList(slide, [
      "Phase 1 X자 패턴",
      "Phase 1 NZ 및 역순 패턴",
      "Phase 1 사각형 2칸 돌진",
      "Phase 1 십자 돌진",
      "Phase 2 시계방향 4줄 돌진",
      "Phase 3 MarkATK 표식 돌진",
      "Final 찍고 돌진 4회",
    ], 638, 160, 530, { size: 20, gap: 36, dot: colors.red });
  } else if (spec.kind === "patternPhotos") {
    image(slide, assets.gameWarning, 70, 154, 536, 302, "경고 타일");
    image(slide, assets.v2, 656, 154, 536, 302, "피해 범위");
    bulletList(slide, [
      "경고 타일은 위험 회피 관측으로 사용",
      "피해 타일은 패널티와 action mask 근거로 사용",
      "MarkATK real fake visible mask를 49칸 단위로 추가",
      "Phase2 sweep history는 이전 warning damage 흐름을 저장",
    ], 86, 488, 1060, { size: 20, gap: 34, dot: colors.green });
  } else if (spec.kind === "videoTable") {
    table(slide, ["번호", "분류", "파일", "근거", "상태"], videoRows, 54, 156, [56, 170, 320, 440, 110], 56);
    text(slide, "영상 3은 799K checkpoint 본캡처와 Player log를 함께 사용", 74, 610, 930, 28, { size: 17, color: colors.green, bold: true });
  } else if (spec.kind === "mdp") {
    text(slide, "플레이어 MDP", 88, 158, 470, 34, { size: 23, bold: true, color: colors.blue });
    bulletList(slide, [
      "State 위치 방향 체력 공격 가능 범위",
      "State warning tile damage tile recent hazard",
      "Action move none up down left right",
      "Action attack no attack attack",
      "Transition 이동 공격 피격 쿨다운",
      "Termination player_dead timeout",
    ], 92, 206, 500, { size: 20, gap: 36, dot: colors.blue });
    text(slide, "보스 MDP", 676, 158, 470, 34, { size: 23, bold: true, color: colors.red });
    bulletList(slide, [
      "State 위치 방향 HP phase",
      "State pattern warning damage",
      "State MarkATK real fake cue",
      "Transition phase pattern 진행",
      "Transition HP 감소와 boss_dead",
      "Reward는 두 MDP 결과를 한 step에서 결합",
    ], 680, 206, 500, { size: 20, gap: 36, dot: colors.red });
  } else if (spec.kind === "stateSpace") {
    metric(slide, "기본 관측", "193", 76, 160, 190, colors.blue);
    metric(slide, "최종 관측", "438", 286, 160, 190, colors.green);
    metric(slide, "arena mask", "49칸", 496, 160, 190, colors.red);
    metric(slide, "추가 mask", "5종", 706, 160, 190, colors.amber);
    bulletList(slide, [
      "플레이어 위치 방향 체력 보스 HP",
      "보스 visible 여부와 visible 위치",
      "warning mask damage mask prev warning mask",
      "공격 가능 여부 거리 위험 타일",
      "MarkATK real visible mask",
      "MarkATK fake visible mask",
      "prev warning2 prev damage sweep history",
      "next_band와 sweep index 직접 노출 없음",
    ], 86, 300, 1000, { size: 20, gap: 32, dot: colors.green });
  } else if (spec.kind === "actionSpace") {
    rect(slide, 80, 174, 500, 160, colors.softBlue, colors.line, 1);
    text(slide, "Branch 0 move", 100, 198, 240, 28, { size: 23, bold: true, color: colors.blue });
    bulletList(slide, ["none", "up", "down", "left", "right"], 110, 242, 340, { size: 20, gap: 28, dot: colors.blue });
    rect(slide, 674, 174, 430, 160, colors.softRed, colors.line, 1);
    text(slide, "Branch 1 attack", 694, 198, 250, 28, { size: 23, bold: true, color: colors.red });
    bulletList(slide, ["no attack", "attack"], 704, 242, 300, { size: 20, gap: 34, dot: colors.red });
    bulletList(slide, [
      "불가능 이동은 mask로 차단",
      "hidden target 공격 차단",
      "off-lane stale target 공격 차단",
      "공격 허용 여부는 BossRLTargetAlignmentDiagnostics와 ApplySingleAction 경로로 확인",
    ], 92, 412, 1020, { size: 21, gap: 40, dot: colors.slate });
  } else if (spec.kind === "ppo") {
    bulletList(slide, [
      "ML-Agents PPO trainer 사용",
      "policy gradient 기반으로 행동 확률을 직접 학습",
      "epsilon 0.2로 급격한 policy update 제한",
      "GAE lambda 0.95와 gamma 0.99 사용",
      "hidden_units 512 num_layers 3",
      "discrete action과 action mask를 함께 사용",
    ], 84, 164, 610, { size: 22, gap: 42, dot: colors.blue });
    rect(slide, 760, 172, 360, 270, colors.panel, colors.line, 1);
    text(slide, "PPO 적용 이유", 782, 198, 300, 32, { size: 23, bold: true });
    bulletList(slide, [
      "보스전은 순차 의사결정",
      "탐색 중 위험 타일 회피 필요",
      "공격 시점 학습 필요",
      "사람 입력과 같은 discrete branch 사용",
    ], 794, 250, 280, { size: 19, gap: 36, dot: colors.green });
  } else if (spec.kind === "reward") {
    table(slide, ["항목", "값", "의미"], [
      ["BossDamagePerHp", "+0.10", "HP 감소 보상"],
      ["SuccessfulHitBonusReward", "+0.08", "실제 hit 보상"],
      ["BossKillReward", "+5.0", "boss_dead 보상"],
      ["PlayerHitPenalty", "-2.0", "피격 패널티"],
      ["PlayerDeathPenalty", "-8.0", "사망 패널티"],
      ["Warning Damage", "-0.10 -0.20", "위험 타일 패널티"],
    ], 78, 156, [330, 170, 520], 54);
    bulletList(slide, [
      "공격 시도 자체 보상 0",
      "miss farming 방지 목적",
      "fast-clear reward는 main run 미적용",
    ], 94, 536, 940, { size: 20, gap: 34, dot: colors.red });
  } else if (spec.kind === "training") {
    metric(slide, "trainer", "PPO", 78, 162, 180, colors.blue);
    metric(slide, "max steps", "1M", 278, 162, 180, colors.green);
    metric(slide, "batch buffer", "1024 10240", 478, 162, 250, colors.red);
    metric(slide, "network", "512 x 3", 748, 162, 200, colors.amber);
    bulletList(slide, [
      "config ml-agents-config/boss_ppo_target_gate_markatk_sweep_1m_c84b1a4.yaml",
      "run-id BossPPO_TargetGate_MarkATKObs_SweepHistory_1M_c84b1a4_v1",
      "c84b1a4 계열 구조 유지",
      "NO stationary lever",
      "NO warn-tile relax",
      "NO demo BC",
      "max_steps summary_freq checkpoint_interval만 long-run 용도로 조정",
    ], 88, 310, 1030, { size: 20, gap: 34, dot: colors.green });
  } else if (spec.kind === "videoSingle") {
    image(slide, spec.image, 78, 156, 560, 315, "영상 1 썸네일");
    bulletList(slide, spec.items, 696, 164, 460, { size: 21, gap: 38, dot: colors.blue });
    text(slide, "videos/01_initial_policy.mp4", 78, 486, 560, 28, { size: 15, color: colors.muted });
  } else if (spec.kind === "videoDouble") {
    image(slide, assets.v2, 70, 154, 528, 297, "영상 2 중기 규칙 학습");
    image(slide, assets.v3, 654, 154, 528, 297, "영상 3 중기 패턴 학습");
    bulletList(slide, [
      "영상 2는 499K checkpoint 실제 gameplay",
      "warning tile 회피와 safe opportunity 판단",
      "일부 attack과 boss 이동 대응 확인",
      "영상 3은 799K checkpoint 실제 gameplay",
      "boss_damage 8 이후 player_dead",
      "phase2 sweep history 653 step 확인",
    ], 92, 486, 1040, { size: 19, gap: 32, dot: colors.amber });
  } else if (spec.kind === "lateVideos") {
    image(slide, assets.v4, 70, 154, 528, 297, "영상 4 후기 2 3 페이즈");
    image(slide, assets.v5, 654, 154, 528, 297, "영상 5 꼼수성 클리어");
    bulletList(slide, [
      "후기 패턴 영상은 phase2 sweep warning tile과 smoke 노출",
      "꼼수성 클리어는 diagonal blindspot처럼 보이는 근접 연속 공격",
      "영상 4는 799K phase2 sweep 노출 구간",
      "영상 5는 1M PPO inference boss_dead 로그 확인",
    ], 92, 486, 1040, { size: 19, gap: 34, dot: colors.red });
  } else if (spec.kind === "metrics") {
    metric(slide, "boss_dead", "199회", 76, 160, 200, colors.red);
    metric(slide, "전체 클리어율", "22.3%", 296, 160, 220, colors.green);
    metric(slide, "최근 100 클리어율", "80%", 536, 160, 240, colors.green);
    metric(slide, "최속 클리어", "42.6초", 796, 160, 210, colors.blue);
    metric(slide, "MarkATK observed", "15,309회", 76, 294, 250, colors.amber);
    metric(slide, "최근 hit_rate", "97.3%", 346, 294, 220, colors.green);
    metric(slide, "최근 in_range", "27.5%", 586, 294, 220, colors.blue);
    metric(slide, "boss_damage avg max", "40.8 / 59", 826, 294, 250, colors.red);
    bulletList(slide, [
      "그래프는 Player log episode summary 892개와 training_status checkpoint reward에서 생성",
      "boss_dead 199회 파싱",
      "평균 클리어 시간 70.0초",
    ], 88, 454, 940, { size: 20, gap: 36, dot: colors.slate });
  } else if (spec.kind === "charts") {
    image(slide, assets.bossDamage, 54, 150, 270, 168, "boss damage");
    image(slide, assets.clearRate, 354, 150, 270, 168, "clear rate");
    image(slide, assets.reward, 654, 150, 270, 168, "reward");
    image(slide, assets.hitRate, 954, 150, 270, 168, "hit rate");
    image(slide, assets.inRange, 54, 356, 270, 168, "in range");
    image(slide, assets.clearTime, 354, 356, 270, 168, "clear time");
    image(slide, assets.phaseTimeline, 654, 356, 270, 168, "phase timeline");
    image(slide, assets.integrity, 954, 356, 270, 168, "leak table");
  } else if (spec.kind === "integrity") {
    image(slide, assets.integrity, 76, 154, 450, 270, "무결성 표");
    bulletList(slide, [
      "attack_out_of_range 0",
      "hidden hit 0",
      "off-lane hit 0",
      "stale hit 0",
      "fake marker mask leak 0",
      "next_band leak 0",
      "sweep_sequence_index leak 0",
    ], 590, 164, 560, { size: 21, gap: 36, dot: colors.green });
    text(slide, "한계", 86, 468, 160, 30, { size: 22, bold: true, color: colors.red });
    bulletList(slide, [
      "영상 3과 영상 4는 799K phase2 sweep 구간 기반",
      "영상 5는 1M inference boss_dead 구간 기반",
      "꼼수성 클리어는 exploit 확정 없이 현상으로 설명",
    ], 92, 512, 1020, { size: 20, gap: 34, dot: colors.red });
  } else if (spec.kind === "closing") {
    drawFlow(slide, ["게임 소개", "패턴 소개", "MDP", "PPO", "보상 설계", "학습 설계", "학습 결과"], 64, 174, 1120);
    bulletList(slide, [
      "발표 본문은 마침표 없는 개조식 유지",
      "PPT는 실제 캡처와 학습 그래프 포함",
      "영상 5개 파일 경로를 슬라이드에 명시",
      "중기 패턴 영상은 799K 본캡처로 보강 완료",
      "PPO-only 1M에서 클린 클리어 정책 학습 성공",
    ], 92, 326, 1040, { size: 22, gap: 42, dot: colors.slate });
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
  sourceImages: {
    gameWarning: rel(assets.gameWarning),
    bossClose: rel(assets.bossClose),
  },
  videoSlots: videoRows.map(([slot, label, file, basis, status]) => ({ slot, label, file, basis, status })),
  figures: [
    rel(assets.bossDamage),
    rel(assets.clearRate),
    rel(assets.reward),
    rel(assets.hitRate),
    rel(assets.inRange),
    rel(assets.clearTime),
    rel(assets.phaseTimeline),
    rel(assets.integrity),
  ],
  caveats: [
    "영상 4는 클리어 영상이 아니라 phase2 pattern exposure clip",
    "영상 5는 exploit 확정이 아니라 abnormal-looking clear로 설명",
  ],
};

await fs.writeFile(path.join(OUT_DIR, "CODE_BLUE_RL_FINAL_manifest.json"), `${JSON.stringify(manifest, null, 2)}\n`, "utf8");
console.log(JSON.stringify(manifest, null, 2));
