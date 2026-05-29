import sys, os; sys.path.append(os.path.dirname(os.path.dirname(__file__)))
from player_rules import get_player_attack_cells
def test_attack_range():
    cells = get_player_attack_cells((0,0), (0,1))
    assert len(cells) == 6
    assert (0, 1) in cells
