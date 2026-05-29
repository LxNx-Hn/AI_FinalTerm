import sys, os; sys.path.append(os.path.dirname(os.path.dirname(__file__)))
from boss_env import BossEnv
def test_reward_no_hint():
    env = BossEnv(seed=0)
    obs, r, d, i = env.step(0)
    assert r == -2 # step penalty
