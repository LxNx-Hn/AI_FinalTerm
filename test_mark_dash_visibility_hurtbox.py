
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
