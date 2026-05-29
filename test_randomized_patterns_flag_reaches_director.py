
import pytest
from boss_env import BossEnv
import config

def test_randomized_flag_deterministic():
    config.RANDOMIZE_BOSS_PATTERNS = False
    env1 = BossEnv(seed=42)
    env2 = BossEnv(seed=42)
    assert env1.director.rng.random() == env2.director.rng.random()
