"""Read-only preflight or exact-source isolated build; never controls the open main Editor."""
from pathlib import Path
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
STAGE=ROOT/'.artifacts/phase4-build-project'
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

def source_manifest():
    return {folder+'/'+name:digest for folder in ['Assets','Packages','ProjectSettings'] for name,digest in tree_manifest(ROOT/folder).items()}

def preserved_players():
    result={};builds=ROOT/'Builds';no_reparse(builds)
    for p in sorted(builds.iterdir()) if builds.is_dir() else []:
        if p.name.casefold() in {'phase4windows','saves','logs'}:continue
        if p.is_dir():result[p.name]=tree_manifest(p,{'saves','logs'})
    return result

def admission():
    if ROOT!=APPROVED:raise ValueError('Only approved source root allowed')
    candidate=ROOT/'Builds/Phase4Windows';no_reparse(candidate)
    if candidate.exists():
        if not candidate.is_dir():raise ValueError('Candidate is not a directory')
        for p in candidate.iterdir():
            if p.name.casefold()=='saves':
                no_reparse(p)
                if not p.is_dir() or next(p.iterdir(),None) is not None:raise ValueError('Populated candidate Saves blocks build; nothing modified')
    return candidate

def check_scene():
    scene=ROOT/'Assets/VintageCollector/Scenes/BootstrapPhase4.unity'
    if not scene.is_file():raise ValueError('Production Phase 4 scene is missing')
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
    expected={'OpeningGame.cs','TabletopPresentation.cs','Phase3MediaHost.cs','Phase4CollectionHost.cs','BinderPresentation.cs','Phase4SmokeProbe.cs'}
    if set(scripts)!=expected or len(scripts)!=len(expected):raise ValueError('Production scene components differ: '+repr(scripts))
    return {'scene':str(scene.relative_to(ROOT)),'scripts':scripts,'guidClosure':True}

def stage_exact(pins,out,restage=False):
    no_reparse(STAGE);marker=STAGE/'.phase4-owned-stage.json'
    expected={'purpose':'phase4-exact-source','source':str(ROOT)}
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

