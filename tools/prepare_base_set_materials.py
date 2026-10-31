"""Private, deterministic Base Set image authoring; never modifies source images."""
import argparse
from collections import Counter
import hashlib
import io
import json
import os
from pathlib import Path
import re
import shutil
import stat
import struct
import sys
import tempfile
import uuid
import zlib
from PIL import Image, __version__ as PILLOW_VERSION

sys.dont_write_bytecode = True
CATALOG = Path('Assets/VintageCollector/Data/base-en-unlimited1999.catalog.json')
INTAKE = Path('docs/verification/Supplied_Base_Set_Intake.json')
DESTINATION = Path('Assets/Supplied/BaseSetMaterials')
RESTRICTIONS = 'private personal project use only; no redistribution; permission not established'
RECIPE = 'lossless-full-RGBA8-png-zlib9-v1'


def sha(data):
    return hashlib.sha256(data).hexdigest()


def json_bytes(value):
    return (json.dumps(value, ensure_ascii=False, sort_keys=True, indent=2) + '\n').encode('utf-8')


class PreparationError(ValueError):
    """Unsafe or inconsistent authoring input; no new manifest is published."""


def require(condition, message):
    if not condition:
        raise PreparationError(message)


def normalize_png(data, dimensions):
    """Fully verify/decode PNG; preserve exact RGBA pixels in a runtime-safe codec."""
    require(33 <= len(data) <= 16 * 1024 * 1024, 'PNG byte limit')
    require(data[:8] == b'\x89PNG\r\n\x1a\n', 'not a PNG')
    require(data[12:16] == b'IHDR', 'missing PNG header')
    width, height, depth, color, compression, filtering, interlace = struct.unpack('>IIBBBBB', data[16:29])
    require((width, height) == dimensions and depth in (1, 2, 4, 8) and color in (0, 2, 3, 4, 6), 'unsupported PNG header')
    require(compression == 0 and filtering == 0 and interlace in (0, 1), 'invalid PNG codec')
    offset, ended = 8, False
    while offset < len(data):
        require(offset + 12 <= len(data), 'truncated PNG chunk')
        length = struct.unpack('>I', data[offset:offset + 4])[0]
        chunk_type = data[offset + 4:offset + 8]
        end = offset + 12 + length
        require(end <= len(data), 'truncated PNG payload')
        require(zlib.crc32(data[offset + 4:end - 4]) & 0xffffffff == struct.unpack('>I', data[end - 4:end])[0], 'PNG chunk CRC mismatch')
        require(chunk_type not in (b'acTL', b'fcTL', b'fdAT'), 'animated PNG refused')
        offset = end
        if chunk_type == b'IEND':
            require(length == 0 and offset == len(data), 'PNG trailing data refused')
            ended = True
            break
    require(ended, 'missing PNG end')
    try:
        with Image.open(io.BytesIO(data)) as check:
            require(check.format == 'PNG' and check.size == dimensions, 'PNG dimensions mismatch')
            check.verify()
        with Image.open(io.BytesIO(data)) as original:
            original.load()
            require(getattr(original, 'n_frames', 1) == 1, 'animated PNG not allowed')
            rgba = original.convert('RGBA')
            # Strip ancillary metadata; conversion performs no color-space transform.
            pixels = rgba.tobytes()
            encoded = io.BytesIO()
            Image.frombytes('RGBA', dimensions, pixels).save(encoded, format='PNG', optimize=False, compress_level=9)
        result = encoded.getvalue()
        require(struct.unpack('>IIBBBBB', result[16:29]) == (*dimensions, 8, 6, 0, 0, 0), 'derived PNG codec mismatch')
        with Image.open(io.BytesIO(result)) as derived:
            derived.verify()
        with Image.open(io.BytesIO(result)) as derived:
            derived.load()
            require(derived.convert('RGBA').tobytes() == pixels, 'derived pixel mismatch')
        return result
    except (OSError, SyntaxError, struct.error) as error:
        raise PreparationError('PNG decode failed: ' + str(error)) from error


CANONICAL_CATALOG_JSON_SHA256 = '173794c7e49dada65950ec43f4cf5f321d7cd2cce22df938da26aea70af5711e'
PNG_NAMES = {f'base1-{number:03d}.png' for number in range(1, 103)} | {
    'base1-back.png', 'base1-foil.png',
    'base1-base-set-booster-blastoise-unlimited.png',
    'base1-base-set-booster-charizard-unlimited.png',
    'base1-base-set-booster-venusaur-unlimited.png',
    'base1-base-set-booster-unlimited-back-diffuse.png',
    'base1-base-set-booster-unlimited-back-metallic.png',
    'base1-base-set-booster-unlimited-front-metallic.png',
}
SOURCE_NAMES = PNG_NAMES | {'2cf8c21a73eecc4118d4aa001faa90a9dc3531a4.jpeg', 'back.psd', 'base1-016.psd', 'base1-base-set-booster-unlimited.psd'}


