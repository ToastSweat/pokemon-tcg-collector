"""Reproducible standalone C# approved-model proof. Never launches Unity or opens saves."""
from pathlib import Path
import argparse
import gzip
import hashlib
import json
import math
import os
import re
import subprocess
import sys
from typing import Any
sys.dont_write_bytecode = True
from prepare_phase5_models import ROOT, APPROVED_DIR, ARTIFACT_DIR, no_reparse, read_approved, sha, effective_source_pins, publication_observations, safe_output
sys.path.insert(0, str(ROOT / 'TestsPhase5'))
from ModelOracle import fixed_diagnostic, inclusion_marginals  # pyright: ignore[reportMissingImports]

MONO = Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Data/MonoBleedingEdge')
SOURCES = ['Assets/VintageCollector/Domain/' + n for n in ['Data.cs', 'Catalog.cs', 'Random.cs', 'PackGenerator.cs']]


def tree(directory):
    no_reparse(directory)
    result = {}
    def visit(folder):
        for p in sorted(folder.iterdir()):
            # Case-insensitive pruning before stat/read/descent; never traverse live Saves or Logs.
            if p.name.casefold() in {'saves', 'logs'}:
                continue
            no_reparse(p)
            if p.is_dir():
                visit(p)
            elif p.is_file():
                result[p.relative_to(directory).as_posix()] = sha(p)
    if directory.is_dir():
        visit(directory)
    return result


def review_snapshot():
    review = json.loads((ROOT / '.artifacts/phase5-review/pins.json').read_text(encoding='utf-8'))
    return {name: dict(reviewedLiveSha256=record['live'], currentLiveSha256=sha(ROOT / name)) for name, record in review.items()}


def preservation() -> dict[str, Any]:
    return dict(players={p.name: tree(p) for p in sorted((ROOT / 'Builds').iterdir()) if p.name.casefold() not in {'saves', 'logs'} and p.is_dir()},
                source={n: tree(ROOT / n) for n in ['Assets', 'Packages', 'ProjectSettings']},
                originals={n: tree(ROOT / n) for n in ['Pokemon-Base-Set', 'Pokemon Cries']},
                savesAndLogsPrunedBeforeTraversal=True)


def write_json(path, value):
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False, allow_nan=False) + '\n', encoding='utf-8')


def run(command, out):
    env = dict(os.environ, TEMP=str(out), TMP=str(out), TMPDIR=str(out), PYTHONDONTWRITEBYTECODE='1')
    p = subprocess.run(command, cwd=ROOT, env=env, capture_output=True, text=True, timeout=300)
    result = dict(command=command, exitCode=p.returncode, stdout=p.stdout, stderr=p.stderr)
    print(p.stdout, end=''); print(p.stderr, end='')
    return result


def checked_run(command, out, stage, runs):
    if not re.fullmatch('[A-Za-z0-9_-]+', stage):
        raise ValueError('Unsafe command stage')
    result = run(command, out)
    runs.append(result)
    write_json(out / ('step-' + stage + '.json'), result)
    if result['exitCode'] != 0:
        raise ValueError(stage + ' process failed with exit ' + str(result['exitCode']))
    return result


def accept_execution(execution, proof_path, docs):
    if execution['exitCode'] != 0:
        raise ValueError('Runtime process failed')
    matches = re.findall(r'^CHECKS ([0-9]+) FAILED ([0-9]+)$', execution['stdout'], re.MULTILINE)
    if len(matches) != 1 or int(matches[0][0]) <= 0 or int(matches[0][1]) != 0:
        raise ValueError('Runtime stdout has zero, missing or failed checks')
    try:
        proof = json.loads(proof_path.read_text(encoding='utf-8'))
    except (OSError, json.JSONDecodeError) as error:
        raise ValueError('Missing/malformed C# proof JSON') from error
    count = validate_proof(proof, docs)
    if count != int(matches[0][0]):
        raise ValueError('Runtime stdout and proof declared counts disagree')
    return proof


