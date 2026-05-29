
import pytest
from boss_env import BossEnv

def test_pattern_sequence_logging():
    env = BossEnv(seed=42)
    env.director.log_pattern("TestPattern")
    assert "TestPattern" in env.director.executed_sequences
