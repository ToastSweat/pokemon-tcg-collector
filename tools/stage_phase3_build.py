"""Stage exact source inputs while leaving the interactive original Unity project open."""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess

ROOT=Path(__file__).resolve().parents[1]
STAGE=ROOT/'.artifacts/phase3-build-project'
STAGE.resolve().relative_to(ROOT.resolve())
marker=STAGE/'.vcc-owned-stage.json'
if STAGE.exists() and not marker.is_file():
    raise RuntimeError('Existing stage has no ownership marker')
STAGE.mkdir(parents=True,exist_ok=True)
marker.write_text(json.dumps({'purpose':'exact-source-phase3-build','source':str(ROOT)}),encoding='utf-8')
paths=[p for p in (ROOT/'Assets/VintageCollector').rglob('*') if p.is_file()]
for relative in ['Assets/VintageCollector.meta','Assets/Supplied/AssetManifest.json','Assets/Supplied.meta','Packages/manifest.json','Packages/packages-lock.json']:
    p=ROOT/relative
    if p.is_file(): paths.append(p)
tracked=subprocess.check_output(['git','ls-files','ProjectSettings'],cwd=ROOT).decode().splitlines()
paths += [ROOT/p for p in tracked]
paths=sorted(set(paths))
before={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
for p in paths:
    target=STAGE/p.relative_to(ROOT)
    target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copy2(p,target)
for relative,digest in before.items():
    if hashlib.sha256((ROOT/relative).read_bytes()).hexdigest()!=digest or hashlib.sha256((STAGE/relative).read_bytes()).hexdigest()!=digest:
        raise RuntimeError('Source changed during staging: '+relative)
(STAGE/'.artifacts').mkdir(exist_ok=True)
(ROOT/'.artifacts/phase3-build').mkdir(exist_ok=True)
(ROOT/'.artifacts/phase3-build/staged-inputs.json').write_text(json.dumps({'stage':str(STAGE),'fileCount':len(before),'files':before},indent=2),encoding='utf-8')
print(json.dumps({'stage':str(STAGE),'byteVerifiedInputCount':len(before),'interactiveProjectTouched':False,'userSettingsCopied':False},indent=2))
