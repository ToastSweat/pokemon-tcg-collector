"""Isolated supplied-art proposal. Import is pure; --run is an explicit future native gate."""
import sys
sys.dont_write_bytecode=True
import json,hashlib,struct,zlib,re,os,subprocess,time
from pathlib import Path
ROOT=Path('F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG')
HERE=Path(__file__).parent
GEN='b98f62fecb5239f3bbc7cf78273c2b8f209488f40d5362ba4519824a419058fb'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def marker(v,mode):
 if type(v)!=dict or set(v)!={'schemaVersion','owner','mode'} or type(v['schemaVersion'])!=int or v['schemaVersion']!=1 or v['owner']!='supplied-art-player' or v['mode']!=mode or mode not in ('art','reopen'):raise ValueError('Exact marker required')
 return v
def relative(n):
 if type(n)!=str or not n or '\\' in n or ':' in n or any(p in ('','.','..') for p in n.split('/')):raise ValueError('Unsafe member')
 return n
def png(b):
 if b[:8]!=b'\x89PNG\r\n\x1a\n':raise ValueError('PNG signature')
 p=8;dims=None;data=False
 while p+12<=len(b):
  n=struct.unpack_from('>I',b,p)[0];t=b[p+4:p+8];end=p+12+n
  if end>len(b) or zlib.crc32(b[p+4:p+8+n])!=struct.unpack_from('>I',b,p+8+n)[0]:raise ValueError('PNG CRC/truncation')
  if t==b'IHDR':
   if p!=8 or n!=13 or dims is not None:raise ValueError('PNG IHDR')
   dims=struct.unpack_from('>II',b,p+8)
  if t==b'IDAT':data=True
  p=end
  if t==b'IEND':
   if n or p!=len(b) or not data or dims is None or min(dims)<=0:raise ValueError('PNG incomplete')
   return dims
 raise ValueError('PNG missing IEND')
