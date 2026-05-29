import pytest
from boss_env import BossEnv
import config

def test_random_pattern_selection():
    # If we use different seeds, the choices for run_two_additional should differ.
    env1 = BossEnv(seed=42)
    env1.reset()
    
    # Fast forward to run_two_additional
    env1.director.current_phase = 1
    env1.director.expand_token("run_two_additional")
    seq1 = env1.director.executed_sequences.copy()
    
    env2 = BossEnv(seed=99)
    env2.reset()
    env2.director.current_phase = 1
    env2.director.expand_token("run_two_additional")
    seq2 = env2.director.executed_sequences.copy()
    
    # Depending on the seeds, they should be different sequences or identical if we got very lucky.
    # But let's test a known divergence.
    assert seq1 != seq2, "Random seeds did not produce different pattern sequences."
