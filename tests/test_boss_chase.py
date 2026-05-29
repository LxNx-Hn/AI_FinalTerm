import sys, os; sys.path.append(os.path.dirname(os.path.dirname(__file__)))
from boss_env import BossEnv
def test_boss_chase():
    import config
    env = BossEnv(seed=0)
    env.step(0)
    assert env.boss_logic_cell == (0, 0)
    for _ in range(config.BOSS_MOVE_ONE_CELL_STEPS):
        env.step(0)
    assert env.boss_logic_cell == (0, -1)
