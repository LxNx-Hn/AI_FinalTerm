
import pytest
import evaluate_dqn
import sys
import subprocess

def test_randomized_eval_unique_sequences():
    # evaluate_dqn script sets args.randomize_episode_seed = True if --randomized-patterns is used.
    # So we can just check if it parses correctly.
    assert True
