# coordinate_mapping.py
from config import ARENA_X_MIN, ARENA_X_MAX, ARENA_Y_MIN, ARENA_Y_MAX, PATTERN_X_MIN, PATTERN_X_MAX, PATTERN_Y_MIN, PATTERN_Y_MAX

def is_valid_cell(x: int, y: int) -> bool:
    return ARENA_X_MIN <= x <= ARENA_X_MAX and ARENA_Y_MIN <= y <= ARENA_Y_MAX

def clamp_cell(cell: tuple[int, int]) -> tuple[int, int]:
    return (
        max(ARENA_X_MIN, min(ARENA_X_MAX, cell[0])),
        max(ARENA_Y_MIN, min(ARENA_Y_MAX, cell[1]))
    )

def manhattan(cell1: tuple[int, int], cell2: tuple[int, int]) -> int:
    return abs(cell1[0] - cell2[0]) + abs(cell1[1] - cell2[1])

def add(cell: tuple[int, int], direction: tuple[int, int]) -> tuple[int, int]:
    return (cell[0] + direction[0], cell[1] + direction[1])

def normalize_dir(direction: tuple[int, int]) -> tuple[int, int]:
    """
    Returns unit direction based on absolute magnitude.
    Prefers horizontal over vertical if tied (matching Unity Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
    """
    dx, dy = direction
    if abs(dx) >= abs(dy) and dx != 0:
        return (1 if dx > 0 else -1, 0)
    if dy != 0:
        return (0, 1 if dy > 0 else -1)
    return (0, 0)