def validate_proof(proof, docs):
    if type(proof.get('checkCount')) is not int or proof['checkCount'] <= 0 or proof['checkCount'] != len(proof['checks']) or len(set(proof['checks'])) != proof['checkCount'] or type(proof.get('failedCount')) is not int or proof['failedCount'] != 0:
        raise ValueError('C# proof has failed, zero or inconsistent checks')
    if len(proof['sets']) != 2:
        raise ValueError('Missing set proof')
    if [s['printingId'] for s in proof['sets']] != [s + '-en-unlimited1999' for s in ['jungle', 'fossil']]:
        raise ValueError('Wrong set proof identity')
    for result in proof['sets']:
        printing = result['printingId']
        if result['passed'] is not True or [d['seed'] for d in result['fixedDiagnostics']] != [1, 2, 3, 42, 0xffffffff, 0x4a554e47, 0x464f5353]:
            raise ValueError('Missing/distorted fixed diagnostic seed matrix')
        catalog, rule = docs[printing + '.catalog.json'], docs[printing + '.rules.json']
        for d in result['fixedDiagnostics']:
            if d != fixed_diagnostic(catalog, rule, d['seed']):
                raise ValueError('C# complete ordered diagnostic disagrees with independent Python oracle')
    return proof['checkCount']


SEEDS = {'jungle-en-unlimited1999': 0x4a554e47, 'fossil-en-unlimited1999': 0x464f5353}


def experiment_policy(packs):
    return dict(statisticalTrialsPerSet=packs, seeds=SEEDS,
                sampling='One continuous nonzero-seeded production xorshift32-v1 stream per set; generator resets pack-scoped uniqueness each pack. No wrapper draws.',
                replay='Identical second C# execution is deterministic replay only; zero additional statistical trials.',
                rng='Unsigned (13,17,5); raw support 1..2^32-1; v=raw-1; reject v>=M-M%bound for M=2^32-1; bound-one calls consume state.',
                drawOrder='7 C weighted tickets, branch ticket in [0,3), one selected H/R ticket, 3 U weighted tickets; ordered source entries; whole identity removed for C/U.',
                tolerance='abs(actual - n*p) <= 6*sqrt(n*p*(1-p)) + 2; predeclared deterministic regression guard, not factory confidence/independence evidence.',
                expectation='Target-exclusion weight-group DP for C/U inclusion and position probabilities; exact branch times weighted share for H/R, also conditional branch checks.',
                factoryAccuracyClaim=False, physicalOrderAuthenticated=False, boxCollation=False)


def expectations(docs):
    result = {}
    for printing in SEEDS:
        per_card = {}
        for pool in docs[printing + '.rules.json']['pools']:
            pool_id = pool['id']
            weights = [e['weight'] for e in pool['entries']]
            k = 7 if pool_id == 'C' else 3 if pool_id == 'U' else 1
            marginals = inclusion_marginals(weights, k) if pool_id in ['C', 'U'] else None
            for entry in pool['entries']:
                positions = [0.0] * 11
                conditional = None
                if marginals is not None:
                    offset = 0 if pool_id == 'C' else 8
                    positions[offset:offset + k] = marginals[entry['weight']]
                else:
                    conditional = entry['weight'] / sum(weights)
                    positions[7] = conditional * (1/3 if pool_id == 'H' else 2/3)
                per_card[entry['cardId']] = dict(poolId=pool_id, weight=entry['weight'], inclusionProbability=sum(positions), positionProbabilities=positions, conditionalBranchProbability=conditional)
        for pos in range(11):
            if abs(sum(c['positionProbabilities'][pos] for c in per_card.values()) - 1) > 1e-12:
                raise ValueError('Expected position probability does not conserve mass')
        if abs(sum(c['inclusionProbability'] for c in per_card.values()) - 11) > 1e-12:
            raise ValueError('Expected inclusion count does not conserve mass')
        result[printing] = per_card
    return result