MANIFEST_SHA='0cb3940530050398754c8bcc649b8234bc18d44c04c3006cd1d14d0a743f6eb7'
def rows(m):
 if m['generationId']!=GEN:raise ValueError('Wrong generation')
 r=m['generationInput']['rows']
 expected=set(['pkmn-fossil-en-unlimited1999-001-holo-standard', 'pkmn-fossil-en-unlimited1999-002-holo-standard', 'pkmn-fossil-en-unlimited1999-003-holo-standard', 'pkmn-fossil-en-unlimited1999-004-holo-standard', 'pkmn-fossil-en-unlimited1999-005-holo-standard', 'pkmn-fossil-en-unlimited1999-006-holo-standard', 'pkmn-fossil-en-unlimited1999-007-holo-standard', 'pkmn-fossil-en-unlimited1999-008-holo-standard', 'pkmn-fossil-en-unlimited1999-009-holo-standard', 'pkmn-fossil-en-unlimited1999-010-holo-standard', 'pkmn-fossil-en-unlimited1999-011-holo-standard', 'pkmn-fossil-en-unlimited1999-012-holo-standard', 'pkmn-fossil-en-unlimited1999-013-holo-standard', 'pkmn-fossil-en-unlimited1999-014-holo-standard', 'pkmn-fossil-en-unlimited1999-015-holo-standard', 'pkmn-fossil-en-unlimited1999-016-nonholo-standard', 'pkmn-fossil-en-unlimited1999-017-nonholo-standard', 'pkmn-fossil-en-unlimited1999-018-nonholo-standard', 'pkmn-fossil-en-unlimited1999-019-nonholo-standard', 'pkmn-fossil-en-unlimited1999-020-nonholo-standard', 'pkmn-fossil-en-unlimited1999-021-nonholo-standard', 'pkmn-fossil-en-unlimited1999-022-nonholo-standard', 'pkmn-fossil-en-unlimited1999-023-nonholo-standard', 'pkmn-fossil-en-unlimited1999-024-nonholo-standard', 'pkmn-fossil-en-unlimited1999-025-nonholo-standard', 'pkmn-fossil-en-unlimited1999-026-nonholo-standard', 'pkmn-fossil-en-unlimited1999-027-nonholo-standard', 'pkmn-fossil-en-unlimited1999-028-nonholo-standard', 'pkmn-fossil-en-unlimited1999-029-nonholo-standard', 'pkmn-fossil-en-unlimited1999-030-nonholo-standard', 'pkmn-fossil-en-unlimited1999-031-nonholo-standard', 'pkmn-fossil-en-unlimited1999-032-nonholo-standard', 'pkmn-fossil-en-unlimited1999-033-nonholo-standard', 'pkmn-fossil-en-unlimited1999-034-nonholo-standard', 'pkmn-fossil-en-unlimited1999-035-nonholo-standard', 'pkmn-fossil-en-unlimited1999-036-nonholo-standard', 'pkmn-fossil-en-unlimited1999-037-nonholo-standard', 'pkmn-fossil-en-unlimited1999-038-nonholo-standard', 'pkmn-fossil-en-unlimited1999-039-nonholo-standard', 'pkmn-fossil-en-unlimited1999-040-nonholo-standard', 'pkmn-fossil-en-unlimited1999-041-nonholo-standard', 'pkmn-fossil-en-unlimited1999-042-nonholo-standard', 'pkmn-fossil-en-unlimited1999-043-nonholo-standard', 'pkmn-fossil-en-unlimited1999-044-nonholo-standard', 'pkmn-fossil-en-unlimited1999-045-nonholo-standard', 'pkmn-fossil-en-unlimited1999-046-nonholo-standard', 'pkmn-fossil-en-unlimited1999-047-nonholo-standard', 'pkmn-fossil-en-unlimited1999-048-nonholo-standard', 'pkmn-fossil-en-unlimited1999-049-nonholo-standard', 'pkmn-fossil-en-unlimited1999-050-nonholo-standard', 'pkmn-fossil-en-unlimited1999-051-nonholo-standard', 'pkmn-fossil-en-unlimited1999-052-nonholo-standard', 'pkmn-fossil-en-unlimited1999-053-nonholo-standard', 'pkmn-fossil-en-unlimited1999-054-nonholo-standard', 'pkmn-fossil-en-unlimited1999-055-nonholo-standard', 'pkmn-fossil-en-unlimited1999-056-nonholo-standard', 'pkmn-fossil-en-unlimited1999-057-nonholo-standard', 'pkmn-fossil-en-unlimited1999-058-nonholo-standard', 'pkmn-fossil-en-unlimited1999-059-nonholo-standard', 'pkmn-fossil-en-unlimited1999-060-nonholo-standard', 'pkmn-fossil-en-unlimited1999-061-nonholo-standard', 'pkmn-fossil-en-unlimited1999-062-nonholo-standard', 'pkmn-jungle-en-unlimited1999-001-holo-standard', 'pkmn-jungle-en-unlimited1999-002-holo-standard', 'pkmn-jungle-en-unlimited1999-003-holo-standard', 'pkmn-jungle-en-unlimited1999-004-holo-standard', 'pkmn-jungle-en-unlimited1999-005-holo-standard', 'pkmn-jungle-en-unlimited1999-006-holo-standard', 'pkmn-jungle-en-unlimited1999-007-holo-standard', 'pkmn-jungle-en-unlimited1999-008-holo-standard', 'pkmn-jungle-en-unlimited1999-009-holo-standard', 'pkmn-jungle-en-unlimited1999-010-holo-standard', 'pkmn-jungle-en-unlimited1999-011-holo-standard', 'pkmn-jungle-en-unlimited1999-012-holo-standard', 'pkmn-jungle-en-unlimited1999-013-holo-standard', 'pkmn-jungle-en-unlimited1999-014-holo-standard', 'pkmn-jungle-en-unlimited1999-015-holo-standard', 'pkmn-jungle-en-unlimited1999-016-holo-standard', 'pkmn-jungle-en-unlimited1999-017-nonholo-standard', 'pkmn-jungle-en-unlimited1999-018-nonholo-standard', 'pkmn-jungle-en-unlimited1999-019-nonholo-standard', 'pkmn-jungle-en-unlimited1999-020-nonholo-standard', 'pkmn-jungle-en-unlimited1999-021-nonholo-standard', 'pkmn-jungle-en-unlimited1999-022-nonholo-standard', 'pkmn-jungle-en-unlimited1999-023-nonholo-standard', 'pkmn-jungle-en-unlimited1999-024-nonholo-standard', 'pkmn-jungle-en-unlimited1999-025-nonholo-standard', 'pkmn-jungle-en-unlimited1999-026-nonholo-standard', 'pkmn-jungle-en-unlimited1999-027-nonholo-standard', 'pkmn-jungle-en-unlimited1999-028-nonholo-standard', 'pkmn-jungle-en-unlimited1999-029-nonholo-standard', 'pkmn-jungle-en-unlimited1999-030-nonholo-standard', 'pkmn-jungle-en-unlimited1999-031-nonholo-standard', 'pkmn-jungle-en-unlimited1999-032-nonholo-standard', 'pkmn-jungle-en-unlimited1999-033-nonholo-standard', 'pkmn-jungle-en-unlimited1999-034-nonholo-standard', 'pkmn-jungle-en-unlimited1999-035-nonholo-standard', 'pkmn-jungle-en-unlimited1999-036-nonholo-standard', 'pkmn-jungle-en-unlimited1999-037-nonholo-standard', 'pkmn-jungle-en-unlimited1999-038-nonholo-standard', 'pkmn-jungle-en-unlimited1999-039-nonholo-standard', 'pkmn-jungle-en-unlimited1999-040-nonholo-standard', 'pkmn-jungle-en-unlimited1999-041-nonholo-standard', 'pkmn-jungle-en-unlimited1999-042-nonholo-standard', 'pkmn-jungle-en-unlimited1999-043-nonholo-standard', 'pkmn-jungle-en-unlimited1999-044-nonholo-standard', 'pkmn-jungle-en-unlimited1999-045-nonholo-standard', 'pkmn-jungle-en-unlimited1999-046-nonholo-standard', 'pkmn-jungle-en-unlimited1999-047-nonholo-standard', 'pkmn-jungle-en-unlimited1999-048-nonholo-standard', 'pkmn-jungle-en-unlimited1999-049-nonholo-standard', 'pkmn-jungle-en-unlimited1999-050-nonholo-standard', 'pkmn-jungle-en-unlimited1999-051-nonholo-standard', 'pkmn-jungle-en-unlimited1999-052-nonholo-standard', 'pkmn-jungle-en-unlimited1999-053-nonholo-standard', 'pkmn-jungle-en-unlimited1999-054-nonholo-standard', 'pkmn-jungle-en-unlimited1999-055-nonholo-standard', 'pkmn-jungle-en-unlimited1999-056-nonholo-standard', 'pkmn-jungle-en-unlimited1999-057-nonholo-standard', 'pkmn-jungle-en-unlimited1999-058-nonholo-standard', 'pkmn-jungle-en-unlimited1999-059-nonholo-standard', 'pkmn-jungle-en-unlimited1999-060-nonholo-standard', 'pkmn-jungle-en-unlimited1999-061-nonholo-standard', 'pkmn-jungle-en-unlimited1999-062-nonholo-standard', 'pkmn-jungle-en-unlimited1999-063-nonholo-standard', 'pkmn-jungle-en-unlimited1999-064-nonholo-standard'])
 faces=[v['canonicalId'] for v in r if v['role']=='card-face']
 wrappers=[(v['printingId'],v.get('wrapperRole')) for v in r if v['role']=='wrapper-front']
 if len(faces)!=126 or set(faces)!=expected or len(wrappers)!=6 or set(wrappers)!={(s+'-en-unlimited1999',w) for s in ('jungle','fossil') for w in 'ABC'} or any(v['role'] not in ('card-face','wrapper-front') for v in r):raise ValueError('Canonical IDs and six wrapper roles')
 names=[relative(v['relativeAssetPath']) for v in r]
 if len(r)!=132 or len(set(names))!=132 or sum(v['role']=='card-face' for v in r)!=126:raise ValueError('Exact 132 roles')
 for v in r:
  if (v['width'],v['height'])!=((600,825) if v['role']=='card-face' else (260,475)) or not re.fullmatch('[0-9a-f]{64}',v['rgbaPixelSha256']):raise ValueError('Pixel provenance')
 return r
