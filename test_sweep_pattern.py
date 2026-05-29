import pytest
from boss_env import BossEnv
import boss_patterns
import config

def test_sweep_pattern():
    env = BossEnv(seed=42)
    env.reset()
    
    # Phase2SweepOnly casts 4 bands
    env.director.expand_token("phase2_sweep_only")
    
    # Should result in 8 events (warning/damage for each band) + 3 gap events = 11 events
    assert len(env.events) == 11
    
    # Each band has 4 * 9 = 36 cells (since arena is 7x9)
    # Actually arena X is -3 to 3 (7 cells), Y is -4 to 4 (9 cells)
    # Left/Right bands: 4 columns x 9 rows = 36 cells.
    # Top/Bottom bands: 7 columns x 4 rows = 28 cells.
    # Let's just check the length of the first warning
    w1 = env.events[0]
    assert len(w1.warning_cells) in [36, 28]
