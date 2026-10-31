"""Exercise the real graphics-enabled Phase 3 Windows player in isolated project saves.

Never sends foreground input or moves the user's cursor. Optional input mode uses
background Win32 messages to this script's own player and requires observed effects.
"""
import argparse
import ctypes
from ctypes import wintypes
import hashlib
import json
import os
from pathlib import Path
import re
import struct
import subprocess
import sys
import time
import zlib

ROOT = Path(__file__).resolve().parents[1]
EXE = ROOT / 'Builds/Phase3Windows/VintageCardCollector.exe'


def png_size(path):
    raw = path.read_bytes()
    if raw[:8] != b'\x89PNG\r\n\x1a\n':
        raise ValueError('Not a PNG: ' + str(path))
    cursor = 8
    dimensions = None
    ended = False
    while cursor < len(raw):
        length = struct.unpack('>I', raw[cursor:cursor+4])[0]
        kind = raw[cursor+4:cursor+8]
        data = raw[cursor+8:cursor+8+length]
        crc = struct.unpack('>I', raw[cursor+8+length:cursor+12+length])[0]
        if len(data) != length or zlib.crc32(kind+data) & 0xffffffff != crc:
            raise ValueError('Incomplete/corrupt PNG chunk: ' + str(path))
        if kind == b'IHDR':
            dimensions = struct.unpack('>II', data[:8])
        cursor += length + 12
        if kind == b'IEND':
            ended = True
            break
    if not ended or dimensions is None:
        raise ValueError('PNG did not finish: ' + str(path))
    return dimensions


