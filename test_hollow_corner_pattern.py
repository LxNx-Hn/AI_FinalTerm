import pytest
import boss_patterns

def test_hollow_corner_pattern():
    cells = boss_patterns.hollow_corner_5x5((0, 0))
    # A 5x5 has 25 cells. The 4 corners (dx=2,dy=2) are excluded.
    # So 25 - 4 = 21 cells.
    assert len(cells) == 21
    
    # Check that (2,2) is missing
    assert (2, 2) not in cells
    assert (-2, 2) not in cells
    assert (2, -2) not in cells
    assert (-2, -2) not in cells
    
    # Check that center is present
    assert (0, 0) in cells
