
import pytest
from boss_env import BossEnv, Event
from boss_patterns import PATTERN_IDS

def test_boss_movement_during_dash():
    env = BossEnv(seed=42)
    env.pattern_tokens = ["gap"]
    env.events = []
    
    start_cell = (0, 0)
    end_cell = (0, 3)
    path = [(0,0), (0,1), (0,2), (0,3)]
    
    # Assert before dash
    assert env.boss_logic_cell == (0, 1)
    
    # Warning event
    ev_warn = Event(PATTERN_IDS["PATTERN_ENTRY_DASH"], "warning", 2, warning_cells=path)
    # Damage event (Dash)
    ev_dmg = Event(PATTERN_IDS["PATTERN_ENTRY_DASH"], "damage", 2, damage_cells=path, commit_start=start_cell, commit_end=end_cell, dash_path_cells=path, boss_dash_active=True, hide_after=False)
    
    env.events.extend([ev_warn, ev_dmg])
    
    env.step(0) # Trigger Warning
    assert env.boss_logic_cell == (0, 1) # warning  boss_logic_cell
    
    env.step(0)
    env.step(0) # Trigger Damage tick 1 (remaining = 2 -> 1, progress = 0.5)
    assert env.boss_logic_cell == (0, 2)
    
    env.step(0) # Trigger Damage tick 2 (remaining = 1 -> 0, progress = 1.0)
    assert env.boss_logic_cell == end_cell
    assert env.boss_hurtbox_cells == [end_cell]
    
    env.step(0)
    env.step(0) # End Damage
    
    # dash ���� �� boss_logic_cell == expected_end_cell
    assert env.boss_logic_cell == end_cell