def read_json_eventually(path, predicate, timeout):
    deadline = time.monotonic() + timeout
    last = None
    while time.monotonic() < deadline:
        try:
            value = json.loads(path.read_text(encoding='utf-8'))
            last = value
            if predicate(value):
                return value
        except (OSError, ValueError):
            pass
        time.sleep(0.05)
    raise TimeoutError('No confirmed state at ' + str(path) + '; last=' + repr(last))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--label', required=True)
    parser.add_argument('--mode', choices=['smoke', 'layouts', 'input', 'crash', 'keyboard'], default='smoke')
    args = parser.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9_-]+', args.label):
        parser.error('Unsafe artifact label')
    if not EXE.is_file():
        parser.error('No production Phase 3 player built at ' + str(EXE))
    out = ROOT / '.artifacts/phase3-player' / args.label
    out.mkdir(parents=True, exist_ok=False)  # Never recycle an existing save/claim fresh state.
    (out/'tmp').mkdir()
    env = dict(os.environ, TMP=str(out/'tmp'), TEMP=str(out/'tmp'), TMPDIR=str(out/'tmp'))
    checks, cases, screenshots = [], {}, []
    source_paths = sorted((ROOT/'Assets/VintageCollector/Domain').glob('*.cs')) + sorted((ROOT/'Assets/VintageCollector/Data').glob('*.json'))
    before = {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in source_paths}

    def check(name, passed, detail=''):
        checks.append({'name': name, 'passed': bool(passed), 'detail': detail})
        if not passed:
            raise RuntimeError(name + ': ' + detail)

    def launch(scenario, save, case, width=1280, height=720):
        folder = out/case
        folder.mkdir()
        command = [str(EXE), '-screen-width', str(width), '-screen-height', str(height),
                   '-screen-fullscreen', '0', '-logFile', str(folder/'player.log'),
                   '--vcc-smoke='+scenario, '--vcc-save-root='+str(save), '--vcc-artifacts='+str(folder)]
        stream = (folder/'process.log').open('w', encoding='utf-8')
        process = subprocess.Popen(command, cwd=EXE.parent, env=env, stdout=stream, stderr=subprocess.STDOUT)
        return process, stream, folder

    def finish_case(process, stream, folder, width, height):
        try:
            code = process.wait(timeout=90)
        finally:
            if process.poll() is None:
                process.kill()
                process.wait(timeout=10)
            stream.close()
        check(folder.name+':exit', code == 0, 'Real player exit '+str(code))
        report = json.loads((folder/'report.json').read_text(encoding='utf-8'))
        check(folder.name+':report', report['passed'] and report['checkCount'] > 0 and
              report['checkCount'] == len(report['checks']) and all(c['passed'] for c in report['checks']),
              'Declared assertion count reconciled.')
        for name in report['screenshots']:
            if Path(name).name != name or not name.endswith('.png'):
                raise ValueError('Unsafe screenshot report path')
            image = folder/name
            check(folder.name+':png:'+name, png_size(image) == (width, height), 'Actual PNG CRCs/dimensions.')
            screenshots.append(str(image.relative_to(ROOT)))
        cases[folder.name] = report
        return report

    def run(scenario, save, case, width=1280, height=720):
        return finish_case(*launch(scenario, save, case, width, height), width, height)

    def verify_state(save, expected_total, expected_packs):
        raw = (save/'state.save').read_bytes()
        header, digest, payload = raw.split(b'\n', 2)
        check(save.name+':save-envelope', header == b'VTCG-SAVE-1' and hashlib.sha256(payload).hexdigest().encode() == digest,
              'Actual authoritative session payload checksum.')
        state = json.loads(payload)
        check(save.name+':archive-total', len(state['packs']) == expected_packs and
              sum(len(p['orderedCardIds']) for p in state['packs']) == expected_total,
              'Read authoritative player save, not only a UI report.')
        check(save.name+':backup-mirrors-primary', raw == (save/'state.backup').read_bytes() and not (save/'state.pending').exists(),
              'Acknowledged copies synchronized; no pending commit remains.')

    failure = None
    try:
        check('player-dependencies-present', (EXE.parent/'VintageCardCollector_Data').is_dir() and
              (EXE.parent/'UnityPlayer.dll').is_file() and (EXE.parent/'MonoBleedingEdge').is_dir(),
              'Runnable directory kept together.')
        if args.mode == 'smoke':
            save = out/'session'
            prepare = run('prepare', save, 'prepare')
            resume = run('resume', save, 'resume')
            old, new = prepare['final'], resume['initial']
            check('second-process-resumes-identical-transaction', old['transactionId'] == new['transactionId'] and
                  old['cardIds'] == new['cardIds'] and old['newToVirtual'] == new['newToVirtual'] and
                  old['revealedCount'] == new['revealedCount'] == 3 and old['revision'] == new['revision'],
                  'Independent player process, no reseeding/reordering/award.')
            check('resume-started-with-zero-generation', new['generatedResultCount'] == 0 and new['totalVirtualCards'] == 11)
            verify_state(save, 22, 2)
            error_save = out/'error-session'
            error = run('error', error_save, 'error')
            check('native-prepared-failure-retry-no-reroll', error['final']['generatedResultCount'] == 1 and
                  error['final']['totalVirtualCards'] == 11 and error['final']['revealedCount'] == 1)
            verify_state(error_save, 11, 1)
        elif args.mode == 'layouts':
            for width, height in [(1600,900), (2560,1080), (1280,360)]:
                name = str(width)+'x'+str(height)
                run('layout', out/('session-'+name), name, width, height)
        elif args.mode == 'input':
            process, stream, folder = launch('input', out/'input-session', 'input')
            probe = folder/'probe.json'
            try:
                state = read_json_eventually(probe, lambda s: s['stage'] == 'ready', 35)
                window = owned_window(process.pid, 15)
                if state['paused']:
                    state = native_click(window, probe, state, 'resume', lambda s: not s['paused'])
                state = native_click(window, probe, state, 'primary', lambda s: s['stage'] == 'sealed')
                tx = state['transactionId']
                state = native_click(window, probe, state, 'primary', lambda s: s['stage'] == 'revealing')
                state = native_click(window, probe, state, 'primary', lambda s: s['revealedCount'] == 1)
                state = native_click(window, probe, state, 'hero', lambda s: s['inspectIndex'] == 0)
                state = native_click(window, probe, state, 'back', lambda s: s['inspectIndex'] == -1)
                state = native_click(window, probe, state, 'pause', lambda s: s['paused'])
                state = native_click(window, probe, state, 'resume', lambda s: not s['paused'])
                state = native_click(window, probe, state, 'secondary', lambda s: s['stage'] == 'results')
                check('background-native-mouse-loop-confirmed', state['transactionId'] == tx and state['totalVirtualCards'] == 11 and state['generatedResultCount'] == 1,
                      'Observed actual GUI-button effects; no foreground focus or cursor move.')
                state = native_click(window, probe, state, 'result-7', lambda s: s['inspectIndex'] == 7)
                state = native_click(window, probe, state, 'back', lambda s: s['inspectIndex'] == -1)
                post_click(window, state, 'quit')
                finish_case(process, stream, folder, 1280, 720)
                verify_state(out/'input-session', 11, 1)
            finally:
                if process.poll() is None:
                    process.kill(); process.wait(timeout=10)
                stream.close()
        elif args.mode == 'crash':
            save = out/'crash-session'
            process, stream, folder = launch('input', save, 'before-kill')
            probe = folder/'probe.json'
            before_kill = None
            try:
                state = read_json_eventually(probe, lambda s: s['stage'] == 'ready', 35)
                window = owned_window(process.pid, 15)
                if state['paused']:
                    state = native_click(window, probe, state, 'resume', lambda s: not s['paused'])
                state = native_click(window, probe, state, 'primary', lambda s: s['stage'] == 'sealed')
                state = native_click(window, probe, state, 'primary', lambda s: s['stage'] == 'revealing')
                for target in range(1, 4):
                    state = native_click(window, probe, state, 'primary', lambda s, target=target: s['revealedCount'] == target)
                before_kill = state
                process.kill()  # Owned process only: no OnApplicationQuit or Shutdown opportunity.
                code = process.wait(timeout=10)
                check('actual-unity-player-forcibly-terminated', code != 0, 'Owned process killed after three acknowledged native-click reveals.')
                cases['before-kill'] = {'kind':'driver-hard-kill-observation', 'processExitCode':code, 'probe':before_kill,
                                        'note':'Pre-crash screenshot writes may be incomplete and are not counted as validated captures.'}
            finally:
                if process.poll() is None:
                    process.kill(); process.wait(timeout=10)
                stream.close()
            process, stream, folder = launch('input', save, 'after-kill')
            probe = folder/'probe.json'
            try:
                state = read_json_eventually(probe, lambda s: s['stage'] == 'revealing' and s['revealedCount'] == 3, 35)
                check('hard-kill-restart-preserves-exact-pack-and-awards',
                      all(state[key] == before_kill[key] for key in ['transactionId','cardIds','newToVirtual','revealedCount','revision']) and
                      state['generatedResultCount'] == 0 and state['totalVirtualCards'] == 11,
                      'Fresh Unity player recovered without a final-save callback, reroll or duplicate award.')
                window = owned_window(process.pid, 15)
                if state['paused']:
                    state = native_click(window, probe, state, 'resume', lambda s: not s['paused'])
                state = native_click(window, probe, state, 'primary', lambda s: s['revealedCount'] == 4)
                state = native_click(window, probe, state, 'secondary', lambda s: s['stage'] == 'results')
                check('hard-kill-resumed-native-input-continues', state['totalVirtualCards'] == 11 and state['generatedResultCount'] == 0)
                post_click(window, state, 'quit')
                finish_case(process, stream, folder, 1280, 720)
                verify_state(save, 11, 1)
            finally:
                if process.poll() is None:
                    process.kill(); process.wait(timeout=10)
                stream.close()
        else:
            save = out/'keyboard-session'
            process, stream, folder = launch('input', save, 'keyboard')
            probe = folder/'probe.json'
            try:
                state = read_json_eventually(probe, lambda s: s['stage'] == 'ready', 35)
                window = owned_window(process.pid, 15)
                if state['paused']:
                    state = native_click(window, probe, state, 'resume', lambda s: not s['paused'])
                post_escape(window)
                state = read_json_eventually(probe, lambda s: s['paused'], 8)
                state = native_click(window, probe, state, 'resume', lambda s: not s['paused'])
                state = native_click(window, probe, state, 'primary', lambda s: s['stage'] == 'sealed')
                state = native_click(window, probe, state, 'primary', lambda s: s['stage'] == 'revealing')
                state = native_click(window, probe, state, 'primary', lambda s: s['revealedCount'] == 1)
                revision = state['revision']
                state = native_click(window, probe, state, 'hero', lambda s: s['inspectIndex'] == 0)
                post_escape(window)
                state = read_json_eventually(probe, lambda s: s['inspectIndex'] == -1 and not s['paused'], 8)
                post_escape(window)
                state = read_json_eventually(probe, lambda s: s['paused'], 8)
                check('background-native-escape-back-and-pause-confirmed', state['revision'] == revision and state['totalVirtualCards'] == 11,
                      'Observed real native key events without writes, focus steal or global keyboard injection.')
                state = native_click(window, probe, state, 'resume', lambda s: not s['paused'])
                post_click(window, state, 'quit')
                finish_case(process, stream, folder, 1280, 720)
                verify_state(save, 11, 1)
            finally:
                if process.poll() is None:
                    process.kill(); process.wait(timeout=10)
                stream.close()
    except Exception as error:
        failure = repr(error)
        checks.append({'name':'verification-failure', 'passed':False, 'detail':failure})
    after = {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in source_paths}
    checks.append({'name':'approved-generator-data-unchanged', 'passed':before == after, 'detail':'Before/after SHA-256.'})
    report = {'passed':failure is None and all(c['passed'] for c in checks), 'mode':args.mode,
              'checkCount':len(checks), 'checks':checks, 'cases':cases,
              'screenshotCount':len(screenshots), 'screenshots':screenshots,
              'protectedBefore':before, 'protectedAfter':after}
    (out/'verification.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    print(json.dumps({'passed':report['passed'], 'mode':args.mode, 'checkCount':len(checks),
                      'screenshotCount':len(screenshots), 'failure':failure, 'report':str(out/'verification.json')}, indent=2))
    return 0 if report['passed'] else 1


