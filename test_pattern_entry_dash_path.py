
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
