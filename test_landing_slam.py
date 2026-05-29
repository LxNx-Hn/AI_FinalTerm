import pytest
from boss_env import BossEnv
import boss_patterns

def test_landing_slam():
    env = BossEnv(seed=42)
    env.reset()
    
    # Move player to a specific cell
    env.player_cell = (2, 2)
    
    env.director.expand_token("landing_slam")
    
    assert len(env.events) == 2
    warning = env.events[0]
    
    # The slam should center around player's cell (2, 2)
    cells = warning.warning_cells
    assert (2, 2) in cells
    assert (2, 2+3) not in cells
    assert (2+2, 2+2) not in cells # The corners are hollow
