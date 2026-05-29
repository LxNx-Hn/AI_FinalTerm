import sys, os; sys.path.append(os.path.dirname(os.path.dirname(__file__)))
from action_space import decode_action
def test_action_space():
    assert decode_action(0) == ('NONE', False)
    assert decode_action(1) == ('UP', False)
    assert decode_action(5) == ('NONE', True)
    assert decode_action(6) == ('UP', True)
