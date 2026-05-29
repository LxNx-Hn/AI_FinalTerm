import pytest
from boss_env import BossEnv
import config

def test_phase_thresholds():
    # Force boss HP to cross thresholds and see if phase changes
    env = BossEnv(seed=42)
    env.reset()
    
    # Initally Phase 1
    env.director.evaluate_phase()
    assert env.director.current_phase == 1
    
    # Phase 2 (70%)
    env.boss_hp = config.BOSS_MAX_HP * 0.69
    env.director.evaluate_phase()
    assert env.director.current_phase == 2
    assert env.director.phase2_triggered
    
    # Phase 3 (40%)
    env.boss_hp = config.BOSS_MAX_HP * 0.39
    env.director.evaluate_phase()
    assert env.director.current_phase == 3
    assert env.director.phase3_triggered
    
    # Final Phase (10%)
    env.boss_hp = config.BOSS_MAX_HP * 0.09
    env.director.evaluate_phase()
    assert env.director.current_phase == 4
    assert env.director.final_triggered