def validate(report,mode,manifest,directory):
 marker({'schemaVersion':1,'owner':'supplied-art-player','mode':mode},mode)
 if report.get('passed') is not True or report.get('mode')!=mode:raise ValueError('Native pass/mode')
 for count,key in [('checkCount','checks'),('screenshotCount','screenshots'),('wrapperCount','wrappers'),('seedCount','seeds'),('snapshotCount','snapshots')]:
  if type(report.get(count))!=int or report[count]!=len(report.get(key,[])):raise ValueError('Count reconciliation '+count)
 if not report['checks'] or any(v['passed'] is not True for v in report['checks']) or len({v['name'] for v in report['checks']})!=len(report['checks']):raise ValueError('Unique nonzero native checks')
 before=json.loads(report['initial']['sessionJson']);after=json.loads(report['final']['sessionJson'])
 if len(after['packs'])!=6 or report['final']['totalVirtualCards']!=66 or any(len(p['orderedCardIds'])!=11 for p in after['packs']):raise ValueError('Actual six-pack 66-card history')
 if mode=='reopen':
  if before!=after or report['final']['generatedResultCount']!=0:raise ValueError('Reload rerolled')
 else:
  if before['packs'] or report['final']['generatedResultCount']!=6 or len(report['wrappers'])!=6 or any(v['sameNativeReference'] is not True for v in report['wrappers']):raise ValueError('Native wrapper/history proof')
  if {(v['printingId'],v['index']) for v in report['wrappers']}!={(s,i) for s in ('jungle-en-unlimited1999','fossil-en-unlimited1999') for i in range(3)}:raise ValueError('All six wrappers required')
 if mode=='art':
  expected_wrappers={(v['printingId'],ord(v['wrapperRole'])-ord('A')):v['rgbaPixelSha256'] for v in rows(manifest) if v['role']=='wrapper-front'}
  if {(v['printingId'],v['index']):v['rgbaSha256'] for v in report['wrappers']}!=expected_wrappers:raise ValueError('All six native wrapper pixel hashes')
 expected={v['canonicalId']:v['rgbaPixelSha256'] for v in rows(manifest) if v['role']=='card-face'}
 observed={v['cardId']:v['rgbaSha256'] for v in report.get('faces',[])}
 if expected!=observed or len(report.get('faces',[]))!=126:raise ValueError('All native face pixels, top-down RGBA')
 captures={'reopened.png'} if mode=='reopen' else {'complete-six-packs.png'}|{f'wrapper-{s}-{i}.png' for s in (1,2) for i in range(3)}|{f'{n}-{s}.png' for s in (1,2) for n in ('card11','inspect')}
 required={'actual-ready-after-ordered-hosts','actual-host-ready','all126-native-faces','invalid-optional-profile-rejected'}|({'reload-six-packs'} if mode=='reopen' else {'fresh-empty-session'})|{'framebuffer:'+n[:-4] for n in captures}
 if len(set(report['screenshots']))!=len(report['screenshots']) or not captures<=set(report['screenshots']) or not required<={v['name'] for v in report['checks']}:raise ValueError('Unique required capture/check coverage')
 if report['seedCount']!=(0 if mode=='reopen' else 6) or report['snapshotCount']!=2:raise ValueError('Required seed/snapshot coverage')
 for n in report['screenshots']:
  relative(n);png((directory/n).read_bytes())
  sys.path.insert(0,str(ROOT/'tools'))
  from verify_tilt_player import validate_png
  if validate_png(directory/n)!=(report['final']['screenWidth'],report['final']['screenHeight']):raise ValueError('Framebuffer dimensions drift')
 if not report['screenshots']:raise ValueError('No native framebuffer')
 return after

