"""Original generic art authoring; never a runtime AssetManifest or art downloader."""
import struct
import zlib
import math
import hashlib
import io
import json
from pathlib import Path
import platform
import shutil
import tempfile
import stat
from PIL import Image, ImageDraw, __version__ as PILLOW_VERSION

PROJECT_ROOT = Path(__file__).resolve().parents[1]
OUTPUT_RELATIVE = Path('Authoring/Phase5/Placeholders')
REFERENCES = {
    'wrapper-front': 'Pokemon-Base-Set/base1-base-set-booster-charizard-unlimited.png',
    'card-face-template': 'Pokemon-Base-Set/base1-001.png',
}
OWNER = {'owner': 'phase5-original-placeholder-authoring', 'schemaVersion': 1}


def json_bytes(value):
    return (json.dumps(value, indent=2, sort_keys=True, ensure_ascii=True) + '\n').encode('utf-8')


def safe_path(path):
    """Static ownership checks, not an OS sandbox against hostile concurrent writers."""
    path = Path(path).absolute()
    if '..' in path.parts or not path.is_relative_to(PROJECT_ROOT):
        raise ValueError('path outside approved project root scope')
    for member in (path, *path.parents):
        if member == PROJECT_ROOT.parent:
            break
        try:
            info = member.lstat()
        except FileNotFoundError:
            continue
        if stat.S_ISLNK(info.st_mode) or getattr(info, 'st_file_attributes', 0) & 0x400:
            raise ValueError('symlink/junction/reparse path refused: ' + str(member))
        if stat.S_ISREG(info.st_mode) and info.st_nlink != 1:
            raise ValueError('hardlink ownership refused: ' + str(member))
    return path


def reference_metadata(root):
    rows = []
    for role, relative in sorted(REFERENCES.items()):
        data = safe_path(root / relative).read_bytes()
        if len(data) < 33 or data[:16] != b'\x89PNG\r\n\x1a\n\0\0\0\rIHDR':
            raise ValueError('invalid PNG dimension reference: ' + relative)
        if zlib.crc32(data[12:29]) & 0xffffffff != struct.unpack('>I', data[29:33])[0]:
            raise ValueError('invalid dimension-reference IHDR CRC: ' + relative)
        width, height = struct.unpack('>II', data[16:24])
        if not (1 <= width <= 4096 and 1 <= height <= 4096):
            raise ValueError('dimension reference outside authoring bounds')
        rows.append({'role': role, 'relativePath': relative, 'width': width, 'height': height,
                     'sha256': hashlib.sha256(data).hexdigest(),
                     'use': 'dimension metadata only; no source pixels copied or decoded'})
    return rows


def make_contact_sheet(images):
    """Native-pixel contact sheet: every source pixel is copied 1:1, without resize."""
    sheet = Image.new('RGBA', (1540, 1842), '#242824')
    d = ImageDraw.Draw(sheet)
    for index, set_id in enumerate(('jungle', 'fossil')):
        y = 96 + index * 889
        lettering(d, set_id.upper() + ' - ORIGINAL TEMPORARY ART', 770, y - 46, 3, CREAM)
        for column, variant in enumerate('ABC'):
            with Image.open(io.BytesIO(images[f'{set_id}-wrapper-{variant}.png'])) as image:
                sheet.paste(image, (32 + column * 284, y))
        with Image.open(io.BytesIO(images[f'{set_id}-card-face-template.png'])) as image:
            sheet.paste(image, (908, y))
        lettering(d, 'AUTHORING ONLY', 438, y + 543, 3, CREAM)
        lettering(d, 'NOT RUNTIME BOUND', 438, y + 588, 2, GOLD)
        lettering(d, 'NO HISTORICAL ART CLAIM', 438, y + 625, 2, GOLD)
    return encode_png(sheet.width, sheet.height, sheet.tobytes())


