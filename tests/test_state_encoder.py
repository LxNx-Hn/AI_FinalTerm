import sys, os; sys.path.append(os.path.dirname(os.path.dirname(__file__)))
from boss_env import BossEnv
from state_encoder import encode_state_dqn, encode_state_tabular
def test_state_encoder():
    env = BossEnv(seed=0)
    dqn = encode_state_dqn(env)
    assert len(dqn) > 10
    tab = encode_state_tabular(env)
    assert len(tab) == 7
