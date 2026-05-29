# player_rules.py
from coordinate_mapping import add

def get_player_attack_cells(origin: tuple[int, int], direction: tuple[int, int]) -> list[tuple[int, int]]:
    """
    Replicates Unity PlayerCombat GetAttackCells logic.
    2x3 rectangle in front of the player.
    """
    result = []
    side = (-direction[1], direction[0])
    
    for forward in range(1, 3):
        for offset in range(-1, 2):
            cx = origin[0] + direction[0] * forward + side[0] * offset
            cy = origin[1] + direction[1] * forward + side[1] * offset
            result.append((cx, cy))
            
    return result