def prepare(root=PROJECT_ROOT):
    """Publish only the fixed new authoring folder; refuse changed owned outputs."""
    root = safe_path(root)
    safe_path(root / OUTPUT_RELATIVE)
    refs = reference_metadata(root)
    sizes = {row['role']: (row['width'], row['height']) for row in refs}
    # The initial layouts are native to the verified supplied dimensions, not guessed resizing.
    if sizes != {'wrapper-front': (260, 475), 'card-face-template': (600, 825)}:
        raise ValueError('dimension references changed; layout review required, no automatic resize')
    images = build_art(sizes['wrapper-front'], sizes['card-face-template'])
    assets = []
    for name, data in sorted(images.items()):
        set_id = name.split('-')[0]
        role = 'wrapper-front' if '-wrapper-' in name else 'card-face-template'
        variant = name[-5] if role == 'wrapper-front' else 'template'
        width, height = sizes[role]
        assets.append({'bindingId': f'{set_id}.{role}.{variant}', 'setId': set_id,
                       'variant': variant, 'role': role,
                       'relativePath': (OUTPUT_RELATIVE / name).as_posix(),
                       'sha256': hashlib.sha256(data).hexdigest(), 'width': width, 'height': height,
                       'format': 'PNG RGBA8 noninterlaced',
                       'provenance': {'origin': 'original procedural geometric placeholder',
                                      'sourcePixelsReused': False, 'historicalArtSubjectClaim': False,
                                      'intendedUse': 'replaceable private-project presentation; not a card identity'}})
    contact = make_contact_sheet(images)
    manifest = {'schemaVersion': 1, 'kind': 'Phase5PlaceholderAuthoring',
                'status': 'authoring-only-not-runtime-bound', 'runtimeBound': False,
                'supportedPlayableSetsAdded': [], 'dimensionReferences': refs,
                'distinctArtSha256Count': len({row['sha256'] for row in assets}),
                'wrapperSha256Count': len({row['sha256'] for row in assets if row['role'] == 'wrapper-front'}),
                'generator': {'relativePath': 'tools/prepare_phase5_placeholders.py',
                              'python': platform.python_version(), 'pillow': PILLOW_VERSION,
                              'pngEncoding': 'RGBA8 filter-0 stored-DEFLATE; no timestamps or random state'},
                'assets': assets, 'previews': [{'relativePath': (OUTPUT_RELATIVE / 'ContactSheet.png').as_posix(),
                                               'sha256': hashlib.sha256(contact).hexdigest(),
                                               'width': 1540, 'height': 1842, 'nativePixelScale': 1}]}
    files = {**images, 'ContactSheet.png': contact, 'Owner.json': json_bytes(OWNER),
             'PlaceholderManifest.json': json_bytes(manifest)}
    output = root / OUTPUT_RELATIVE
    if reference_metadata(root) != refs:
        raise ValueError('source references changed during render; refusing publication')
    if output.exists():
        if not output.is_dir() or set(p.name for p in output.iterdir()) != set(files):
            raise ValueError('foreign or incomplete authoring destination; refusing adoption')
        for name, data in files.items():
            if safe_path(output / name).read_bytes() != data:
                raise ValueError('changed ownership/art/source receipt; refusing overwrite: ' + name)
        return manifest
    output.parent.mkdir(parents=True, exist_ok=True)
    stage = Path(tempfile.mkdtemp(prefix='.placeholder-stage-', dir=output.parent))
    try:
        for name, data in files.items():
            with (stage / name).open('xb') as handle:
                handle.write(data)
        stage.rename(output)
    finally:
        if stage.exists():
            shutil.rmtree(stage)
    return manifest

