# config.py

# Game Rules
ARENA_X_MIN, ARENA_X_MAX = -3, 3
ARENA_Y_MIN, ARENA_Y_MAX = -4, 4

# Boss Pattern Cast Bounds (Inside of Arena)
PATTERN_X_MIN, PATTERN_X_MAX = -3, 3
PATTERN_Y_MIN, PATTERN_Y_MAX = -3, 3

PLAYER_START_CELL = (0, -3)
PLAYER_START_FACING = (0, 1) # UP
PLAYER_MAX_HP = 3

BOSS_START_CELL = (0, 1)
BOSS_START_FACING = (0, -1) # DOWN
BOSS_MAX_HP = 60

# Random Boss Patterns
# If False, BossDirector will use a fixed seed per episode to generate deterministic pattern sequences.
# If True, BossDirector will use varying seeds across episodes for generalized evaluation.
RANDOMIZE_BOSS_PATTERNS = False

# Timings (converted to steps assuming some ticks per second)
TICKS_PER_SECOND = 60 # Unity usually runs at 60fps, we define our MDP step rate. 
# We'll run the MDP at 10 ticks per second (0.1s per step) to keep state space manageable, or 30?
# Let's use 10 steps/sec for MDP.
TICK_RATE = 10 

def seconds_to_steps(seconds: float) -> int:
    return max(1, int(seconds * TICK_RATE))

# Player Timings
ATTACK_COOLDOWN_SECONDS = 0.5
ATTACK_COOLDOWN_STEPS = seconds_to_steps(ATTACK_COOLDOWN_SECONDS)
MOVE_COOLDOWN_SECONDS = 0.15 # Unity GridMover time
MOVE_COOLDOWN_STEPS = seconds_to_steps(MOVE_COOLDOWN_SECONDS)

# Boss Timings (from ElevatorBossController)
BOSS_MOVE_ONE_CELL_SECONDS = 0.3
BOSS_MOVE_ONE_CELL_STEPS = seconds_to_steps(BOSS_MOVE_ONE_CELL_SECONDS)

NORMAL_SCRATCH_WARNING_SECONDS = 0.5
NORMAL_SCRATCH_DAMAGE_SECONDS = 0.15
NORMAL_SCRATCH_RECOVERY_SECONDS = 0.5
PATTERN_ENTRY_WARNING_SECONDS = 0.70
PATTERN_ENTRY_DAMAGE_SECONDS = 0.15
PATTERN_STEP_GAP_SECONDS = 0.5

# Boss Phases
PHASE1 = 1
PHASE2 = 2
PHASE3 = 3
FINAL = 4

def get_phase_for_hp(hp: int) -> int:
    if hp <= 6: # 10%
        return FINAL
    if hp <= 24: # 40%
        return PHASE3
    if hp <= 42: # 70%
        return PHASE2
    return PHASE1

# Reward Definition (Natural Discovery)
REWARDS = {
    "boss_hp_damage_reward": 25,
    "step_penalty": -2,
    "warning_tile_penalty": -0.5,
    "next_damage_risk_penalty": -1,
    "current_damage_tile_penalty": -4,
    "hit_penalty": -40,
    "death_penalty": -700,
    "missed_attack_penalty": -2,
    "invalid_move_penalty": -3
}

def calculate_boss_kill_reward(remaining_time_steps: int) -> float:
    return 600.0 + 2.0 * remaining_time_steps

# RL config
SEED = 42
MAX_STEPS = 1200 # 2 minutes at 10 ticks/sec
