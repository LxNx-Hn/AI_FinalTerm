# state_encoder.py
import numpy as np
from boss_env import BossEnv
from coordinate_mapping import ARENA_X_MIN, ARENA_X_MAX, ARENA_Y_MIN, ARENA_Y_MAX
import config
from boss_patterns import PATTERN_IDS

def encode_state_dqn(env: BossEnv) -> np.ndarray:
    """
    Returns a 1D float32 numpy array representing the full state for DQN.
    """
    features = []
    
    # Player
    px = (env.player_cell[0] - ARENA_X_MIN) / (ARENA_X_MAX - ARENA_X_MIN)
    py = (env.player_cell[1] - ARENA_Y_MIN) / (ARENA_Y_MAX - ARENA_Y_MIN)
    features.extend([px, py])
    
    facing_map = {(0, 1): 0, (0, -1): 1, (-1, 0): 2, (1, 0): 3}
    facing_idx = facing_map.get(env.player_facing, 0)
    features.extend([1.0 if i == facing_idx else 0.0 for i in range(4)])
    
    features.append(env.player_hp / config.PLAYER_MAX_HP)
    features.append(env.attack_cooldown / config.ATTACK_COOLDOWN_STEPS)
    features.append(env.move_cooldown / config.MOVE_COOLDOWN_STEPS)
    
    # Boss Scalar
    features.append(1.0 if env.boss_visible_flag else 0.0)
    
    if env.boss_visible_cell is not None:
        bx = (env.boss_visible_cell[0] - ARENA_X_MIN) / (ARENA_X_MAX - ARENA_X_MIN)
        by = (env.boss_visible_cell[1] - ARENA_Y_MIN) / (ARENA_Y_MAX - ARENA_Y_MIN)
    else:
        bx, by = 0.5, 0.5
    features.extend([bx, by])
    
    features.append(env.boss_hp / config.BOSS_MAX_HP)
    
    # Spatial Maps (7x9 flattened = 63 cells each)
    width = ARENA_X_MAX - ARENA_X_MIN + 1
    height = ARENA_Y_MAX - ARENA_Y_MIN + 1
    
    warning_map = np.zeros(width * height)
    damage_map = np.zeros(width * height)
    hurtbox_map = np.zeros(width * height)
    
    def cell_to_idx(c):
        return (c[1] - ARENA_Y_MIN) * width + (c[0] - ARENA_X_MIN)
        
    if env.current_event:
        if env.current_event.kind == "warning":
            for c in env.current_event.warning_cells:
                if ARENA_X_MIN <= c[0] <= ARENA_X_MAX and ARENA_Y_MIN <= c[1] <= ARENA_Y_MAX:
                    warning_map[cell_to_idx(c)] = 1.0
        elif env.current_event.kind == "damage":
            for c in env.current_event.damage_cells:
                if ARENA_X_MIN <= c[0] <= ARENA_X_MAX and ARENA_Y_MIN <= c[1] <= ARENA_Y_MAX:
                    damage_map[cell_to_idx(c)] = 1.0
                    
    for c in env.boss_hurtbox_cells:
        if ARENA_X_MIN <= c[0] <= ARENA_X_MAX and ARENA_Y_MIN <= c[1] <= ARENA_Y_MAX:
            hurtbox_map[cell_to_idx(c)] = 1.0
            
    features.extend(warning_map)
    features.extend(damage_map)
    features.extend(hurtbox_map)
    
    return np.array(features, dtype=np.float32)

def encode_state_tabular(env: BossEnv) -> tuple:
    """
    Returns a simplified tuple representing the state for Tabular Q-learning.
    """
    px, py = env.player_cell
    
    if env.boss_visible_flag and env.boss_visible_cell:
        bx, by = env.boss_visible_cell
    else:
        bx, by = 99, 99 # Out of bounds marker
        
    in_warn = 0
    in_dmg = 0
    if env.current_event:
        if env.current_event.kind == "warning" and env.player_cell in env.current_event.warning_cells:
            in_warn = 1
        elif env.current_event.kind == "damage" and env.player_cell in env.current_event.damage_cells:
            in_dmg = 1
            
    can_attack = 1 if env.attack_cooldown <= 0 else 0
    
    return (px, py, bx, by, in_warn, in_dmg, can_attack)