# Self-contained 5x7 block alphabet: no installed fonts, logos or image inputs.
_GLYPH_ROWS = {
    'A':'01110/10001/10001/11111/10001/10001/10001',
    'B':'11110/10001/10001/11110/10001/10001/11110',
    'C':'01111/10000/10000/10000/10000/10000/01111',
    'D':'11110/10001/10001/10001/10001/10001/11110',
    'E':'11111/10000/10000/11110/10000/10000/11111',
    'F':'11111/10000/10000/11110/10000/10000/10000',
    'G':'01111/10000/10000/10111/10001/10001/01111',
    'H':'10001/10001/10001/11111/10001/10001/10001',
    'I':'11111/00100/00100/00100/00100/00100/11111',
    'J':'00111/00010/00010/00010/10010/10010/01100',
    'K':'10001/10010/10100/11000/10100/10010/10001',
    'L':'10000/10000/10000/10000/10000/10000/11111',
    'M':'10001/11011/10101/10101/10001/10001/10001',
    'N':'10001/11001/10101/10011/10001/10001/10001',
    'O':'01110/10001/10001/10001/10001/10001/01110',
    'P':'11110/10001/10001/11110/10000/10000/10000',
    'Q':'01110/10001/10001/10001/10101/10010/01101',
    'R':'11110/10001/10001/11110/10100/10010/10001',
    'S':'01111/10000/10000/01110/00001/00001/11110',
    'T':'11111/00100/00100/00100/00100/00100/00100',
    'U':'10001/10001/10001/10001/10001/10001/01110',
    'V':'10001/10001/10001/10001/10001/01010/00100',
    'W':'10001/10001/10001/10101/10101/10101/01010',
    'X':'10001/10001/01010/00100/01010/10001/10001',
    'Y':'10001/10001/01010/00100/00100/00100/00100',
    'Z':'11111/00001/00010/00100/01000/10000/11111',
    ' ':'00000/00000/00000/00000/00000/00000/00000',
    '-':'00000/00000/00000/11111/00000/00000/00000',
}
CREAM = '#F5E6BD'
GOLD = '#CEA76A'


def lettering(draw, text, center_x, y, scale, color):
    x = center_x - (len(text) * 6 - 1) * scale // 2
    for letter in text:
        for row, bits in enumerate(_GLYPH_ROWS[letter].split('/')):
            for col, bit in enumerate(bits):
                if bit == '1':
                    left, top = x + col * scale, y + row * scale
                    draw.rectangle((left, top, left + scale - 1, top + scale - 1), fill=color)
        x += 6 * scale


