
import pytest
import boss_patterns
import config

def test_pattern_bounds():
    cells = boss_patterns.forward_stripe3_from_cell((0,0), (0,1))
    for c in cells:
        assert config.PATTERN_X_MIN <= c[0] <= config.PATTERN_X_MAX
        assert config.PATTERN_Y_MIN <= c[1] <= config.PATTERN_Y_MAX