def strict_json(data):
    def pairs(items):
        result = {}
        for key, value in items:
            require(key not in result, 'duplicate JSON key: ' + key)
            result[key] = value
        return result
    try:
        require(not data.startswith(b'\xef\xbb\xbf'), 'UTF-8 BOM not allowed')
        return json.loads(data.decode('utf-8'), object_pairs_hook=pairs,
                          parse_constant=lambda value: (_ for _ in ()).throw(PreparationError('non-finite JSON number')))
    except (UnicodeError, ValueError) as error:
        raise PreparationError('invalid strict JSON: ' + str(error)) from error


def checked_path(path):
    """Reject traversal, symlinks, junctions/reparse points and regular-file hard links."""
    require(not str(path).startswith(('\\\\', '//')), 'UNC/network/device path refused')
    path = Path(path)
    require('..' not in path.parts, 'path traversal refused')
    path = path.absolute()
    for member in [*reversed(path.parents), path]:
        try:
            info = member.lstat()
        except FileNotFoundError:
            continue
        require(not stat.S_ISLNK(info.st_mode) and not (getattr(info, 'st_file_attributes', 0) & 0x400), 'reparse/symlink path refused: ' + str(member))
        if stat.S_ISREG(info.st_mode):
            require(info.st_nlink == 1, 'hard-linked file refused: ' + str(member))
    return path


def preflight(root):
    """Validate the exact supplied inventory and frozen canonical catalog before staging."""
    checked_path(root)
    catalog_path, intake_path = checked_path(root / CATALOG), checked_path(root / INTAKE)
    try:
        catalog = strict_json(catalog_path.read_bytes())
        require(isinstance(catalog, dict) and isinstance(catalog.get('cards'), list), 'catalog shape')
        cards = sorted(catalog['cards'], key=lambda card: int(card['collectorNumber']))
        catalog['cards'] = cards
        require(sha(json_bytes(catalog)) == CANONICAL_CATALOG_JSON_SHA256, 'catalog identities/variants/names differ from frozen existing catalog')
        intake = strict_json(intake_path.read_bytes())
        require(isinstance(intake, dict) and intake.get('sourceFolder') == 'Pokemon-Base-Set', 'source folder mismatch')
        rows = intake.get('files')
        require(isinstance(rows, list), 'inventory file list missing')
        assert isinstance(rows, list)  # Type narrowing after the runtime guard.
        require(len(rows) == 114 and intake.get('fileCount') == 114 and intake.get('pngCount') == 110, 'inventory counts mismatch')
        expected = {'Pokemon-Base-Set/' + name for name in SOURCE_NAMES}
        paths = [row['relativePath'] for row in rows]
        require(len(set(paths)) == len(paths) and set(paths) == expected, 'inventory paths missing, duplicated, foreign or unsafe')
        source_dir = checked_path(root / 'Pokemon-Base-Set')
        require({path.name for path in source_dir.iterdir()} == SOURCE_NAMES, 'source directory has missing or foreign files')
        for row in rows:
            require(isinstance(row.get('sha256'), str) and re.fullmatch('[0-9a-f]{64}', row['sha256']) is not None, 'invalid lowercase source hash')
            require(type(row.get('bytes')) is int and 0 < row['bytes'] <= 20 * 1024 * 1024, 'invalid source byte count')
            path = checked_path(root / row['relativePath'])
            require(path.is_file(), 'missing/non-file source: ' + str(path))
            require(path.stat().st_size == row['bytes'] and sha(path.read_bytes()) == row['sha256'], 'source pin mismatch: ' + path.name)
            if path.name in PNG_NAMES:
                dimensions = (260, 475) if 'booster-' in path.name else (600, 825)
                require((row.get('width'), row.get('height')) == dimensions, 'inventory dimensions mismatch')
        return cards, sorted(rows, key=lambda row: row['relativePath'])
    except (OSError, KeyError, TypeError, AttributeError, ValueError) as error:
        if isinstance(error, PreparationError):
            raise
        raise PreparationError('invalid preflight input: ' + str(error)) from error


