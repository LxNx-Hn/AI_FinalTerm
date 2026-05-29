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
    
    # In normal mark dash, display is horizontal iff actual is horizontal
    # Since display is mark_horizontal5/vertical5, its len is 5.
    # actual_cells is stripe3, which is 3 thick, width=7 (21 cells).
    assert len(warning.warning_cells) == 5
    assert len(damage.damage_cells) == 21
    
    # It should hide boss
    assert warning.hide_boss_visible == True
    assert damage.hide_after == True

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
        
        if len(warning.warning_cells) == 5 and damage.commit_start is not None and damage.commit_end is not None:
            is_display_horizontal = (warning.warning_cells[0][1] == warning.warning_cells[1][1])
            is_actual_horizontal = (damage.commit_start[1] == damage.commit_end[1])
            if is_display_horizontal != is_actual_horizontal:
                found_fake = True
                break
            
    assert found_fake, "Failed to generate a fake Mark Dash in Phase 3 after 20 tries."
