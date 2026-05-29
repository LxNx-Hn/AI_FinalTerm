import sys, os; sys.path.append(os.path.dirname(os.path.dirname(__file__)))
from boss_env import BossEnv
def test_boss_visibility_hurtbox():
    env = BossEnv(seed=0)
    env.step(0)
    assert env.boss_visible_flag == True
