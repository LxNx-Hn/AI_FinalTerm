
import pytest
from boss_env import BossEnv

def test_run_two_additional_no_duplicate():
    env = BossEnv(seed=42)
    env.director.current_phase = 1
    env.director.expand_token("run_two_additional")
    
    # Tokens should be: run_additional_X, gap, run_additional_Y
    t1 = env.pattern_tokens[0]
    t2 = env.pattern_tokens[2]
    assert t1 != t2
    assert env.pattern_tokens[1] == "gap"
