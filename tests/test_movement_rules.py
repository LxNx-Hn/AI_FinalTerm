import sys, os; sys.path.append(os.path.dirname(os.path.dirname(__file__)))
from boss_env import BossEnv
def test_movement_rules():
    env = BossEnv(seed=0)
    env.player_cell = (0, 4)
    env.player_facing = (0, 1)
    env.step(1)
    assert env.player_cell == (0, 4)
    assert env.player_facing == (0, 1)
