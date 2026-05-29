import sys, os
sys.path.append(os.path.dirname(os.path.dirname(__file__)))
from boss_env import BossEnv
import config

def test_boss_chase():
    env = BossEnv(seed=0)
    # Boss starts at (0, 1), Player starts at (0, -3)
    # Distance is 4.
    
    # Check initial boss state
    assert env.boss_logic_cell == (0, 1)
    assert env.boss_visible_cell == (0, 1)
    assert env.boss_hurtbox_cells == [(0, 1)]
    assert env.boss_visible_flag == True

    # First step triggers basic_rep_step (boss chase)
    # It schedules a MOVE event taking BOSS_MOVE_ONE_CELL_STEPS
    # The move commits instantly logically? In boss_env:
    # "commit_start=self.boss_logic_cell" and boss_logic_cell is updated.
    env.step(0) 
    
    # After 1 step (which starts the move event), boss logic cell is updated
    assert env.boss_logic_cell == (0, 0), "Boss should move 1 cell towards player (0, -3)"
    assert env.boss_visible_cell == (0, 0)
    assert env.boss_hurtbox_cells == [(0, 0)]
    
    # Wait for the move event to finish (remaining steps: BOSS_MOVE_ONE_CELL_STEPS - 1)
    for _ in range(config.BOSS_MOVE_ONE_CELL_STEPS - 1):
        env.step(0)
        
    # The move is finished. The next step should trigger another basic_rep_step
    # because player is still at (0, -3), boss is at (0, 0).
    env.step(0)
    assert env.boss_logic_cell == (0, -1)
    
    # Wait for this move to finish
    for _ in range(config.BOSS_MOVE_ONE_CELL_STEPS - 1):
        env.step(0)
        
    # Now boss is at (0, -1), player is at (0, -3). Distance is 2.
    # Next step triggers another move to (0, -2).
    env.step(0)
    assert env.boss_logic_cell == (0, -2)
    
    # Wait for this move to finish
    for _ in range(config.BOSS_MOVE_ONE_CELL_STEPS - 1):
        env.step(0)
        
    # Now boss is at (0, -2), player is at (0, -3).
    # Manhattan distance is exactly 1!
    # The while loop breaks, boss should cast BasicAttack next!
    env.step(0)
    
    # The next event should be a warning for the basic scratch.
    # Boss should NOT move to (0, -3).
    assert env.boss_logic_cell == (0, -2), "Boss should stop 1 cell in front of player"
    assert env.current_event.kind == "warning"
    assert env.current_event.pattern_id == 3 # BASIC_SCRATCH
    
    # Attack happens AFTER movement.
    # Boss logic cell, visible cell, and hurtbox remained together during the chase.
    assert env.boss_visible_cell == (0, -2)
    assert env.boss_hurtbox_cells == [(0, -2)]
