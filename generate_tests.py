import os

tests = {
"test_boss_movement_during_dash.py": """
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
    assert env.boss_logic_cell == (0, 0)
    
    # Warning event
    ev_warn = Event(PATTERN_IDS["PATTERN_ENTRY_DASH"], "warning", 2, warning_cells=path)
    # Damage event (Dash)
    ev_dmg = Event(PATTERN_IDS["PATTERN_ENTRY_DASH"], "damage", 2, damage_cells=path, commit_start=start_cell, commit_end=end_cell, dash_path_cells=path, boss_dash_active=True, hide_after=False)
    
    env.events.extend([ev_warn, ev_dmg])
    
    env.step(0) # Trigger Warning
    assert env.boss_logic_cell == (0, 0) # warning 중 boss_logic_cell
    
    env.step(0)
    env.step(0) # Trigger Damage
    
    # damage/dash active 중 boss_logic_cell 변화
    assert env.boss_logic_cell == end_cell
    assert env.boss_hurtbox_cells == path
    
    env.step(0)
    env.step(0) # End Damage
    
    # dash 종료 후 boss_logic_cell == expected_end_cell
    assert env.boss_logic_cell == end_cell
""",

"test_pattern_entry_dash_path.py": """
import pytest
from boss_env import BossEnv

def test_pattern_entry_dash_path():
    env = BossEnv(seed=42)
    env.director.expand_token("pattern_entry_dash")
    # Verify events
    assert len(env.events) > 0
    ev = env.events[1] # damage event
    assert ev.kind == "damage"
    assert ev.boss_dash_active == True
""",

"test_phase1_first_fixed_sequence.py": """
import pytest
from boss_env import BossEnv

def test_phase1_first_fixed():
    env = BossEnv(seed=42)
    env.director.expand_token("phase1_first_fixed")
    assert "Phase1FirstFixed" in env.director.executed_sequences
    assert env.pattern_tokens[-1].startswith("run_additional") # Should push additional
""",

"test_phase2_sequence.py": """
import pytest
from boss_env import BossEnv

def test_phase2_sequence():
    env = BossEnv(seed=42)
    env.director.current_phase = 2
    env.director.expand_token("phase2_first_fixed")
    assert "Phase2FirstFixed" in env.director.executed_sequences
""",

"test_phase3_sequence.py": """
import pytest
from boss_env import BossEnv

def test_phase3_sequence():
    env = BossEnv(seed=42)
    env.director.current_phase = 3
    env.director.expand_token("phase3_first_fixed")
    assert "Phase3FirstFixed" in env.director.executed_sequences
""",

"test_run_two_additional_patterns_no_duplicate.py": """
import pytest
from boss_env import BossEnv

def test_run_two_additional_no_duplicate():
    env = BossEnv(seed=42)
    env.director.current_phase = 1
    env.director.expand_token("run_two_additional_patterns")
    
    # Tokens should be: run_additional_X, gap, run_additional_Y
    t1 = env.pattern_tokens[0]
    t2 = env.pattern_tokens[2]
    assert t1 != t2
    assert env.pattern_tokens[1] == "gap"
""",

"test_additional_1a_geometry.py": """
import pytest
from boss_env import BossEnv

def test_additional_1a_geometry():
    env = BossEnv(seed=42)
    env.director.expand_token("additional_1a")
    assert "Additional1A" in env.director.executed_sequences
""",

"test_mark_dash_visibility_hurtbox.py": """
import pytest
from boss_env import BossEnv

def test_mark_dash_visibility_hurtbox():
    env = BossEnv(seed=42)
    env.director.current_phase = 3
    env.director.expand_token("mark_dash")
    
    # Just checking the events created
    warn_ev = env.events[0]
    dmg_ev = env.events[1]
    
    assert warn_ev.hide_boss_visible == True
    assert dmg_ev.hide_after == True
""",

"test_boss_visibility_hurtbox.py": """
import pytest
from boss_env import BossEnv, Event
from boss_patterns import PATTERN_IDS

def test_boss_visibility_hurtbox():
    env = BossEnv(seed=42)
    ev = Event(PATTERN_IDS["GAP"], "warning", 2, hide_boss_visible=True)
    env.events.append(ev)
    env.step(0)
    assert env.boss_visible_flag == False
    assert len(env.boss_hurtbox_cells) > 0 # hurtbox remains
""",

"test_damage_timing.py": """
import pytest
from boss_env import BossEnv, Event
from boss_patterns import PATTERN_IDS

def test_damage_timing():
    env = BossEnv(seed=42)
    env.player_cell = (0,0)
    env.player_hp = 100
    ev = Event(PATTERN_IDS["GAP"], "damage", 5, damage_cells=[(0,0)])
    env.events.append(ev)
    
    env.step(0)
    assert env.player_hp == 99
    env.step(0)
    assert env.player_hp == 99 # single hit limit
""",

"test_pattern_sequence_logging.py": """
import pytest
from boss_env import BossEnv

def test_pattern_sequence_logging():
    env = BossEnv(seed=42)
    env.director.log_pattern("TestPattern")
    assert "TestPattern" in env.director.executed_sequences
"""
}

for name, content in tests.items():
    with open(name, "w") as f:
        f.write(content)
print("Tests generated.")
