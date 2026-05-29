
import pytest
from boss_env import BossEnv, Event

def test_dash_progress():
    env = BossEnv()
    ev = Event(0, 'damage', 2, commit_start=(0,0), commit_end=(0,2), boss_dash_active=True)
    env.current_event = ev
    
    # Tick 1: remaining goes from 2 to 1 (progress = 1/2) -> boss moves to (0,1)
    env.step(0)
    assert env.boss_logic_cell == (0,1)
    
    # Tick 2: remaining goes from 1 to 0 (progress = 2/2) -> boss moves to (0,2)
    env.step(0)
    assert env.boss_logic_cell == (0,2)
