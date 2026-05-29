
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