OWNER_BYTES = json_bytes({'schemaVersion': 1, 'owner': 'prepare_base_set_materials.py', 'destination': 'Assets/Supplied/BaseSetMaterials'})


UNITY_META_PINS = Path('docs/verification/Base_Set_Materials_Unity_Meta_Pins.json')
MAX_META_BYTES = 64 * 1024
MAX_META_LEDGER_BYTES = 1024 * 1024
MAX_META_PINS = 4096


def bounded_regular_bytes(path, limit):
    """Check every ancestor/link before a bounded regular-file leaf read."""
    path = checked_path(path)
    require(path.is_file() and 0 < path.stat().st_size <= limit, 'invalid/bounded metadata file: ' + str(path))
    with path.open('rb') as stream:
        data = stream.read(limit + 1)
    require(0 < len(data) <= limit, 'metadata byte limit')
    return data


def pinned_native_metas(destination):
    """Read only the exact source-root ledger; never discover, adopt or write metas."""
    root = checked_path(destination.parents[2])
    require(destination == root / DESTINATION, 'metadata destination is not the fixed owned path')
    ledger_path = checked_path(root / UNITY_META_PINS)
    if not ledger_path.exists():
        return set()
    ledger = strict_json(bounded_regular_bytes(ledger_path, MAX_META_LEDGER_BYTES))
    require(isinstance(ledger, dict) and set(ledger) == {'schemaVersion', 'originStage', 'files'}, 'metadata ledger schema')
    require(type(ledger['schemaVersion']) is int and ledger['schemaVersion'] == 1, 'metadata ledger version')
    origin = ledger['originStage']
    require(isinstance(origin, str) and 0 < len(origin) <= 256 and origin.strip() == origin
            and all(ord(c) >= 32 and ord(c) != 127 for c in origin), 'metadata origin stage')
    rows = ledger['files']
    require(isinstance(rows, list) and len(rows) <= MAX_META_PINS, 'metadata ledger file limit')
    prefix = DESTINATION.as_posix() + '/'
    result, guids = set(), set()
    for row in rows:
        require(isinstance(row, dict) and set(row) == {'path', 'sha256', 'bytes', 'guid'}, 'metadata pin schema')
        relative = row['path']
        require(isinstance(relative, str) and len(relative) <= 256 and relative.startswith(prefix), 'metadata pin path')
        suffix = relative[len(prefix):]
        # Grammar is checked before constructing or probing a candidate filesystem path.
        is_folder = suffix == 'generations.meta' or re.fullmatch(r'generations/[0-9a-f]{64}\.meta', suffix) is not None
        leaf_match = re.fullmatch(r'generations/[0-9a-f]{64}/(Inventory\.json|[a-z0-9.-]+)\.meta', suffix)
        is_file = suffix in {'Owner.json.meta', 'Manifest.json.meta'} or (
            leaf_match is not None and leaf_match[1] in PNG_NAMES | {'Inventory.json'})
        require(is_folder or is_file, 'metadata pin is not an exact owned sidecar')
        require(isinstance(row['sha256'], str) and re.fullmatch('[0-9a-f]{64}', row['sha256']) is not None, 'metadata hash format')
        require(type(row['bytes']) is int and 0 < row['bytes'] <= MAX_META_BYTES, 'metadata byte count')
        guid = row['guid']
        require(isinstance(guid, str) and re.fullmatch('[0-9a-f]{32}', guid) is not None and guid != '0' * 32, 'native GUID format')
        sidecar = checked_path(root / relative)
        require(sidecar not in result and guid not in guids, 'duplicate metadata path/GUID')
        asset = checked_path(root / relative[:-5])
        require(asset.is_dir() if is_folder else asset.is_file(), 'metadata corresponding owned asset missing/wrong type')
        data = bounded_regular_bytes(sidecar, MAX_META_BYTES)
        require(len(data) == row['bytes'] and sha(data) == row['sha256'], 'metadata pin mismatch: ' + relative)
        try:
            text = data.decode('utf-8')
        except UnicodeError as error:
            raise PreparationError('metadata is not UTF-8') from error
        require(text.startswith(('fileFormatVersion: 2\n', 'fileFormatVersion: 2\r\n')) and '\x00' not in text,
                'native metadata header')
        guid_lines = [line for line in text.splitlines() if line.startswith('guid:')]
        require(guid_lines == ['guid: ' + guid], 'native metadata GUID mismatch/duplicate')
        result.add(sidecar)
        guids.add(guid)
    return result


