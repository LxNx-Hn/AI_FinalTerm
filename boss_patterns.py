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
    # 0, 1, 2 cells forward, 3 cells wide
    for forward in range(0, 3):
        for offset in range(-1, 2):
            cx = boss_cell[0] + facing[0] * forward + side[0] * offset
            cy = boss_cell[1] + facing[1] * forward + side[1] * offset
            result.append((cx, cy))
    return result

def normal_scratch3(boss_cell: tuple[int, int]) -> list[tuple[int, int]]:
    result = []
    for dx in range(-1, 2):
        for dy in range(-1, 2):
            cx, cy = boss_cell[0] + dx, boss_cell[1] + dy
            if ARENA_X_MIN <= cx <= ARENA_X_MAX and ARENA_Y_MIN <= cy <= ARENA_Y_MAX:
                result.append((cx, cy))
    return result

def enhanced_scratch_directional(boss_cell: tuple[int, int], facing: tuple[int, int]) -> list[tuple[int, int]]:
    result = []
    side = (-facing[1], facing[0])
    # 0, 1, 2 cells forward, 3 cells wide
    for forward in range(0, 3):
        for offset in range(-1, 2):
            cx = boss_cell[0] + facing[0] * forward + side[0] * offset
            cy = boss_cell[1] + facing[1] * forward + side[1] * offset
            result.append((cx, cy))
    return result

def forward_stripe3_from_cell(boss_cell: tuple[int, int], facing: tuple[int, int]) -> list[tuple[int, int]]:
    result = []
    if facing == (0, 0):
        return result
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

def vertical_stripe3(x: int) -> list[tuple[int, int]]:
    result = []
    for y in range(ARENA_Y_MIN, ARENA_Y_MAX + 1):
        for dx in range(-1, 2):
            result.append((x + dx, y))
    return result

def left_edge2() -> list[tuple[int, int]]:
    result = []
    for y in range(ARENA_Y_MIN, ARENA_Y_MAX + 1):
        for dx in range(0, 2):
            result.append((ARENA_X_MIN + dx, y))
    return result

def right_edge2() -> list[tuple[int, int]]:
    result = []
    for y in range(ARENA_Y_MIN, ARENA_Y_MAX + 1):
        for dx in range(0, 2):
            result.append((ARENA_X_MAX - dx, y))
    return result

def top_edge2() -> list[tuple[int, int]]:
    result = []
    for x in range(ARENA_X_MIN, ARENA_X_MAX + 1):
        for dy in range(0, 2):
            result.append((x, ARENA_Y_MAX - dy))
    return result

def bottom_edge2() -> list[tuple[int, int]]:
    result = []
    for x in range(ARENA_X_MIN, ARENA_X_MAX + 1):
        for dy in range(0, 2):
            result.append((x, ARENA_Y_MIN + dy))
    return result

def left_band4() -> list[tuple[int, int]]:
    result = []
    for y in range(ARENA_Y_MIN, ARENA_Y_MAX + 1):
        for dx in range(0, 4):
            result.append((ARENA_X_MIN + dx, y))
    return result

def right_band4() -> list[tuple[int, int]]:
    result = []
    for y in range(ARENA_Y_MIN, ARENA_Y_MAX + 1):
        for dx in range(0, 4):
            result.append((ARENA_X_MAX - dx, y))
    return result

def top_band4() -> list[tuple[int, int]]:
    result = []
    for x in range(ARENA_X_MIN, ARENA_X_MAX + 1):
        for dy in range(0, 4):
            result.append((x, ARENA_Y_MAX - dy))
    return result

def bottom_band4() -> list[tuple[int, int]]:
    result = []
    for x in range(ARENA_X_MIN, ARENA_X_MAX + 1):
        for dy in range(0, 4):
            result.append((x, ARENA_Y_MIN + dy))
    return result

def diagonal_tr_bl_3() -> list[tuple[int, int]]:
    # Top-Right to Bottom-Left 3-width
    result = []
    for x in range(ARENA_X_MIN, ARENA_X_MAX + 1):
        for y in range(ARENA_Y_MIN, ARENA_Y_MAX + 1):
            # Center diagonal is x == y (since TR is 3,3 and BL is -3,-3)
            # Distance from center diagonal is abs(x - y)
            if abs(x - y) <= 1:
                result.append((x, y))
    return result

def diagonal_tl_br_3() -> list[tuple[int, int]]:
    # Top-Left to Bottom-Right 3-width
    result = []
    for x in range(ARENA_X_MIN, ARENA_X_MAX + 1):
        for y in range(ARENA_Y_MIN, ARENA_Y_MAX + 1):
            # Center diagonal is x == -y (since TL is -3,3 and BR is 3,-3)
            if abs(x + y) <= 1:
                result.append((x, y))
    return result

def mark_vertical5(center: tuple[int, int]) -> list[tuple[int, int]]:
    result = []
    for dy in range(-2, 3):
        result.append((center[0], center[1] + dy))
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
    if direction == (0, 0):
        return start
    curr = start
    while True:
        nxt = (curr[0] + direction[0], curr[1] + direction[1])
        if not (ARENA_X_MIN <= nxt[0] <= ARENA_X_MAX and ARENA_Y_MIN <= nxt[1] <= ARENA_Y_MAX):
            break
        curr = nxt
    return curr

def get_centered_dash_start(damage_cells: list[tuple[int, int]], dash_dir: tuple[int, int]) -> tuple[int, int]:
    if not damage_cells:
        return (0, 0)
        
    avg_x = sum(c[0] for c in damage_cells) / len(damage_cells)
    avg_y = sum(c[1] for c in damage_cells) / len(damage_cells)
    
    center_x = max(ARENA_X_MIN, min(ARENA_X_MAX, round(avg_x)))
    center_y = max(ARENA_Y_MIN, min(ARENA_Y_MAX, round(avg_y)))
    
    if dash_dir[0] > 0:
        return (ARENA_X_MIN, center_y)
    if dash_dir[0] < 0:
        return (ARENA_X_MAX, center_y)
    if dash_dir[1] > 0:
        return (center_x, ARENA_Y_MIN)
    if dash_dir[1] < 0:
        return (center_x, ARENA_Y_MAX)
        
    return (center_x, center_y)

def get_centered_dash_end(start: tuple[int, int], dash_dir: tuple[int, int]) -> tuple[int, int]:
    if dash_dir[0] > 0:
        return (ARENA_X_MAX, start[1])
    if dash_dir[0] < 0:
        return (ARENA_X_MIN, start[1])
    if dash_dir[1] > 0:
        return (start[0], ARENA_Y_MAX)
    if dash_dir[1] < 0:
        return (start[0], ARENA_Y_MIN)
        
    return start
