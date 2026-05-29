
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
