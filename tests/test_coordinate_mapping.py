import sys, os; sys.path.append(os.path.dirname(os.path.dirname(__file__)))
from coordinate_mapping import *
def test_coordinate_mapping():
    assert is_valid_cell(0, 0) == True
    assert is_valid_cell(-4, 0) == False
    assert clamp_cell((-5, 5)) == (-3, 4)
    assert manhattan((0,0), (2,3)) == 5
    assert normalize_dir((3, 2)) == (1, 0)
    assert normalize_dir((2, 3)) == (0, 1)
