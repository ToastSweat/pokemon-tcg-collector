"""Read-only preflight or exact-source isolated build; never controls the open main Editor."""
from pathlib import Path, PurePosixPath
import argparse
import hashlib
import json
import os
import re
import shutil
import stat
import subprocess
import sys
ROOT=Path(__file__).resolve().parents[1]
APPROVED=Path('F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG')
STAGE=ROOT/'.artifacts/tilt-build-project'
EDITOR=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Unity.exe')
EXCLUDED={'saves','library','builds','logs','temp','obj','.git'}

def no_reparse(path):
    for p in [*reversed(path.parents),path]:
        try: info=p.lstat()
        except FileNotFoundError:continue
        if info.st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT:raise ValueError('Reparse path refused: '+str(p))

def tree_manifest(directory,excluded=EXCLUDED):
    no_reparse(directory)
    result={}
    def visit(folder):
        for p in sorted(folder.iterdir()):
            # Prune before stat/descent/read. This is also the preservation-audit boundary.
            if p.name.casefold() in excluded:continue
            info=p.lstat()
            if info.st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT:raise ValueError('Reparse entry refused: '+str(p))
            if p.is_dir():visit(p)
            elif p.is_file():result[p.relative_to(directory).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest()
    if directory.is_dir():visit(directory)
    return result

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()

def source_manifest():
    return {folder+'/'+name:digest for folder in ['Assets','Packages','ProjectSettings'] for name,digest in tree_manifest(ROOT/folder).items()}

def preserved_players():
    result={};builds=ROOT/'Builds';no_reparse(builds)
    for p in sorted(builds.iterdir()) if builds.is_dir() else []:
        if p.name.casefold() in {'phase3tiltwindows','saves','logs'}:continue
        if p.is_dir():result[p.name]=tree_manifest(p,{'saves','logs'})
    return result

def admission():
    if ROOT!=APPROVED:raise ValueError('Only approved source root allowed')
    candidate=ROOT/'Builds/Phase3TiltWindows';no_reparse(candidate)
    if candidate.exists():
        if not candidate.is_dir():raise ValueError('Candidate is not a directory')
        for p in candidate.iterdir():
            if p.name.casefold()=='saves':
                no_reparse(p)
                if not p.is_dir() or next(p.iterdir(),None) is not None:raise ValueError('Populated candidate Saves blocks build; nothing modified')
    if sha(ROOT/'Assets/Supplied/AssetManifest.json')!='f75e096eae99b26eebb69e91729099ad2f1c71f7538658a6a79be4b278935fb8':raise ValueError('Accepted runtime manifest0 changed')
    return candidate

def check_scene():
    scene=ROOT/'Assets/VintageCollector/Scenes/BootstrapTilt.unity'
    if not scene.is_file():raise ValueError('Production Tilt scene is missing')
    text=scene.read_text(encoding='utf-8');scripts=[];guid_map={}
    for folder in ['Assets']:
        for relative in tree_manifest(ROOT/folder):
            if relative.endswith('.meta'):
                p=ROOT/folder/relative;m=re.search(r'^guid: ([a-f0-9]{32})$',p.read_text(encoding='utf-8'),re.M)
                if m:guid_map.setdefault(m[1],[]).append(p)
    for guid in re.findall(r'guid: ([a-f0-9]{32})',text):
        if guid.startswith('0000000000000000'):continue
        if len(guid_map.get(guid,[]))!=1:raise ValueError('Scene GUID not uniquely delivered: '+guid)
    for guid in re.findall(r'm_Script: \{fileID: 11500000, guid: ([a-f0-9]{32})',text):scripts.append(guid_map[guid][0].name[:-5])
    expected={'OpeningGame.cs','TabletopPresentation.cs','Phase3MediaHost.cs','Phase4CollectionHost.cs','BinderPresentation.cs','Phase4SmokeProbe.cs','MaterialsSmokeProbe.cs','TiltSmokeProbe.cs'}
    if set(scripts)!=expected or len(scripts)!=len(expected):raise ValueError('Production scene components differ: '+repr(scripts))
    if '  pointerTiltEnabled: 1' not in text or '  manifestRelativePath: Assets/Supplied/BaseSetMaterials/Manifest.json' not in text or '  fullBaseSetBudget: 1' not in text:raise ValueError('New scene must explicitly select actual manifest and full budget')
    return {'scene':str(scene.relative_to(ROOT)),'scripts':scripts,'guidClosure':True}

def require_source_asset_metas(pins):
    missing=sorted(name for name in pins if name.startswith('Assets/') and not name.endswith('.meta') and name+'.meta' not in pins)
    if missing:raise ValueError('Source asset metas required before native import; only folder metas may be generated: '+repr(missing))

BOOTSTRAP_META_ASSETS={
    'Assets/VintageCollector/Editor/TiltBuild.cs','Assets/VintageCollector/Editor/TiltBuildPolicy.cs',
    'Assets/VintageCollector/Editor/TiltPresentationChecks.cs','Assets/VintageCollector/Editor/TiltSurfaceChecks.cs',
    'Assets/VintageCollector/Presentation/HoverTiltMotion.cs','Assets/VintageCollector/Presentation/TiltedSurfaceRenderer.cs',
    'Assets/VintageCollector/Resources/Presentation/TiltedSurface.shader','Assets/VintageCollector/Runtime/TiltSmokeProbe.cs'}
OWNED_CS_METAS={n+'.meta' for n in BOOTSTRAP_META_ASSETS if n.endswith(('/TiltBuild.cs','/TiltBuildPolicy.cs','/TiltSmokeProbe.cs'))}

def require_bootstrap_meta_scope(pins):
    missing={n for n in pins if n.startswith('Assets/') and not n.endswith('.meta') and n+'.meta' not in pins}
    if missing-BOOTSTRAP_META_ASSETS:raise ValueError('Metadata bootstrap only permits exact new tilt assets: '+repr(sorted(missing-BOOTSTRAP_META_ASSETS)))
    return sorted(missing)

def stage_exact(pins,out,restage=False,prepare_scene=False):
    if prepare_scene:require_bootstrap_meta_scope(pins)
    else:require_source_asset_metas(pins)
    no_reparse(STAGE);marker=STAGE/'.tilt-owned-stage.json'
    expected={'purpose':'tilt-exact-source','source':str(ROOT)}
    if STAGE.exists():
        if not marker.is_file() or json.loads(marker.read_text())!=expected:raise ValueError('Unowned stage refused')
        if not restage:raise ValueError('Existing owned stage requires explicit --restage; previous logs remain')
    STAGE.mkdir(parents=True,exist_ok=True);marker.write_text(json.dumps(expected))
    for folder in ['Assets','Packages','ProjectSettings']:
        target=STAGE/folder
        if target.exists():shutil.rmtree(target)
    for relative in pins:
        target=STAGE/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(ROOT/relative,target)
    actual={folder+'/'+name:digest for folder in ['Assets','Packages','ProjectSettings'] for name,digest in tree_manifest(STAGE/folder).items()}
    if actual!=pins or source_manifest()!=pins:raise ValueError('Source changed during exact staging; do not build mixed inputs')
    (out/'staged-inputs.json').write_text(json.dumps({'stage':str(STAGE),'fileCount':len(pins),'files':pins},indent=2))

def verify_compiler_closure(source, compiled, stage):
    files={item['path']:item['sha256'] for item in compiled['files']}
    if compiled['fileCount']!=len(compiled['files']) or len(files)!=len(compiled['files']):
        raise ValueError('Compiler input count/duplicates')
    if any(files.get(name)!=digest for name,digest in source.items()):
        raise ValueError('Compiler changed delivered input')
    generated={}
    for name in sorted(set(files)-set(source)):
        relative=PurePosixPath(name)
        if not name.startswith('Assets/') or '\\' in name or relative.is_absolute() or any(p in ('.','..') for p in name.split('/')) or not name.endswith('.meta'):
            raise ValueError('Unexpected compiler input')
        path=stage/name
        no_reparse(path);no_reparse(path.with_suffix(''))
        if not path.with_suffix('').is_dir():raise ValueError('Compiler extra is not a folder meta')
        with path.open('rb') as stream:raw=stream.read(4097)
        text=raw.decode('utf-8',errors='strict')
        if len(raw)>4096 or len(re.findall(r'^guid: [a-f0-9]{32}$',text,re.M))!=1 or not re.search(r'^fileFormatVersion: 2$',text,re.M) or not re.search(r'^folderAsset: yes$',text,re.M) or not re.search(r'^DefaultImporter:$',text,re.M) or hashlib.sha256(raw).hexdigest()!=files[name]:
            raise ValueError('Compiler folder meta content/hash mismatch')
        generated[name]=files[name]
    derived={'sourceCount':len(source),'compilerCount':len(files),'allDeliveredInputsExact':True,'nativeGeneratedFolderMetas':generated}
    return derived

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--label',required=True)
    parser.add_argument('--build',action='store_true');parser.add_argument('--host-ready',action='store_true',help='Parent confirms host integration/native compilation coherent; do not freeze changing worker inputs')
    parser.add_argument('--restage',action='store_true');parser.add_argument('--prepare-scene',action='store_true',help='Generate owned scene in compiled isolated stage and copy only that scene/meta back to source');args=parser.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9_-]+',args.label):parser.error('Unsafe label')
    # Admission has no file/directory writes. Reject canary before even creating an evidence run.
    admission()
    out=ROOT/'.artifacts/tilt-build'/args.label;out.mkdir(parents=True,exist_ok=False)
    before=preserved_players();(out/'protected-before.json').write_text(json.dumps(before,indent=2))
    report={'passed':False,'built':False,'failure':None};runner_before=hashlib.sha256(Path(__file__).read_bytes()).hexdigest();report['runnerSha256']=runner_before;report['toolPins']={p.relative_to(ROOT).as_posix():sha(p) for p in sorted((ROOT/'tools').glob('*tilt*build*.py'))};pins=source_manifest();(out/'source-pins.json').write_text(json.dumps(pins,indent=2))
    try:
        if args.prepare_scene:
            if args.build:raise ValueError('--prepare-scene and --build are separate admission steps')
            if not args.host_ready:raise ValueError('Host readiness not acknowledged; no staging')
            missing_meta=require_bootstrap_meta_scope(pins)
            stage_exact(pins,out,args.restage,prepare_scene=True)
            tmp=out/'tmp';tmp.mkdir();env=dict(os.environ,TMP=str(tmp),TEMP=str(tmp),TMPDIR=str(tmp),PYTHONDONTWRITEBYTECODE='1')
            command=[str(EDITOR),'-batchmode','-nographics','-quit','-projectPath',str(STAGE),'-executeMethod','VintageCollector.EditorTools.TiltBuild.CreateBootstrap','-logFile',str(out/'unity-scene.log')]
            (out/'command.json').write_text(json.dumps(command,indent=2));result=subprocess.run(command,cwd=ROOT,env=env,timeout=600)
            report['editorExit']=result.returncode
            if result.returncode:raise ValueError('Native scene preparation failed; retained attempt')
            if source_manifest()!=pins:raise ValueError('Source changed during native scene generation; nothing copied back')
            native_meta={n+'.meta':{'stagePath':str(STAGE/(n+'.meta')),'sha256':sha(STAGE/(n+'.meta')),'sourceSha256':pins[n]} for n in missing_meta}
            (out/'native-source-meta-generated.json').write_text(json.dumps(native_meta,indent=2))
            # Only this worker's new CS metas are adopted. Foreign worker metadata is left to its owner/parent.
            for relative in sorted(OWNED_CS_METAS & set(native_meta)):
                shutil.copy2(STAGE/relative,ROOT/relative)
            for relative in ['Assets/VintageCollector/Scenes/BootstrapTilt.unity','Assets/VintageCollector/Scenes/BootstrapTilt.unity.meta']:
                if not (STAGE/relative).is_file():raise ValueError('Native scene/meta missing')
                shutil.copy2(STAGE/relative,ROOT/relative)
            report['scene']=check_scene()
            report['preparedScene']=True
            (out/'generated-source-pins.json').write_text(json.dumps(source_manifest(),indent=2))
        else:report['scene']=check_scene()
        if args.build:
            if not args.host_ready:raise ValueError('Host readiness not acknowledged; no stage or build mutation')
            stage_exact(pins,out,args.restage)
            tmp=out/'tmp';tmp.mkdir();env=dict(os.environ,TMP=str(tmp),TEMP=str(tmp),TMPDIR=str(tmp),PYTHONDONTWRITEBYTECODE='1')
            command=[str(EDITOR),'-batchmode','-nographics','-quit','-projectPath',str(STAGE),'-executeMethod','VintageCollector.EditorTools.TiltBuild.BuildWindows','-logFile',str(out/'unity-build.log')]
            (out/'command.json').write_text(json.dumps(command,indent=2));result=subprocess.run(command,cwd=ROOT,env=env,timeout=600)
            report['editorExit']=result.returncode
            shared=ROOT/'.artifacts/tilt-build/editor-build-report.json'
            if not shared.is_file() or shared.stat().st_mtime < (out/'command.json').stat().st_mtime:raise ValueError('No fresh BuildReport')
            report['buildReport']=json.loads(shared.read_text());shutil.copy2(shared,out/'build-report.json')
            compiled=ROOT/'.artifacts/tilt-build/editor-compiled-inputs.json';shutil.copy2(compiled,out/'compiled-inputs.json')
            report['compiledClosure']=verify_compiler_closure(pins,json.loads(compiled.read_text()),STAGE)
            (out/'post-build-stage-pins.json').write_text(json.dumps({folder+'/'+name:digest for folder in ['Assets','Packages','ProjectSettings'] for name,digest in tree_manifest(STAGE/folder).items()},indent=2))
            actual={folder+'/'+name:digest for folder in ['Assets','Packages','ProjectSettings'] for name,digest in tree_manifest(STAGE/folder).items()}
            if actual!={item['path']:item['sha256'] for item in json.loads(compiled.read_text())['files']}:raise ValueError('Stage compiler closure drift after build')
            report['compilerAssemblies']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (STAGE/'Library/ScriptAssemblies').glob('Assembly-CSharp*.dll')}
            if result.returncode or report['buildReport']['result']!='Succeeded' or report['buildReport']['errors']:raise ValueError('Native Unity build failed; retained attempt')
            if source_manifest()!=pins:raise ValueError('Main source changed during build; candidate is not current')
            report['built']=True;report['candidateManifest']=tree_manifest(ROOT/'Builds/Phase3TiltWindows',{'saves','logs'})
        if hashlib.sha256(Path(__file__).read_bytes()).hexdigest()!=runner_before:raise ValueError('Build runner changed during execution')
        report['passed']=True
    except Exception as error:report['failure']=repr(error)
    finally:
        after=preserved_players();(out/'protected-after.json').write_text(json.dumps(after,indent=2));report['olderPlayersUnchanged']=before==after
        report['passed']=report['passed'] and before==after
        (out/'results.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
    return 0 if report['passed'] else 1
if __name__=='__main__':sys.exit(main())