def run(label,receipt_path,trusted_sha):
 # Explicit operational invocation only; never invoked during this proposal's tests.
 sys.path.insert(0,str(ROOT/'tools'));sys.path.insert(0,str(ROOT/'.artifacts/phase5-supplied-art-operative-01'))
 import supplied_art_guard as guard
 from phase5_owned_window_input import OwnedWindowInput
 from verify_phase3_player import owned_window
 from verify_tilt_player import read_eventually
 if not re.fullmatch('[A-Za-z0-9_-]+',label):raise ValueError('Fresh label')
 receipt=json.loads(Path(receipt_path).read_text());guard.validate_authorization(receipt,trusted_sha)
 if receipt['action']!='BuildWindows':raise ValueError('Build authorization required')
 build=json.loads((guard.Policy().stage/'.supplied-art-build-report.json').read_text())
 candidate=guard.Policy().candidate
 if type(build.get('warnings'))!=int or build['warnings']!=0 or type(build.get('errors'))!=int or build['errors']!=0 or build.get('passed') is not True or {v['path']:v['sha256'] for v in build['compilerPins']}!=guard.compiler_manifest(guard.Policy().stage) or {v['path']:v['sha256'] for v in build['candidatePins']}!=guard.player_manifest(candidate):raise ValueError('Fresh actual builder closure required')
 baseline=guard.authorized_protected_baseline(receipt,trusted_sha)['protectedBaseline'];before=guard.protected_snapshot(ROOT,baseline)
 directory=ROOT/'.artifacts/phase5-supplied-art-player'/label;guard.no_reparse(directory)
 if directory.exists():raise ValueError('Never overwrite run label')
 manifest_path=ROOT/'.artifacts/phase5-supplied-art-assets-work/manifest.json'
 if sha(manifest_path)!=MANIFEST_SHA:raise ValueError('Pinned expected-pixel manifest required')
 manifest=json.loads(manifest_path.read_text());rows(manifest);histories=[]
 directory.mkdir(parents=True);save=directory/'session';save.mkdir()
 try:
  for mode in ('art','reopen'):
   dest=directory/mode;dest.mkdir();(dest/'supplied-art-fixture.json').write_text(json.dumps({'schemaVersion':1,'owner':'supplied-art-player','mode':mode},separators=(',',':')))
   args=[str(candidate/'VintageCardCollector.exe'),'--vcc-smoke=input','--vcc-save-root='+str(save),'--vcc-artifacts='+str(dest),'-logFile',str(dest/'Player.log')]
   process=subprocess.Popen(args,cwd=candidate);normal=False
   try:
    hwnd=owned_window(process.pid,30);fence=OwnedWindowInput(process)
    # Fence ownership with a no-input WM_NULL message; every later control must use this same fence.
    fence.message(hwnd,0,0,0)
    (dest/'supplied-art-driver-ready.json').write_text('{"owner":"supplied-art-owned-window","pid":'+str(process.pid)+'}')
    process.wait(timeout=240);normal=process.returncode==0
    if not normal:raise ValueError('Native nonzero exit')
    report=read_eventually(dest/'report.json',lambda v:v.get('passed') is True,seconds=10)
    histories.append(validate(report,mode,manifest,dest))
    shutdown=json.loads((dest/'shutdown.json').read_text())
    if shutdown!={'released':True,'borrowedAlive':True}:raise ValueError('Production shutdown resource evidence')
   finally:
    if process.poll() is None:
     process.terminate()
     try:process.wait(timeout=15)
     except subprocess.TimeoutExpired:process.kill();process.wait(timeout=15)
    if not normal:raise RuntimeError('Watchdog/abnormal termination is never pass')
  if histories[0]!=histories[1]:raise ValueError('Separate-process durable history drift')
 finally:
  after=guard.protected_snapshot(ROOT,baseline)
  (directory/'preservation.json').write_text(json.dumps({'before':before,'after':after,'unchanged':before==after},indent=2))
  if before!=after:raise ValueError('Nine-player/original/Base preservation failed')
 return {'passed':True,'native':True,'modes':['art','reopen']}
if __name__=='__main__':
 import argparse
 p=argparse.ArgumentParser();p.add_argument('--run',action='store_true');p.add_argument('--label');p.add_argument('--authorization');p.add_argument('--trusted-sha256');a=p.parse_args()
 if not a.run:p.error('Explicit --run plus trusted parent authorization required')
 print(json.dumps(run(a.label,a.authorization,a.trusted_sha256),indent=2))
