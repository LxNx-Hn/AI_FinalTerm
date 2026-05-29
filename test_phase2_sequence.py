
import pytest
from boss_env import BossEnv

def test_phase2_sequence():
    env = BossEnv(seed=42)
    env.director.current_phase = 2
    env.director.expand_token("phase2_first_fixed")
    assert "Phase2FirstFixed" in env.director.executed_sequences