def statistics(sample, expected):
    n = sample['packs']
    if type(n) is not int or n != 100000 or sample['cardCount'] != 11*n or sample['holoPacks'] + sample['rarePacks'] != n:
        raise ValueError('Sample declared totals do not reconcile')
    counts = {c['cardId']: c for c in sample['counts']}
    if len(counts) != len(sample['counts']) or set(counts) != set(expected):
        raise ValueError('Sample card membership mismatch')
    checks = []
    def check(name, actual, trials, probability):
        mean = trials * probability
        sigma = math.sqrt(trials * probability * (1 - probability))
        tolerance = 6 * sigma + 2
        checks.append(dict(name=name, actual=actual, trials=trials, probability=probability, expected=mean,
                           absoluteDeviation=abs(actual-mean), sigmaUnits=abs(actual-mean)/sigma if sigma else 0.0,
                           tolerance=tolerance, passed=abs(actual-mean) <= tolerance))
    check('holo-branch', sample['holoPacks'], n, 1/3)
    for card_id, e in expected.items():
        c = counts[card_id]
        if c['poolId'] != e['poolId'] or len(c['positions']) != 11 or any(type(v) is not int or v < 0 for v in c['positions']) or c['appearances'] != sum(c['positions']):
            raise ValueError('Per-card sample counters malformed')
        check(card_id + ':inclusion', c['appearances'], n, e['inclusionProbability'])
        for position, probability in enumerate(e['positionProbabilities']):
            if probability:
                check(card_id + ':position-' + str(position+1), c['positions'][position], n, probability)
            elif c['positions'][position] != 0:
                raise ValueError('Card counted in ineligible slot')
        if e['conditionalBranchProbability'] is not None:
            check(card_id + ':conditional-' + e['poolId'], c['appearances'], sample['holoPacks' if e['poolId'] == 'H' else 'rarePacks'], e['conditionalBranchProbability'])
    if sum(c['appearances'] for c in counts.values()) != 11*n or any(sum(c['positions'][i] for c in counts.values()) != n for i in range(11)):
        raise ValueError('All sample card/position totals must reconcile')
    return dict(passed=all(c['passed'] for c in checks), checkCount=len(checks), failedCount=sum(not c['passed'] for c in checks),
                maxSigmaUnits=max(c['sigmaUnits'] for c in checks), checks=checks)


def verify_archive(folder, result, rule, parse=True):
    sample = result['sample']
    filename = rule['printingId'] + '.diagnostics.jsonl.gz'
    if sample['diagnosticsFile'] != filename or sample['seed'] != SEEDS[rule['printingId']]:
        raise ValueError('Unexpected diagnostic archive identity/seed')
    path = folder / filename
    no_reparse(path)
    digest = hashlib.sha256(); size = 0
    if not parse:
        with gzip.GzipFile(path, 'rb') as stream:
            while chunk := stream.read(1024*1024):
                size += len(chunk); digest.update(chunk)
    else:
        actual = {e['cardId']: [0]*11 for p in rule['pools'] for e in p['entries']}
        pools = {p['id']: p for p in rule['pools']}
        pool_for = {e['cardId']: p['id'] for p in rule['pools'] for e in p['entries']}
        metadata = dict(modelId=rule['modelId'], modelVersion=rule['modelVersion'], catalogVersion=rule['printingId'] + '-0.1.0', printingId=rule['printingId'], status=rule['status'], rngAlgorithm=rule['rngAlgorithm'])
        count = 0; holo = 0
        with gzip.GzipFile(path, 'rb') as stream:
            for index, line in enumerate(stream):
                digest.update(line); size += len(line)
                d = json.loads(line)
                if d['seed'] != sample['seed'] or d['packIndex'] != index or any(d[k] != v for k, v in metadata.items()) or len(d['orderedCardIds']) != 11 or len(d['draws']) != 12:
                    raise ValueError('Complete diagnostic provenance/index/shape mismatch')
                if len(set(d['orderedCardIds'][:7])) != 7 or len(set(d['orderedCardIds'][8:])) != 3:
                    raise ValueError('Archive violates pack-scoped uniqueness')
                used = {'C': set(), 'U': set(), 'R': set(), 'H': set()}; cursor = 0
                for pos, slot in enumerate(rule['slots']):
                    pool_id = slot['poolId']
                    if slot['alternativePoolId']:
                        branch = d['draws'][cursor]; cursor += 1
                        if branch['bound'] != 3 or type(branch['ticket']) is not int or not 0 <= branch['ticket'] < 3:
                            raise ValueError('Branch trace invalid')
                        if branch['ticket'] == 0:
                            pool_id = 'H'
                    pool = pools[pool_id]
                    entries = [e for e in pool['entries'] if e['cardId'] not in used[pool_id]]
                    draw = d['draws'][cursor]; cursor += 1
                    total = sum(e['weight'] for e in entries)
                    if draw['bound'] != total or type(draw['ticket']) is not int or not 0 <= draw['ticket'] < total:
                        raise ValueError('Weighted trace bound invalid after identity removal')
                    ticket = draw['ticket']; selected = None
                    for e in entries:
                        ticket -= e['weight']
                        if ticket < 0:
                            selected = e['cardId']; break
                    if d['orderedCardIds'][pos] != selected:
                        raise ValueError('Archive ordered ID disagrees with actual weighted trace')
                    if pool['uniqueWithinPack']:
                        used[pool_id].add(selected)
                    actual[selected][pos] += 1
                if pool_for[d['orderedCardIds'][7]] == 'H':
                    holo += 1
                count += 1
        if count != sample['packs'] or holo != sample['holoPacks'] or any(actual[c['cardId']] != c['positions'] for c in sample['counts']):
            raise ValueError('Archive readback disagrees with declared sample counts')
    if digest.hexdigest() != sample['completeOrderedDiagnosticSha256'] or size != sample['uncompressedBytes']:
        raise ValueError('Archive uncompressed digest/byte count mismatch')
    return dict(path=path.relative_to(ROOT).as_posix(), compressedSha256=sha(path), compressedBytes=path.stat().st_size,
                completeOrderedDiagnosticSha256=digest.hexdigest(), uncompressedBytes=size, parsedPackCount=sample['packs'] if parse else None)


