import pytest
from boss_env import BossEnv
import boss_patterns

def test_mark_dash_pattern():
    env = BossEnv(seed=42)
    env.reset()
    env.director.current_phase = 2 # No fake in Phase 2
    
    # We will test normal mark dash
    env.director.expand_token("mark_dash")
    
    assert len(env.events) == 2 # warning and damage
    warning = env.events[0]
    damage = env.events[1]
    
    # In normal mark dash, actual cells == display cells
    assert warning.warning_cells == damage.damage_cells
    
    # It should hide boss
    assert warning.hide_boss_visible == True
    assert damage.hide_boss_visible == True

def test_fake_dash_pattern():
    env = BossEnv(seed=42)
    env.reset()
    env.director.current_phase = 3 # Fake allowed
    
    # Since fake is 50%, run multiple times to guarantee at least one fake
    found_fake = False
    for _ in range(20):
        env.events.clear()
        env.director.expand_token("mark_dash")
        warning = env.events[0]
        damage = env.events[1]
        
        if warning.warning_cells != damage.damage_cells:
            found_fake = True
            break
            
    assert found_fake, "Failed to generate a fake Mark Dash in Phase 3 after 20 tries."
