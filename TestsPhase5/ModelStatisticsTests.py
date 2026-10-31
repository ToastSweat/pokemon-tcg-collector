"""Numerical oracle tests: exhaustive small populations before real weighted-WOR expectations."""
from fractions import Fraction
from itertools import permutations
from pathlib import Path
import json
import unittest
import ModelOracle as oracle
ROOT = Path(__file__).resolve().parents[1]


class ModelStatisticsTests(unittest.TestCase):
    def test_weight_group_dp_matches_exhaustive_small_unequal_weight_oracle(self):
        self.assertTrue(hasattr(oracle, 'inclusion_marginals'), 'Missing actual weighted-without-replacement DP oracle')
        for weights in [[1, 2, 2, 4], [1, 1, 3], [2, 5, 7, 7, 7]]:
            for draws in range(1, len(weights) + 1):
                expected = [[Fraction(0) for _ in range(draws)] for _ in weights]
                for order in permutations(range(len(weights)), draws):
                    mass = Fraction(1)
                    remaining = sum(weights)
                    for at in order:
                        mass *= Fraction(weights[at], remaining)
                        remaining -= weights[at]
                    for slot, at in enumerate(order):
                        expected[at][slot] += mass
                actual = oracle.inclusion_marginals(weights, draws)
                for index, weight in enumerate(weights):
                    for slot in range(draws):
                        self.assertAlmostEqual(float(expected[index][slot]), actual[weight][slot], places=13)
                self.assertAlmostEqual(draws, sum(sum(actual[w]) for w in weights), places=13)
        actual = oracle.inclusion_marginals([1, 2, 2, 4], 3)
        self.assertGreater(abs(sum(actual[4]) - 3 * 4 / 9), 0.01, 'Naive sheet-share is not an inclusion oracle')

    def test_actual_pools_conserve_slot_and_inclusion_probability(self):
        self.assertTrue(hasattr(oracle, 'inclusion_marginals'), 'Missing actual weighted-without-replacement DP oracle')
        for set_id in ['jungle', 'fossil']:
            rule = json.loads((ROOT / ('Authoring/Phase5/ApprovedData/' + set_id + '-en-unlimited1999.rules.json')).read_text())
            for pool in rule['pools']:
                if pool['id'] not in ['C', 'U']:
                    continue
                k = 7 if pool['id'] == 'C' else 3
                weights = [e['weight'] for e in pool['entries']]
                marginals = oracle.inclusion_marginals(weights, k)
                for slot in range(k):
                    self.assertAlmostEqual(1, sum(marginals[w][slot] for w in weights), places=12)
                self.assertAlmostEqual(k, sum(sum(marginals[w]) for w in weights), places=12)
                self.assertTrue(all(0 <= sum(p) <= 1 + 1e-12 for p in marginals.values()))


if __name__ == '__main__':
    unittest.main(verbosity=2)
