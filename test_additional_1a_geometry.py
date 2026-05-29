
import pytest
from boss_env import BossEnv

def test_additional_1a_geometry():
    env = BossEnv(seed=42)
    env.director.expand_token("additional_1a")
    assert "Additional1A" in env.director.executed_sequences
