# boss_patterns.py
from coordinate_mapping import ARENA_X_MIN, ARENA_X_MAX, ARENA_Y_MIN, ARENA_Y_MAX

PATTERN_IDS = {
    "NONE": 0,
    "GAP": 1,
    "BOSS_MOVE": 2,
    "BASIC_SCRATCH": 3,
    "ENHANCED_SCRATCH": 4,
    "PATTERN_ENTRY_DASH": 5,
    "MARK_DASH": 6,
    "LANDING_SLAM": 7
}

def normal_scratch_directional(boss_cell: tuple[int, int], facing: tuple[int, int]) -> list[tuple[int, int]]:
    result = []
    side = (-facing[1], facing[0])
    # 1 cell forward, 3 cells wide
    for offset in range(-1, 2):
        cx = boss_cell[0] + facing[0] + side[0] * offset
        cy = boss_cell[1] + facing[1] + side[1] * offset
        result.append((cx, cy))
    return result

def enhanced_scratch_directional(boss_cell: tuple[int, int], facing: tuple[int, int]) -> list[tuple[int, int]]:
    result = []
    side = (-facing[1], facing[0])
    # 2 cells forward, 3 cells wide
    for forward in range(1, 3):
        for offset in range(-1, 2):
            cx = boss_cell[0] + facing[0] * forward + side[0] * offset
            cy = boss_cell[1] + facing[1] * forward + side[1] * offset
            result.append((cx, cy))
    return result

def forward_stripe3_from_cell(boss_cell: tuple[int, int], facing: tuple[int, int]) -> list[tuple[int, int]]:
    result = []
    side = (-facing[1], facing[0])
    # Infinite line forward, 3 cells wide
    curr = (boss_cell[0] + facing[0], boss_cell[1] + facing[1])
    while ARENA_X_MIN <= curr[0] <= ARENA_X_MAX and ARENA_Y_MIN <= curr[1] <= ARENA_Y_MAX:
        for offset in range(-1, 2):
            result.append((curr[0] + side[0] * offset, curr[1] + side[1] * offset))
        curr = (curr[0] + facing[0], curr[1] + facing[1])
    return result

def hollow_corner_5x5(center: tuple[int, int]) -> list[tuple[int, int]]:
    result = []
    for dx in range(-2, 3):
        for dy in range(-2, 3):
            if abs(dx) == 2 and abs(dy) == 2:
                continue
            result.append((center[0] + dx, center[1] + dy))
    return result

def mark_horizontal5(center: tuple[int, int]) -> list[tuple[int, int]]:
    result = []
    for dx in range(-2, 3):
        result.append((center[0] + dx, center[1]))
    return result

def horizontal_stripe3(y: int) -> list[tuple[int, int]]:
    result = []
    for x in range(ARENA_X_MIN, ARENA_X_MAX + 1):
        for dy in range(-1, 2):
            result.append((x, y + dy))
    return result

def pattern_dash_start(cells: list[tuple[int, int]], dash_dir: tuple[int, int]) -> tuple[int, int]:
    if not cells: return (0, 0)
    # Start is the "minimum" cell in the direction of dash
    # So if dash_dir is (1,0), start is min X
    min_val = float('inf')
    best = cells[0]
    for c in cells:
        val = c[0] * dash_dir[0] + c[1] * dash_dir[1]
        if val < min_val:
            min_val = val
            best = c
    return best

def pattern_dash_end(cells: list[tuple[int, int]], dash_dir: tuple[int, int]) -> tuple[int, int]:
    if not cells: return (0, 0)
    max_val = float('-inf')
    best = cells[0]
    for c in cells:
        val = c[0] * dash_dir[0] + c[1] * dash_dir[1]
        if val > max_val:
            max_val = val
            best = c
    return best

def directional_edge_cell(start: tuple[int, int], direction: tuple[int, int]) -> tuple[int, int]:
    curr = start
    while True:
        nxt = (curr[0] + direction[0], curr[1] + direction[1])
        if not (ARENA_X_MIN <= nxt[0] <= ARENA_X_MAX and ARENA_Y_MIN <= nxt[1] <= ARENA_Y_MAX):
            break
        curr = nxt
    return curr
