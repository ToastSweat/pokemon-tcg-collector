"""Promote approved model authoring inputs; never edits runtime Assets or frozen candidates."""
from pathlib import Path
import argparse
import copy
import hashlib
import json
import stat

ROOT = Path(__file__).resolve().parents[1]
APPROVED_ROOT = Path('F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG')


def require_root():
    if ROOT != APPROVED_ROOT:
        raise ValueError('Only the explicitly approved project root is allowed')


SETS = [('jungle', 64, 'jungle-1-removed'), ('fossil', 62, 'fossil-1')]
APPROVAL = 'docs/verification/Phase_5_Model_Approval.json'
WEIGHTS = 'docs/research/phase5/weight-supplement/sheet-weight-supplement.json'
# Exact publication-only classification, not a Markdown exemption. Retain its
# original review digest in Provenance.json; observe today's bytes separately.
HISTORICAL_REVIEW_EVIDENCE = frozenset({'docs/technical/Phase_5_Data_Preparation.md'})
FROZEN = {
    APPROVAL: '014260958e10bc3c2d9b28018b66026ca73490732e6f618c7b045233101fed9d',
    WEIGHTS: '7ceb3d2b4a429c927301e77be965efcb506d41228a0324bc2ff43e3d012713ef',
    'Authoring/Phase5/Data/jungle-en-unlimited1999.catalog.json': 'cc8298c1e444ac6ba7d5c5397ef86a9adc4d70233f6ea25b44d502eafccf6bcd',
    'Authoring/Phase5/Data/fossil-en-unlimited1999.catalog.json': '555303b36387181e15a78fbf371dbd8c129b9ddadc2e7f2b586bfb4cba5dd5fa'
}


def sha(path):
    no_reparse(path)
    return hashlib.sha256(path.read_bytes()).hexdigest()


def source_pins():
    require_root()
    review = json.loads((ROOT / '.artifacts/phase5-review/pins.json').read_text(encoding='utf-8'))
    # Git text/whitespace policy does not enter standalone JSON generation.
    # Its separate review snapshot is audited by the runner, not silently repinned here.
    pins = {name: value['live'] for name, value in review.items() if name != '.gitattributes'}
    pins.update(FROZEN)
    for name, digest in pins.items():
        if name not in HISTORICAL_REVIEW_EVIDENCE and sha(ROOT / name) != digest:
            raise ValueError('Frozen input drift: ' + name)
    # Generator inputs only. Independent metadata review owns the evidence finalizer,
    # literal quote attachment and verification proof; none controls numeric weights.
    supplement = ROOT / 'docs/research/phase5/weight-supplement'
    for name in ['manual-transcription.json',
                 'c10b7cdcae9c7f888eea6b72108aca993c11c6bc.png',
                 '412a4015bff1ef1eb12453acbc355cf7448ab776.png',
                 '18d87beedd4eb026e3af510c22670f09e3d2ce30.png',
                 '3a4f6ceeac70e776f92552a6b0097d6c62e3ac4e.png']:
        p = supplement / name
        pins[p.relative_to(ROOT).as_posix()] = sha(p)
    for name in ['Data.cs', 'Catalog.cs', 'Random.cs', 'PackGenerator.cs']:
        relative = 'Assets/VintageCollector/Domain/' + name
        pins[relative] = sha(ROOT / relative)
    for name in ['.artifacts/phase5-review/pins.json', '.artifacts/phase5-preservation/before.json']:
        pins[name] = sha(ROOT / name)
    return dict(sorted(pins.items()))


def effective_source_pins():
    """Enforced generation inputs, excluding inert historical publication membership."""
    return {name: digest for name, digest in source_pins().items() if name not in HISTORICAL_REVIEW_EVIDENCE}


def publication_observations():
    """Current publication bytes: observation drift is not numeric authority drift."""
    require_root()
    return {name: sha(ROOT / name) for name in sorted(HISTORICAL_REVIEW_EVIDENCE)}