def validate_destination(destination, cards):
    """Refuse foreign files and modified owned generations; never repair by overwriting."""
    destination = checked_path(destination)
    native_metas = pinned_native_metas(destination)
    if not destination.exists():
        return
    require(destination.is_dir(), 'foreign destination is not a directory')
    owner = checked_path(destination / 'Owner.json')
    require(owner.is_file() and owner.read_bytes() == OWNER_BYTES, 'foreign destination: no exact authoring ownership marker')
    require({p.name for p in destination.iterdir() if p not in native_metas} <= {'Owner.json', 'generations', 'Manifest.json'}, 'foreign destination member')
    generations = checked_path(destination / 'generations')
    inventories = {}
    if generations.exists():
        require(generations.is_dir(), 'foreign generations path')
        for directory in generations.iterdir():
            if directory in native_metas:
                continue
            checked_path(directory)
            require(directory.is_dir() and re.fullmatch('[0-9a-f]{64}', directory.name) is not None, 'foreign generation path')
            require({p.name for p in directory.iterdir() if p not in native_metas} == PNG_NAMES | {'Inventory.json'}, 'foreign/incomplete generation members')
            inventory = strict_json(checked_path(directory / 'Inventory.json').read_bytes())
            require(inventory.get('recipe') == RECIPE and inventory.get('generation') == directory.name and inventory.get('catalogSha256') == CANONICAL_CATALOG_JSON_SHA256, 'foreign generation receipt')
            derived = inventory.get('derivedFiles', {})
            require(set(derived) == PNG_NAMES, 'incomplete derived receipt')
            require(sha(json_bytes({'recipe': RECIPE, 'sources': inventory['sourceFiles'], 'derived': derived, 'cards': cards})) == directory.name, 'generation content identity mismatch')
            for name, row in derived.items():
                data = checked_path(directory / name).read_bytes()
                require(len(data) == row['bytes'] and sha(data) == row['sha256'], 'modified derived file: ' + name)
            inventories[directory.name] = inventory
    manifest_path = checked_path(destination / 'Manifest.json')
    if manifest_path.exists():
        data = manifest_path.read_bytes()
        manifest = strict_json(data)
        require(any(inv.get('manifestSha256') == sha(data) for inv in inventories.values()), 'foreign or modified destination manifest')
        for asset in manifest['assets']:
            path = asset['relativePath']
            require(isinstance(path, str) and re.fullmatch(r'Assets/Supplied/BaseSetMaterials/generations/[0-9a-f]{64}/[a-z0-9.-]+\.png', path) is not None, 'unsafe published manifest path')
            parts = path.split('/')[2:]
            require(parts[2] in inventories and parts[3] in inventories[parts[2]]['derivedFiles'], 'published binding is missing')
            require(inventories[parts[2]]['derivedFiles'][parts[3]]['sha256'] == asset['sha256'], 'published binding hash mismatch')


def publish_manifest(candidate, destination, expected=None):
    """Publish a whole manifest. A first publication cannot clobber a foreign file."""
    checked_path(destination)
    if expected is not None:
        require(destination.is_file() and destination.read_bytes() == expected, 'prior manifest changed before publication')
        os.replace(candidate, destination)
    else:
        try:
            os.link(candidate, destination)  # Atomic no-replace publication, fail closed if unsupported.
        except FileExistsError as error:
            raise PreparationError('late foreign manifest collision') from error
        candidate.unlink()


def prepare(root):
    """Serialize cooperative authors without writing a lock into the readonly source folder."""
    root = checked_path(root)
    artifact_root = checked_path(root / '.artifacts/materials-authoring')
    artifact_root.mkdir(parents=True, exist_ok=True)
    lock = checked_path(artifact_root / 'prepare.lock')
    token = json_bytes({'pid': os.getpid(), 'token': uuid.uuid4().hex})
    try:
        handle = lock.open('xb')
    except FileExistsError as error:
        raise PreparationError('active/foreign authoring lock; no waiting or stale-lock deletion') from error
    identity = os.fstat(handle.fileno())
    try:
        handle.write(token)
        handle.flush()
        return _prepare_locked(root)
    finally:
        handle.close()
        current = checked_path(lock).stat()
        require((current.st_dev, current.st_ino) == (identity.st_dev, identity.st_ino) and lock.read_bytes() == token, 'authoring lock ownership changed; retained')
        lock.unlink()