def verify_compiled_inputs(pins,compiled):
    files={item["path"]:item["sha256"] for item in compiled["files"]}
    if compiled["fileCount"]!=len(compiled["files"]) or len(files)!=len(compiled["files"]):raise ValueError("Compiler input count/duplicates")
    changed=[name for name,digest in pins.items() if files.get(name)!=digest]
    if changed:raise ValueError("Compiler changed delivered inputs: "+repr(changed))
    generated={}
    for name in sorted(set(files)-set(pins)):
        path=STAGE/name
        if not name.endswith(".meta") or not path.with_suffix("").is_dir() or "folderAsset: yes" not in path.read_text(encoding="utf-8"):
            raise ValueError("Unexpected compiler input: "+name)
        generated[name]=files[name]
    return {"sourceCount":len(pins),"compilerCount":len(files),"allDeliveredInputsExact":True,"nativeGeneratedFolderMetas":generated}

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--label',required=True)
    parser.add_argument('--build',action='store_true');parser.add_argument('--host-ready',action='store_true',help='Parent confirms host integration/native compilation coherent; do not freeze changing worker inputs')
    parser.add_argument('--restage',action='store_true');parser.add_argument('--prepare-scene',action='store_true',help='Generate owned scene in compiled isolated stage and copy only that scene/meta back to source');args=parser.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9_-]+',args.label):parser.error('Unsafe label')
    # Admission has no file/directory writes. Reject canary before even creating an evidence run.
    admission()
    out=ROOT/'.artifacts/phase4-build'/args.label;out.mkdir(parents=True,exist_ok=False)
    before=preserved_players();(out/'protected-before.json').write_text(json.dumps(before,indent=2))
    report={'passed':False,'built':False,'failure':None};runner_before=hashlib.sha256(Path(__file__).read_bytes()).hexdigest();report['runnerSha256']=runner_before;pins=source_manifest();(out/'source-pins.json').write_text(json.dumps(pins,indent=2))
    try:
        if args.prepare_scene:
            if args.build:raise ValueError('--prepare-scene and --build are separate admission steps')
            if not args.host_ready:raise ValueError('Host readiness not acknowledged; no staging')
            stage_exact(pins,out,args.restage)
            tmp=out/'tmp';tmp.mkdir();env=dict(os.environ,TMP=str(tmp),TEMP=str(tmp),TMPDIR=str(tmp),PYTHONDONTWRITEBYTECODE='1')
            command=[str(EDITOR),'-batchmode','-nographics','-quit','-projectPath',str(STAGE),'-executeMethod','VintageCollector.EditorTools.Phase4Build.CreateBootstrap','-logFile',str(out/'unity-scene.log')]
            (out/'command.json').write_text(json.dumps(command,indent=2));result=subprocess.run(command,cwd=ROOT,env=env,timeout=600)
            report['editorExit']=result.returncode
            if result.returncode:raise ValueError('Native scene preparation failed; retained attempt')
            for relative in ['Assets/VintageCollector/Scenes/BootstrapPhase4.unity','Assets/VintageCollector/Scenes/BootstrapPhase4.unity.meta']:
                if not (STAGE/relative).is_file():raise ValueError('Native scene/meta missing')
                shutil.copy2(STAGE/relative,ROOT/relative)
            report['scene']=check_scene()
            report['preparedScene']=True
        else:report['scene']=check_scene()
        if args.build:
            if not args.host_ready:raise ValueError('Host readiness not acknowledged; no stage or build mutation')
            stage_exact(pins,out,args.restage)
            tmp=out/'tmp';tmp.mkdir();env=dict(os.environ,TMP=str(tmp),TEMP=str(tmp),TMPDIR=str(tmp),PYTHONDONTWRITEBYTECODE='1')
            command=[str(EDITOR),'-batchmode','-nographics','-quit','-projectPath',str(STAGE),'-executeMethod','VintageCollector.EditorTools.Phase4Build.BuildWindows','-logFile',str(out/'unity-build.log')]
            (out/'command.json').write_text(json.dumps(command,indent=2));result=subprocess.run(command,cwd=ROOT,env=env,timeout=600)
            report['editorExit']=result.returncode
            shared=ROOT/'.artifacts/phase4-build/editor-build-report.json'
            if not shared.is_file() or shared.stat().st_mtime < (out/'command.json').stat().st_mtime:raise ValueError('No fresh BuildReport')
            report['buildReport']=json.loads(shared.read_text());shutil.copy2(shared,out/'build-report.json')
            compiled=ROOT/'.artifacts/phase4-build/editor-compiled-inputs.json';shutil.copy2(compiled,out/'compiled-inputs.json')
            report['compiledClosure']=verify_compiled_inputs(pins,json.loads(compiled.read_text()))
            (out/'post-build-stage-pins.json').write_text(json.dumps({folder+'/'+name:digest for folder in ['Assets','Packages','ProjectSettings'] for name,digest in tree_manifest(STAGE/folder).items()},indent=2))
            report['compilerAssemblies']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (STAGE/'Library/ScriptAssemblies').glob('Assembly-CSharp*.dll')}
            if result.returncode or report['buildReport']['result']!='Succeeded' or report['buildReport']['errors']:raise ValueError('Native Unity build failed; retained attempt')
            if source_manifest()!=pins:raise ValueError('Main source changed during build; candidate is not current')
            report['built']=True;report['candidateManifest']=tree_manifest(ROOT/'Builds/Phase4Windows',{'saves','logs'})
        if hashlib.sha256(Path(__file__).read_bytes()).hexdigest()!=runner_before:raise ValueError('Build runner changed during execution')
        report['passed']=True
    except Exception as error:report['failure']=repr(error)
    finally:
        after=preserved_players();(out/'protected-after.json').write_text(json.dumps(after,indent=2));report['olderPlayersUnchanged']=before==after
        report['passed']=report['passed'] and before==after
        (out/'results.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
    return 0 if report['passed'] else 1
if __name__=='__main__':sys.exit(main())