def build_documents():
    require_root()
    docs = {}
    for set_id, target, _ in SETS:
        printing = set_id + '-en-unlimited1999'
        catalog = json.loads((ROOT / ('Authoring/Phase5/Data/' + printing + '.catalog.json')).read_text(encoding='utf-8'))
        catalog = copy.deepcopy(catalog)
        catalog['catalogVersion'] = printing + '-0.1.0'
        catalog['completionTargetStatus'] = 'approved-standard-checklist-' + str(target)
        docs[printing + '.catalog.json'] = catalog
        weights = json.loads((ROOT / 'docs/research/phase5/weight-supplement/sheet-weight-supplement.json').read_text(encoding='utf-8'))
        table_id = next(t for s, _, t in SETS if s == set_id)
        table = next(t for t in weights['tables'] if t['tableId'] == table_id)
        def single(pool):
            return dict(poolId=pool, alternativePoolId='', alternativeNumerator=0, alternativeDenominator=1)
        docs[printing + '.rules.json'] = {
            'schemaVersion': 1, 'modelId': printing + '-sheet-informed', 'modelVersion': '0.1.0',
            'status': 'approved-provisional', 'printingId': printing, 'rngAlgorithm': 'xorshift32-v1',
            'researchRegister': catalog['sourceResearch'],
            'approvalRecord': 'docs/verification/Phase_5_Model_Approval.json',
            'approximations': [
                'Source-qualified per-card relative magnitudes from ' + table_id + '; exact factory sheet/run not authenticated.',
                'Holo probability exactly 1/3 replaces the nonholo rare; independent loose packs, no box quotas or pity.',
                'Identity-level weighted draws without replacement within common and uncommon pools; not physical sheet-strip collation.',
                'Provisional reveal order 7 common, rare-or-holo at position 8, 3 uncommon; not authenticated wrapper orientation.',
                'Standard English Unlimited numbered holo/nonholo only; no error, promo, alternate variants or extra Energy slots.',
                'Visual wrapper choice is external to generator RNG and cannot change pack identities.'
            ],
            'pools': [dict(id=p, uniqueWithinPack=p in ['C', 'U'], entries=[dict(cardId=c['id'], weight=c['multiplicity']) for c in table['cards'] if c['pool'] == p]) for p in ['C', 'U', 'R', 'H']],
            'slots': [single('C') for _ in range(7)] + [dict(poolId='R', alternativePoolId='H', alternativeNumerator=1, alternativeDenominator=3)] + [single('U') for _ in range(3)]
        }
    docs['Provenance.json'] = {
        'schema': 'phase5-approved-model-authoring/v1',
        'owner': 'tools/prepare_phase5_models.py',
        'status': 'approved-provisional', 'modelVersion': '0.1.0',
        'approvalRecord': APPROVAL, 'approvalSha256': FROZEN[APPROVAL],
        'scope': 'standard English Unlimited numbered holo/nonholo printings only',
        'factoryAccuracyClaim': False, 'runtimeAdmission': False,
        'sets': [dict(setId=s, printingId=s + '-en-unlimited1999', sourceTableId=t, completionTarget=n) for s, n, t in SETS],
        'inputs': source_pins()
    }
    return docs


def canonical(value):
    return json.dumps(value, sort_keys=True, ensure_ascii=False, allow_nan=False, separators=(',', ':'))


def validate_documents(docs):
    # JSON spelling preserves bool/int/float distinctions that Python dict equality loses.
    if canonical(docs) != canonical(build_documents()):
        raise ValueError('Approved configuration drift; model 0.1.0 requires exact source-qualified vectors, order, scope and approval')
    return True


APPROVED_DIR = ROOT / 'Authoring/Phase5/ApprovedData'
ARTIFACT_DIR = ROOT / '.artifacts/phase5-models'


def no_reparse(path):
    for p in [*reversed(Path(path).parents), Path(path)]:
        try:
            info = p.lstat()
        except FileNotFoundError:
            continue
        if stat.S_ISLNK(info.st_mode) or getattr(info, 'st_file_attributes', 0) & stat.FILE_ATTRIBUTE_REPARSE_POINT:
            raise ValueError('Reparse/symlink path refused: ' + str(p))


def safe_output(path):
    require_root()
    path = Path(path)
    no_reparse(path)
    resolved = path.resolve()
    if not (resolved == APPROVED_DIR or resolved.is_relative_to(ARTIFACT_DIR) and resolved != ARTIFACT_DIR):
        raise ValueError('Output outside exclusive model ownership: ' + str(path))
    return resolved


def document_bytes(docs):
    return {name: (json.dumps(doc, indent=2, ensure_ascii=False, allow_nan=False) + '\n').encode('utf-8') for name, doc in docs.items()}


def publish(docs, output=APPROVED_DIR):
    output = safe_output(output)
    validate_documents(docs)
    payloads = document_bytes(docs)
    if output.exists():
        # Refuse even apparently owned partial/changed outputs; never repair by silently overwriting.
        if not output.is_dir() or {p.name for p in output.iterdir()} != set(payloads):
            raise ValueError('Unowned or incomplete output directory')
        for name, data in payloads.items():
            path = output / name
            no_reparse(path)
            if not path.is_file() or path.read_bytes() != data:
                raise ValueError('Existing output differs; explicit review required: ' + name)
        return 'unchanged'
    output.mkdir(parents=True, exist_ok=False)
    for name, data in payloads.items():
        with (output / name).open('xb') as stream:
            stream.write(data)
    return 'created'


def unique_object(pairs):
    result = {}
    for name, value in pairs:
        if name in result:
            raise ValueError('Duplicate JSON key: ' + name)
        result[name] = value
    return result


def read_approved(output=APPROVED_DIR):
    output = safe_output(output)
    names = set(build_documents())
    if {p.name for p in output.iterdir()} != names:
        raise ValueError('Approved document membership drift')
    docs = {}
    for name in names:
        no_reparse(output / name)
        docs[name] = json.loads((output / name).read_text(encoding='utf-8'), object_pairs_hook=unique_object)
    validate_documents(docs)
    return docs


def main():
    parser = argparse.ArgumentParser(__doc__)
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument('--write', action='store_true')
    mode.add_argument('--check', action='store_true')
    args = parser.parse_args()
    before = effective_source_pins()
    publication_before = publication_observations()
    docs = build_documents()
    status = publish(docs) if args.write else ('verified' if read_approved() else 'invalid')
    if before != effective_source_pins():
        raise ValueError('Inputs changed during authoring')
    publication_after = publication_observations()
    if publication_before != publication_after:
        raise ValueError('Publication observation drift during authoring')
    print(json.dumps({'status': status, 'documentCount': len(docs), 'inputCount': len(before), 'publicationAudit': {'before': publication_before, 'after': publication_after, 'unchanged': True}, 'files': {name: hashlib.sha256(data).hexdigest() for name, data in document_bytes(docs).items()}}, indent=2))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
