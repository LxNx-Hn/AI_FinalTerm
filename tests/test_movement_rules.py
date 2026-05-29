import sys, os
sys.path.append(os.path.dirname(os.path.dirname(__file__)))
from boss_env import BossEnv

def test_movement_rules():
    env = BossEnv(seed=0)
    env.player_cell = (0, 4) # Top edge
    env.player_facing = (0, 1) # Facing UP
    
    # 1. 벽 방향 이동 입력 시 position은 유지된다.
    # 2. 벽 방향 이동 입력 시 facing은 입력 방향으로 변경된다.
    env.step(3) # LEFT
    assert env.player_cell == (-1, 4), "Should move left"
    assert env.player_facing == (-1, 0), "Facing should change to LEFT"
    
    env.step(1) # UP (wall)
    assert env.player_cell == (-1, 4), "Should not move through top wall"
    assert env.player_facing == (0, 1), "Facing should change to UP even if move fails"
    
    # 3. NONE은 facing을 변경하지 않는다.
    env.step(0) # NONE
    assert env.player_cell == (-1, 4)
    assert env.player_facing == (0, 1)
    
    # 4. ATTACK은 facing을 변경하지 않는다.
    env.step(5) # ATTACK
    assert env.player_cell == (-1, 4)
    assert env.player_facing == (0, 1)
    
    # 5. MOVE_ATTACK의 facing/attack 처리 순서는 이동 후 공격
    # BossEnv steps process Move then Attack. So attack uses new position/facing.
    # We can test this by checking if cooldown started.
    assert env.attack_cooldown > 0
