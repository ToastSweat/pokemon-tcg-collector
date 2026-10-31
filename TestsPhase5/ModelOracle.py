"""Independent Python diagnostic oracle, never used to produce accepted C# pack results."""
from collections import Counter, defaultdict


def inclusion_marginals(weights, draws):
    """Per-position target probabilities, grouping equal weights; target-exclusion DP.

    State counts removed OTHER identities; target remains in every denominator.
    Each position's missing outgoing mass is exactly target selection there.
    """
    if type(draws) is not int or draws < 1 or draws > len(weights) or any(type(w) is not int or w <= 0 for w in weights):
        raise ValueError('Invalid weighted-WOR population/draw count')
    groups = sorted(Counter(weights).items())
    total = sum(weights)
    result = {}
    for target in sorted(set(weights)):
        other = [n - (1 if w == target else 0) for w, n in groups]
        states = {(0,) * len(groups): 1.0}
        positions = []
        for _ in range(draws):
            next_states = defaultdict(float)
            selected = 0.0
            for removed, mass in states.items():
                remaining = total - sum(r * w for r, (w, _) in zip(removed, groups))
                selected += mass * target / remaining
                for j, (weight, _) in enumerate(groups):
                    count = other[j] - removed[j]
                    if count:
                        next_state = list(removed)
                        next_state[j] += 1
                        next_states[tuple(next_state)] += mass * count * weight / remaining
            positions.append(selected)
            states = next_states
        result[target] = positions
    return result



def fixed_diagnostic(catalog, rule, seed):
    state = seed
    draws = []
    def bounded(bound):
        nonlocal state
        limit = 0xffffffff - 0xffffffff % bound
        while True:
            state = (state ^ (state << 13)) & 0xffffffff
            state ^= state >> 17
            state = (state ^ (state << 5)) & 0xffffffff
            value = state - 1
            if value < limit:
                value %= bound
                draws.append(dict(bound=bound, ticket=value))
                return value
    used = {p['id']: set() for p in rule['pools']}
    pools = {p['id']: p for p in rule['pools']}
    ids = []
    for slot in rule['slots']:
        pool_id = slot['poolId']
        if slot['alternativePoolId'] and bounded(slot['alternativeDenominator']) < slot['alternativeNumerator']:
            pool_id = slot['alternativePoolId']
        pool = pools[pool_id]
        entries = [e for e in pool['entries'] if e['cardId'] not in used[pool_id]]
        ticket = bounded(sum(e['weight'] for e in entries))
        for entry in entries:
            ticket -= entry['weight']
            if ticket < 0:
                ids.append(entry['cardId'])
                if pool['uniqueWithinPack']:
                    used[pool_id].add(entry['cardId'])
                break
    return dict(seed=seed, packIndex=0, modelId=rule['modelId'], modelVersion=rule['modelVersion'], catalogVersion=catalog['catalogVersion'], printingId=rule['printingId'], status=rule['status'], rngAlgorithm=rule['rngAlgorithm'], orderedCardIds=ids, draws=draws)