def main():
    parser = argparse.ArgumentParser(__doc__)
    parser.add_argument('--label', required=True)
    parser.add_argument('--packs', type=int, choices=[0, 100000], default=100000)
    args = parser.parse_args()
    if not re.fullmatch('[A-Za-z0-9_-]+', args.label):
        parser.error('Unsafe label')
    out = safe_output(ARTIFACT_DIR / args.label)
    out.mkdir(parents=True, exist_ok=False)
    report: dict[str, Any] = dict(passed=False, domainCompiledSeparately=False, inputsUnchanged=False, preservationUnchanged=False, csharpChecks=0, sets=[], runs=[], failure=None, scope='authoring-pure-CSharp-not-native-Unity-or-playable-runtime')
    inputs = None
    protected = None
    reviewed = None
    publication_before = None
    try:
        publication_before = publication_observations()
        write_json(out / 'publication-before.json', publication_before)
        reviewed = review_snapshot()
        write_json(out / 'review-before.json', reviewed)
        report['reviewPinCount'] = len(reviewed)
        report['reviewHistoricalDrift'] = {n: v for n, v in reviewed.items() if v['reviewedLiveSha256'] != v['currentLiveSha256']}
        docs = read_approved()
        inputs = effective_source_pins()
        for p in [Path(__file__), ROOT / 'tools/prepare_phase5_models.py', *sorted((ROOT / 'TestsPhase5').glob('Model*')), *sorted(APPROVED_DIR.iterdir())]:
            if p.is_file():
                inputs[p.relative_to(ROOT).as_posix()] = sha(p)
        for p in [MONO / 'bin/mono.exe', MONO / 'lib/mono/4.5/mcs.exe']:
            if not p.is_file():
                raise ValueError('Missing Unity bundled Mono compiler/runtime: ' + str(p))
        report['toolchain'] = {str(p): sha(p) for p in [MONO / 'bin/mono.exe', MONO / 'lib/mono/4.5/mcs.exe']}
        write_json(out / 'inputs-before.json', inputs)
        protected = preservation()
        write_json(out / 'preservation-before.json', protected)
        historical = json.loads((ROOT / '.artifacts/phase5-preservation/before.json').read_text(encoding='utf-8'))
        if protected != historical:
            raise ValueError('Protected source/player/original membership or bytes differ from Phase 5 baseline')
        report['samplingPolicy'] = experiment_policy(args.packs)
        expected = expectations(docs)
        write_json(out / 'sampling-policy.json', report['samplingPolicy'])
        write_json(out / 'expectations-before-generation.json', expected)
        compiler = [str(MONO / 'bin/mono.exe'), str(MONO / 'lib/mono/4.5/mcs.exe'), '-warnaserror+']
        commands = [compiler + ['-target:library', '-out:' + str(out / 'Domain.dll')] + [str(ROOT / n) for n in SOURCES],
                    compiler + ['-r:System.Runtime.Serialization', '-r:' + str(out / 'Domain.dll'), '-out:' + str(out / 'ModelProof.exe'), str(ROOT / 'TestsPhase5/ModelProof.cs')],
                    [str(MONO / 'bin/mono.exe'), str(out / 'ModelProof.exe'), str(APPROVED_DIR), str(out), str(args.packs)]]
        result = None
        for index, command in enumerate(commands):
            result = checked_run(command, out, ['domain-compiler', 'harness-compiler', 'generation'][index], report['runs'])
            if index == 0:
                report['domainCompiledSeparately'] = True
        if result is None:
            raise ValueError('No runtime command executed')
        proof = accept_execution(result, out / 'csharp-proof.json', docs)
        report['csharpChecks'] = proof['checkCount']
        replay = out / 'deterministic-rerun'
        replay.mkdir(exist_ok=False)
        rerun = checked_run([str(MONO / 'bin/mono.exe'), str(out / 'ModelProof.exe'), str(APPROVED_DIR), str(replay), str(args.packs)], out, 'deterministic-rerun', report['runs'])
        replay_proof = accept_execution(rerun, replay / 'csharp-proof.json', docs)
        report['deterministicRerunMatched'] = proof == replay_proof
        if not report['deterministicRerunMatched']:
            raise ValueError('Complete C# rerun diagnostics/counts/digests differ')
        report['sets'] = proof['sets']
        if args.packs:
            for result in report['sets']:
                printing = result['printingId']
                rule = docs[printing + '.rules.json']
                result['archiveReadback'] = verify_archive(out, result, rule)
                result['replayArchiveDigest'] = verify_archive(replay, result, rule, parse=False)
                result['statistics'] = statistics(result['sample'], expected[printing])
                if not result['statistics']['passed']:
                    raise ValueError('Declared statistical regression tolerance failed: ' + printing)
        report['generationCounts'] = dict(statisticalPacksPerSet=args.packs, statisticalPacksTotal=2*args.packs,
                                          replayPacksTotal=2*args.packs, replayAdditionalStatisticalTrials=0,
                                          fixedSeedPacksPerExecution=14)
        report['passed'] = True
    except Exception as error:
        report['failure'] = str(error)
    finally:
        report['publicationObservationsUnchanged'] = False
        if publication_before is not None:
            publication_after = publication_observations()
            write_json(out / 'publication-after.json', publication_after)
            report['publicationObservationsUnchanged'] = publication_before == publication_after
            if not report['publicationObservationsUnchanged'] and report['failure'] is None:
                report['failure'] = 'Publication observation drift during model proof'
        if reviewed is not None:
            review_after = review_snapshot()
            write_json(out / 'review-after.json', review_after)
            report['reviewLiveUnchangedDuringRun'] = reviewed == review_after
        else:
            report['reviewLiveUnchangedDuringRun'] = False
        if inputs is not None:
            after = {n: sha(ROOT / n) for n in inputs}
            write_json(out / 'inputs-after.json', after)
            report['inputsUnchanged'] = inputs == after
        if protected is not None:
            after = preservation()
            write_json(out / 'preservation-after.json', after)
            report['preservationUnchanged'] = protected == after
            report['preservationCounts'] = {k: sum(len(v) for v in protected[k].values()) for k in ['source', 'players', 'originals']}
        report['passed'] = report['passed'] and report['inputsUnchanged'] and report['preservationUnchanged'] and report['reviewLiveUnchangedDuringRun'] and report['publicationObservationsUnchanged']
        write_json(out / 'report.json', report)
    print(json.dumps({k: report[k] for k in ['passed', 'csharpChecks', 'failure', 'inputsUnchanged', 'preservationUnchanged']}))
    return 0 if report['passed'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
