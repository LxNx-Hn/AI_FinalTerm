
import pytest
from boss_env import BossEnv

def test_phase3_sequence():
    env = BossEnv(seed=42)
    env.director.current_phase = 3
    env.director.expand_token("phase3_first_fixed")
    assert "Phase3FirstFixed" in env.director.executed_sequences