def _prepare_locked(root):
    """Prepare one private material manifest and full-resolution lossless generation."""
    root = checked_path(root)
    cards, rows = preflight(root)
    destination = checked_path(root / DESTINATION)
    validate_destination(destination, cards)
    artifact_root = checked_path(root / '.artifacts/materials-authoring')
    artifact_root.mkdir(parents=True, exist_ok=True)
    stage = Path(tempfile.mkdtemp(prefix='stage-', dir=artifact_root))
    try:
        images = stage / 'images'
        images.mkdir()
        derived = {}
        for row in rows:
            original = checked_path(root / row['relativePath'])
            source_bytes = original.read_bytes()
            require(sha(source_bytes) == row['sha256'] and len(source_bytes) == row['bytes'], 'source pin mismatch: ' + original.name)
            if original.suffix != '.png':
                continue
            dimensions = (row['width'], row['height'])
            data = normalize_png(source_bytes, dimensions)
            (images / original.name).write_bytes(data)
            derived[original.name] = {'sha256': sha(data), 'bytes': len(data), 'width': dimensions[0], 'height': dimensions[1], 'sourceSha256': row['sha256'], 'rgbaPixelsEqual': True}
        generation = sha(json_bytes({'recipe': RECIPE, 'sources': rows, 'derived': derived, 'cards': cards}))
        prefix = 'Assets/Supplied/BaseSetMaterials/generations/' + generation + '/'
        assets = []

        def entry(role, filename, card_id=''):
            source = 'local user folder Pokemon-Base-Set/' + filename
            if role == 'card-face':
                source += '; user-approved first-edition scan visual substitute for Unlimited simulation; printing identity unchanged'
            assets.append({'role': role, 'cardId': card_id, 'relativePath': prefix + filename, 'source': source, 'rights': 'unknown', 'restrictions': RESTRICTIONS, 'sha256': derived[filename]['sha256']})

        for card in cards:
            entry('card-face', 'base1-' + str(int(card['collectorNumber'])).zfill(3) + '.png', card['id'])
        for card in cards:
            if card['finish'] == 'holo':
                entry('holo-mask', 'base1-foil.png', card['id'])
        for role, name in [('card-back', 'base1-back.png'), ('wrapper-front', 'base1-base-set-booster-charizard-unlimited.png'), ('wrapper-back', 'base1-base-set-booster-unlimited-back-diffuse.png'), ('wrapper-front-mask', 'base1-base-set-booster-unlimited-front-metallic.png'), ('wrapper-back-mask', 'base1-base-set-booster-unlimited-back-metallic.png')]:
            entry(role, name)
        manifest = {'schemaVersion': 1, 'status': 'user-supplied-private-materials', 'intendedUse': 'private-personal-desktop-project', 'redistributionPermission': 'not-established', 'rightsDefault': 'unknown', 'notes': 'Replaceable user-supplied visual art only. User approved first-edition card scans as visual substitutes for existing Unlimited simulation identities; originals and decoded pixels preserved. No rights or redistribution permission granted.', 'assets': assets}
        manifest_bytes = json_bytes(manifest)
        inventory = {'schemaVersion': 1, 'recipe': RECIPE, 'generation': generation, 'sourceFiles': rows, 'derivedFiles': derived, 'catalogSha256': CANONICAL_CATALOG_JSON_SHA256, 'manifestSha256': sha(manifest_bytes), 'sourcePrinting': 'first-edition scans', 'simulationPrinting': 'base-en-unlimited1999', 'userApprovedVisualMismatch': True, 'rights': 'unknown', 'restrictions': RESTRICTIONS}
        (images / 'Inventory.json').write_bytes(json_bytes(inventory))
        # Re-read source pins immediately before publishing; no lock or write on sources.
        final_cards, final_rows = preflight(root)
        require(final_cards == cards and final_rows == rows, 'inputs changed during preparation')
        validate_destination(destination, cards)
        if not destination.exists():
            destination.parent.mkdir(parents=True, exist_ok=True)
            destination.mkdir()  # Exclusive: never adopt a late-arriving foreign directory.
            with (destination / 'Owner.json').open('xb') as output:
                output.write(OWNER_BYTES)
        generations = destination / 'generations'
        generations.mkdir(exist_ok=True)
        target = generations / generation
        if target.exists():
            require((target / 'Inventory.json').read_bytes() == json_bytes(inventory), 'generation receipt collision')
            for name in derived:
                require((target / name).read_bytes() == (images / name).read_bytes(), 'generation image collision')
        else:
            os.rename(images, target)  # Same-volume complete owned directory, no file overwrites.
        validate_destination(destination, cards)
        target_manifest = checked_path(destination / 'Manifest.json')
        expected = target_manifest.read_bytes() if target_manifest.exists() else None
        if expected != manifest_bytes:
            candidate_manifest = stage / 'Manifest.json'
            candidate_manifest.write_bytes(manifest_bytes)
            publish_manifest(candidate_manifest, target_manifest, expected)  # Last publication operation.
        # Mirror the native cache: paths are bindings, not resource identities.
        # Identical decoded bytes still need separate linear-mask/sRGB resources.
        unique_resources = set()
        for asset in assets:
            image = derived[Path(asset['relativePath']).name]
            semantics = 'linear-mask' if asset['role'] in ('holo-mask', 'wrapper-front-mask', 'wrapper-back-mask') else 'srgb-face'
            unique_resources.add((asset['sha256'], semantics, image['width'], image['height']))
        unique_paths = {asset['relativePath'] for asset in assets}
        unique_pixels = sum(width * height for _, _, width, height in unique_resources)
        return {'manifestPath': str(destination / 'Manifest.json'), 'manifestSha256': sha(manifest_bytes), 'generation': generation, 'bindingCounts': dict(Counter(entry['role'] for entry in assets)), 'bindingCount': len(assets), 'uniqueRuntimeImageCount': len(unique_resources), 'distinctRuntimeFileCount': len(unique_paths), 'runtimeTexturePixels': unique_pixels, 'runtimeRgbaBytes': unique_pixels * 4, 'derivedImageCount': len(derived), 'derivedTexturePixels': sum(item['width'] * item['height'] for item in derived.values()), 'sourceFileCount': len(rows), 'sourcePngCount': len(derived), 'sourcePinsSha256': sha(json_bytes([{'relativePath': row['relativePath'], 'sha256': row['sha256'], 'bytes': row['bytes']} for row in rows])), 'catalogCanonicalJsonSha256': inventory['catalogSha256'], 'catalogSha256': sha((root / CATALOG).read_bytes()), 'inventoryPath': str(target / 'Inventory.json'), 'originalsUnchanged': True, 'allSourceAndDerivedPngsFullyDecoded': True, 'allFullRgbaPixelArraysEqual': True}
    finally:
        shutil.rmtree(stage)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1], help='project root; fixtures must stay in .artifacts/materials-authoring')
    args = parser.parse_args(argv)
    root = checked_path(args.root)
    allowed = Path(__file__).resolve().parents[1]
    require(root == allowed or root.is_relative_to(allowed / '.artifacts/materials-authoring'), 'root outside owned project/authoring fixtures refused')
    artifacts = checked_path(root / '.artifacts/materials-authoring')
    artifacts.mkdir(parents=True, exist_ok=True)
    try:
        old_manifest = checked_path(root / 'Assets/Supplied/AssetManifest.json')
        old_bytes = old_manifest.read_bytes() if old_manifest.exists() else None
        report = prepare(root)
        require(old_bytes is None or old_manifest.read_bytes() == old_bytes, 'accepted production manifest changed during preparation')
        report['acceptedProductionManifestSha256'] = sha(old_bytes) if old_bytes is not None else None
        report['pillowVersion'] = PILLOW_VERSION
        report['pythonVersion'] = sys.version.split()[0]
        report['zlibVersion'] = zlib.ZLIB_RUNTIME_VERSION
        inventory = strict_json(checked_path(report['inventoryPath']).read_bytes())
        pins = {'schemaVersion': 1, 'sourcePinsSha256': report['sourcePinsSha256'], 'files': [{'relativePath': row['relativePath'], 'bytes': row['bytes'], 'sha256': row['sha256']} for row in inventory['sourceFiles']]}
        for name, value in [('Source_Hash_Pins.json', pins), ('Prepared_Report.json', report)]:
            path = checked_path(artifacts / name)
            data = json_bytes(value)
            if not path.exists() or path.read_bytes() != data:
                path.write_bytes(data)
        print(json_bytes(report).decode('utf-8'), end='')
        return 0
    except (OSError, ValueError, KeyError, TypeError) as error:
        failure = {'status': 'failed', 'errorType': type(error).__name__, 'error': str(error), 'newManifestNotClaimed': True}
        path = artifacts / ('Failure-' + uuid.uuid4().hex + '.json')
        with path.open('xb') as output:
            output.write(json_bytes(failure))
        print(json_bytes(failure).decode('utf-8'), file=sys.stderr, end='')
        return 1


if __name__ == '__main__':
    sys.exit(main())
