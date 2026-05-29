
import pytest
import config
from coordinate_mapping import is_valid_cell, clamp_cell

def test_bounds_are_separated():
    assert config.ARENA_Y_MAX == 4
    assert config.PATTERN_Y_MAX == 3
    # is_valid_cell uses movement bounds (ARENA_*)
    assert is_valid_cell(0, 4) == True
    assert clamp_cell((0, 5)) == (0, 4)
