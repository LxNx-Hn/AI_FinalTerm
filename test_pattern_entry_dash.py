import pytest
from boss_env import BossEnv
import boss_patterns
import config

def test_pattern_entry_dash():
    env = BossEnv(seed=42)
    env.reset()
    
    # Simulate additional_1a token which casts 4 PatternEntryDash
    env.director.expand_token("additional_1a")
    
    # The first event added should be PATTERN_ENTRY_DASH warning for left_edge2
    # Wait, additional_1a creates 4 dashes with gaps between them.
    # We should have 8 events (warning/damage for each) + 3 gap events = 11 events
    assert len(env.events) == 11
    
    first_event = env.events[0]
    assert first_event.pattern_id == boss_patterns.PATTERN_IDS["PATTERN_ENTRY_DASH"]
    assert first_event.kind == "warning"
    
    # Left edge cells: x=-3 to -2, y=-4 to 4. Total 18 cells.
    assert len(first_event.warning_cells) == 18