def motif(draw, set_id, variant, box):
    """Original leaf / fern / canopy or spiral / amber / strata geometry."""
    left, top, right, bottom = box
    sx, sy = (right - left) / 220, (bottom - top) / 220
    def point(x, y):
        return round(left + sx * x), round(top + sy * y)
    def poly(points, color):
        draw.polygon([point(x, y) for x, y in points], fill=color)
    def line(points, color, width=2):
        draw.line([point(x, y) for x, y in points], fill=color, width=max(1, round(width * sx)))
    def ellipse(bounds, color, outline=None):
        draw.ellipse((*point(*bounds[:2]), *point(*bounds[2:])), fill=color, outline=outline, width=max(1, round(sx * 2)))
    if set_id == 'jungle':
        if variant == 'A':
            ellipse((17, 17, 203, 203), '#B58D48')
            ellipse((24, 24, 196, 196), '#E2CB86')
            # Broad pointed leaves, not a creature silhouette.
            for tip, side in [((35, 42), -1), ((186, 48), 1), ((32, 120), -1), ((192, 137), 1)]:
                tx, ty = tip
                poly([(110, 184), (110 + side * 16, 96), (tx, ty), (tx + side * 3, ty + 58)], '#315B48')
                line([(110, 184), (tx, ty)], '#A6B87A', 2)
            poly([(110, 181), (88, 80), (110, 22), (134, 82)], '#557A51')
            line([(110, 201), (110, 42)], CREAM, 2)
        elif variant == 'B':
            for x in range(20, 221, 38):
                line([(x - 20, 220), (x + 30, 0)], '#315B48', 1)
            line([(65, 220), (134, 15)], GOLD, 5)
            for i in range(9):
                y = 43 + i * 18
                x = 126 - i * 6
                reach = 30 + i * 3
                poly([(x, y + 14), (x - reach, y - 20), (x - reach - 12, y - 20), (x - 18, y + 12)], '#91A968')
                poly([(x, y + 14), (x + reach, y - 6), (x + reach + 14, y - 6), (x + 15, y + 19)], '#C7C88B')
        else:
            ellipse((70, 27, 150, 107), '#E8C87E')
            for level, color in [(0, '#66885B'), (1, '#42694C'), (2, '#244C40')]:
                y = 85 + level * 49
                poly([(0, y + 40), (24, y), (57, y + 26), (98, y - 10), (132, y + 18),
                      (171, y - 7), (220, y + 30), (220, 220), (0, 220)], color)
            line([(19, 202), (59, 115), (96, 158), (142, 103), (196, 190)], '#A9B27B', 2)
    elif variant == 'A':
        ellipse((18, 18, 202, 202), '#B38252', GOLD)
        points = []
        for i in range(241):
            angle = i * 0.055
            radius = 4 + i * 0.34
            points.append((110 + math.cos(angle) * radius, 111 + math.sin(angle) * radius))
        line(points, '#F2D8A3', 8)
        for i in range(14):
            angle = i * 0.4
            line([(110 + math.cos(angle) * 64, 111 + math.sin(angle) * 64),
                  (110 + math.cos(angle + .07) * 87, 111 + math.sin(angle + .07) * 87)], '#553F35', 2)
        ellipse((98, 99, 120, 121), '#553F35')
    elif variant == 'B':
        for x, y in [(22, 35), (185, 15), (18, 178), (184, 170)]:
            poly([(x, y), (x + 20, y + 4), (x + 11, y + 25), (x - 5, y + 15)], '#7D6250')
        poly([(106, 7), (180, 50), (197, 137), (122, 213), (33, 169), (26, 77)], '#D69B45')
        poly([(106, 7), (117, 85), (26, 77)], '#F1D18A')
        poly([(117, 85), (180, 50), (197, 137), (148, 133)], '#B46937')
        poly([(117, 85), (148, 133), (122, 213), (33, 169)], '#E3B55C')
        line([(106, 7), (117, 85), (26, 77)], CREAM, 2)
        line([(117, 85), (148, 133), (197, 137)], CREAM, 2)
        line([(148, 133), (122, 213)], CREAM, 2)
        # An abstract botanical inclusion, not an identified prehistoric specimen.
        line([(91, 151), (115, 102)], '#70452D', 4)
        poly([(107, 126), (80, 105), (89, 137)], '#70452D')
        poly([(102, 135), (135, 117), (117, 144)], '#70452D')
    else:
        for y, color in [(20, '#8B7261'), (58, '#B39370'), (96, '#786357'), (134, '#CAA77C'), (174, '#8C6B50')]:
            poly([(0, y + 14), (60, y - 4), (124, y + 6), (177, y - 12), (220, y + 3),
                  (220, y + 47), (0, y + 47)], color)
        poly([(45, 169), (67, 49), (154, 26), (187, 94), (156, 194), (88, 207)], '#DAC39B')
        poly([(67, 49), (92, 101), (88, 207), (45, 169)], '#B39A79')
        line([(104, 62), (126, 91), (107, 122), (127, 151), (107, 177)], '#725B48', 4)
        for y in range(87, 172, 22):
            line([(117, y), (153, y - 14)], '#725B48', 3)
            line([(112, y + 9), (82, y - 3)], '#725B48', 3)