def owned_window(pid, timeout):
    user32 = ctypes.WinDLL('user32', use_last_error=True)
    callback_type = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)
    user32.EnumWindows.argtypes = [callback_type, wintypes.LPARAM]
    user32.GetWindowThreadProcessId.argtypes = [wintypes.HWND, ctypes.POINTER(wintypes.DWORD)]
    user32.IsWindowVisible.argtypes = [wintypes.HWND]
    user32.IsWindowVisible.restype = wintypes.BOOL
    deadline = time.monotonic()+timeout
    while time.monotonic() < deadline:
        matches = []
        @callback_type
        def visit(hwnd, parameter):
            owner = wintypes.DWORD()
            user32.GetWindowThreadProcessId(hwnd, ctypes.byref(owner))
            if owner.value == pid and user32.IsWindowVisible(hwnd):
                matches.append(hwnd)
            return True
        user32.EnumWindows(visit, 0)
        if matches:
            return matches[0]
        time.sleep(0.1)
    raise RuntimeError('Owned visible Unity player window not found')


def post_click(hwnd, state, control):
    user32 = ctypes.WinDLL('user32', use_last_error=True)
    user32.PostMessageW.argtypes = [wintypes.HWND, wintypes.UINT, wintypes.WPARAM, wintypes.LPARAM]
    user32.PostMessageW.restype = wintypes.BOOL
    user32.GetClientRect.argtypes = [wintypes.HWND, ctypes.POINTER(wintypes.RECT)]
    rect = wintypes.RECT()
    if not user32.GetClientRect(hwnd, ctypes.byref(rect)):
        raise ctypes.WinError(ctypes.get_last_error())
    bounds = next(b for b in state['controls'] if b['name'] == control)
    x = round((bounds['x']+bounds['width']/2) * (rect.right-rect.left)/state['screenWidth'])
    y = round((bounds['y']+bounds['height']/2) * (rect.bottom-rect.top)/state['screenHeight'])
    position = (y << 16) | (x & 0xffff)
    for message, buttons in [(0x0200,0), (0x0201,1), (0x0202,0)]:
        if not user32.PostMessageW(hwnd, message, buttons, position):
            raise ctypes.WinError(ctypes.get_last_error())
        time.sleep(0.07)


def native_click(hwnd, probe, state, control, expected):
    post_click(hwnd, state, control)
    # Delivery success is NOT confirmation; read the real controller state back.
    return read_json_eventually(probe, expected, 8)


def post_escape(hwnd):
    user32 = ctypes.WinDLL('user32', use_last_error=True)
    user32.PostMessageW.argtypes = [wintypes.HWND, wintypes.UINT, wintypes.WPARAM, wintypes.LPARAM]
    user32.PostMessageW.restype = wintypes.BOOL
    user32.MapVirtualKeyW.argtypes = [wintypes.UINT, wintypes.UINT]
    scan = user32.MapVirtualKeyW(0x1b, 0)
    down = 1 | (scan << 16)
    for message, flags in [(0x0100, down), (0x0101, down | (1 << 30) | (1 << 31))]:
        if not user32.PostMessageW(hwnd, message, 0x1b, flags):
            raise ctypes.WinError(ctypes.get_last_error())
        time.sleep(0.07)


if __name__ == '__main__':
    sys.exit(main())
