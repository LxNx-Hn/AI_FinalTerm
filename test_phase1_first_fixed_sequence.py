
import pytest
from boss_env import BossEnv

def test_phase1_first_fixed():
    env = BossEnv(seed=42)
    env.director.expand_token("phase1_first_fixed")
    assert "Phase1FirstFixed" in env.director.executed_sequences
    assert env.pattern_tokens[0].startswith("run_additional") # Should push additional
