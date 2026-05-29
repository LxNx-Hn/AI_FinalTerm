
import pytest
from boss_env import BossEnv, Event
import config

def test_dash_hurtbox():
    env = BossEnv()
    ev = Event(0, 'damage', 2, commit_start=(0,0), commit_end=(0,2), boss_dash_active=True, dash_path_cells=[(0,0), (0,1), (0,2)])
    env.current_event = ev
    env.step_count = 0
    # On first tick of damage
    env.step(0)
    assert len(env.boss_hurtbox_cells) == 1
    assert env.boss_hurtbox_cells[0] == env.boss_logic_cell