def build_art(wrapper_size, card_size):
    """Render native-sized art from geometry only, never reading supplied pixels."""
    outputs = {}
    for set_id in ('jungle', 'fossil'):
        dark = '#193F35' if set_id == 'jungle' else '#3E3936'
        mid = '#244D40' if set_id == 'jungle' else '#54463B'
        for variant in 'ABC':
            width, height = wrapper_size
            image = Image.new('RGBA', wrapper_size, dark)
            d = ImageDraw.Draw(image)
            d.rectangle((7, 18, width - 8, height - 19), outline=GOLD, width=2)
            for y in (0, height - 27):
                d.rectangle((0, y, width - 1, y + 26), fill=GOLD)
                for x in range(3, width, 7):
                    d.line((x, y + 3, x, y + 23), fill=dark, width=1)
            lettering(d, 'TEMPORARY ART', width // 2, 47, 1, CREAM)
            lettering(d, set_id.upper(), width // 2, 78, 4, CREAM)
            d.line((31, 121, width - 32, 121), fill=GOLD, width=2)
            d.rectangle((17, 135, width - 18, height - 111), fill=mid)
            motif(d, set_id, variant, (20, 138, width - 20, height - 114))
            lettering(d, 'VARIANT ' + variant, width // 2, height - 90, 2, CREAM)
            lettering(d, 'ORIGINAL PLACEHOLDER', width // 2, height - 58, 1, GOLD)
            outputs[f'{set_id}-wrapper-{variant}.png'] = encode_png(width, height, image.tobytes())
        width, height = card_size
        image = Image.new('RGBA', card_size, CREAM)
        d = ImageDraw.Draw(image)
        d.rounded_rectangle((13, 13, width - 14, height - 14), radius=24, fill=dark)
        d.rounded_rectangle((25, 25, width - 26, height - 26), radius=16, outline=GOLD, width=3)
        lettering(d, set_id.upper(), width // 2, 64, 6, CREAM)
        lettering(d, 'GENERIC CARD FACE', width // 2, 132, 3, GOLD)
        d.rectangle((55, 185, width - 56, 550), fill=mid, outline=GOLD, width=2)
        motif(d, set_id, 'A' if set_id == 'jungle' else 'C', (118, 194, width - 119, 540))
        lettering(d, 'ART PLACEHOLDER', width // 2, 590, 3, CREAM)
        lettering(d, 'REPLACEABLE TEMPLATE', width // 2, 642, 2, GOLD)
        d.line((72, 694, width - 73, 694), fill=GOLD, width=2)
        lettering(d, 'TEMPORARY ART', width // 2, 739, 2, CREAM)
        outputs[f'{set_id}-card-face-template.png'] = encode_png(width, height, image.tobytes())
    return outputs


def encode_png(width, height, rgba):
    """Canonical filter-0, RGBA8, noninterlaced PNG with no ancillary metadata."""
    if type(width) is not int or type(height) is not int or width <= 0 or height <= 0:
        raise ValueError('positive integer dimensions required')
    if len(rgba) != width * height * 4:
        raise ValueError('RGBA byte count does not match dimensions')
    def chunk(kind, body):
        return struct.pack('>I', len(body)) + kind + body + struct.pack('>I', zlib.crc32(kind + body) & 0xffffffff)
    rows = b''.join(b'\0' + rgba[y * width * 4:(y + 1) * width * 4] for y in range(height))
    # Stored DEFLATE blocks are portable and byte-identical across zlib versions.
    body = b'\x78\x01'
    for start in range(0, len(rows), 65535):
        block = rows[start:start + 65535]
        body += bytes([int(start + len(block) == len(rows))]) + struct.pack('<HH', len(block), len(block) ^ 0xffff) + block
    body += struct.pack('>I', zlib.adler32(rows) & 0xffffffff)
    return (b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 6, 0, 0, 0))
            + chunk(b'IDAT', body) + chunk(b'IEND', b''))


if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=PROJECT_ROOT,
                        help='project root or root-contained disposable fixture root only')
    args = parser.parse_args()
    result = prepare(args.root)
    print(json.dumps({'manifest': (args.root / OUTPUT_RELATIVE / 'PlaceholderManifest.json').as_posix(),
                      'contactSheet': (args.root / result['previews'][0]['relativePath']).as_posix(),
                      'artImages': len(result['assets']),
                      'distinctArtSha256Count': result['distinctArtSha256Count'],
                      'wrapperSha256Count': result['wrapperSha256Count'],
                      'runtimeBound': result['runtimeBound']}, sort_keys=True))
