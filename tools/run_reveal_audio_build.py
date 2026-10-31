"""Exact-source reveal-audio candidate. Main project is read-only except explicit new scene/meta adoption."""
from pathlib import Path
import json
import re
import sys
sys.dont_write_bytecode=True
from run_tilt_build import no_reparse, tree_manifest, sha, require_source_asset_metas
ROOT=Path(__file__).resolve().parents[1]
APPROVED=Path('F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG')
STAGE=ROOT/'.artifacts/reveal-audio-build-project'
CANDIDATE=ROOT/'Builds/Phase3RevealAudioWindows'
EDITOR=Path('C:/Program Files/Unity/Hub/Editor/6000.2.14f1/Editor/Unity.exe')
SCENE='Assets/VintageCollector/Scenes/BootstrapRevealAudio.unity'

def compiler_manifest(project):
    # Compiler inputs are not player-preservation trees: even Assets/Logs/*.cs can compile.
    return {folder+'/'+name:digest for folder in ['Assets','Packages','ProjectSettings'] for name,digest in tree_manifest(project/folder,set()).items()}

def source_manifest():return compiler_manifest(ROOT)

def require_owned_stage(project):
    no_reparse(project)
    if project.resolve()!=STAGE.resolve():raise ValueError('Reveal audio requires exact owned stage, never the main project')
    return project

def verify_compiler_closure(source, compiled, stage):
    from run_tilt_build import verify_compiler_closure as inherited_closure
    actual=compiler_manifest(stage)
    declared={item['path']:item['sha256'] for item in compiled['files']}
    if actual!=declared:raise ValueError('Independent complete compiler input closure differs from report')
    return inherited_closure(source,compiled,stage)

def stage_exact(pins,out,restage=False,prepare_scene=False):
    import shutil
    if prepare_scene:
        missing={n for n in pins if n.startswith('Assets/') and not n.endswith('.meta') and n+'.meta' not in pins}
        allowed={'Assets/VintageCollector/Editor/RevealAudioBuild.cs','Assets/VintageCollector/Editor/RevealAudioBuildPolicy.cs','Assets/VintageCollector/Editor/RevealAudioNativeChecks.cs','Assets/VintageCollector/Runtime/RevealAudioSmokeProbe.cs'}
        if missing-allowed:raise ValueError('Only owned reveal audio metadata may bootstrap: '+repr(sorted(missing-allowed)))
    else:require_source_asset_metas(pins)
    no_reparse(STAGE);marker=STAGE/'.reveal-audio-owned-stage.json'
    expected={'purpose':'reveal-audio-exact-source','source':str(ROOT)}
    if STAGE.exists():
        if not marker.is_file() or json.loads(marker.read_text())!=expected:raise ValueError('Unowned stage refused')
        # Inspect every compiler root before any stage write/removal. Package assembly
        # definitions can make code Editor-capable without an Editor path segment.
        staged=compiler_manifest(STAGE)
        extras=[n for n in staged if Path(n).suffix.casefold() in {'.cs','.asmdef','.asmref','.dll','.rsp'} and n not in pins]
        if extras:raise ValueError('Unexpected staged Editor-capable compiler inputs; stage left untouched: '+repr(extras))
        if not restage:raise ValueError('Existing stage requires explicit --restage')
    STAGE.mkdir(parents=True,exist_ok=True);marker.write_text(json.dumps(expected))
    for folder in ['Assets','Packages','ProjectSettings']:
        target=STAGE/folder
        if target.exists():shutil.rmtree(target)
    for relative in pins:
        target=STAGE/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(ROOT/relative,target)
    actual=compiler_manifest(STAGE)
    if actual!=pins or source_manifest()!=pins:raise ValueError('Source drift during exact staging')
    (out/'staged-inputs.json').write_text(json.dumps({'stage':str(STAGE),'files':pins,'fileCount':len(pins)},indent=2))

def check_scene(scene=None):
    scene=scene or ROOT/SCENE
    if not scene.is_file():raise ValueError('RevealAudioHost/RevealAudioProfile/RevealAudioSmokeProbe scene missing')
    text=scene.read_text(encoding='utf-8');guid_map={}
    for relative in tree_manifest(ROOT/'Assets',set()):
        if relative.endswith('.meta'):
            path=ROOT/'Assets'/relative
            match=re.search(r'^guid: ([a-f0-9]{32})$',path.read_text(encoding='utf-8'),re.M)
            if match:guid_map.setdefault(match[1],[]).append(path)
    for guid in re.findall(r'guid: ([a-f0-9]{32})',text):
        if not guid.startswith('0000000000000000') and len(guid_map.get(guid,[]))!=1:raise ValueError('Scene GUID not uniquely delivered: '+guid)
    scripts=[guid_map[g][0].name[:-5] for g in re.findall(r'm_Script: \{fileID: 11500000, guid: ([a-f0-9]{32})',text)]
    expected={'OpeningGame.cs','TabletopPresentation.cs','Phase3MediaHost.cs','Phase4CollectionHost.cs','BinderPresentation.cs','Phase4SmokeProbe.cs','MaterialsSmokeProbe.cs','TiltSmokeProbe.cs','RevealAudioHost.cs','RevealAudioSmokeProbe.cs'}
    if set(scripts)!=expected or len(scripts)!=len(expected):raise ValueError('RevealAudioHost/RevealAudioSmokeProbe exact scene component closure required: '+repr(scripts))
    match=re.search(r'^  profile: \{fileID: 11400000, guid: ([a-f0-9]{32}), type: 2\}$',text,re.M)
    if not match:raise ValueError('RevealAudioProfile serialized profile reference missing')
    profile=guid_map[match[1]][0].with_suffix('')
    body=profile.read_text(encoding='utf-8')
    script=re.search(r'm_Script: \{fileID: 11500000, guid: ([a-f0-9]{32})',body)
    if not script or len(guid_map.get(script[1],[]))!=1 or guid_map[script[1]][0].name!='RevealAudioProfile.cs.meta':raise ValueError('Current RevealAudioProfile script closure required')
    if '  rareRevealPosition: 8' not in body:raise ValueError('Rare position must remain eight')
    clip_guids=re.findall(r'fileID: 8300000, guid: ([a-f0-9]{32}), type: 3',body)
    if len(clip_guids)!=14 or len(set(clip_guids))!=14:raise ValueError('Exactly fourteen distinct imported clip references required')
    for g in clip_guids:
        if len(guid_map.get(g,[]))!=1 or guid_map[g][0].with_suffix('').suffix.lower()!='.wav':raise ValueError('Imported WAV GUID closure missing')
    for required in ['  pointerTiltEnabled: 1','  manifestRelativePath: Assets/Supplied/BaseSetMaterials/Manifest.json','  fullBaseSetBudget: 1']:
        if required not in text:raise ValueError('Accepted tilt/art/full quality configuration not retained')
    return {'scene':scene.relative_to(ROOT).as_posix(),'scripts':scripts,'profile':profile.relative_to(ROOT).as_posix(),'clipGuids':clip_guids,'guidClosure':True}

def preserved_players():
    result={};builds=ROOT/'Builds';no_reparse(builds)
    for path in sorted(builds.iterdir()) if builds.is_dir() else []:
        if path.name.casefold() in {'phase3revealaudiowindows','saves','logs'}:continue
        no_reparse(path)
        if path.is_dir():result[path.name]=tree_manifest(path,{'saves','logs'})
    return result

def admission():
    if ROOT!=APPROVED:raise ValueError('Only exact approved source root allowed')
    no_reparse(CANDIDATE)
    if CANDIDATE.exists():
        if not CANDIDATE.is_dir():raise ValueError('Candidate output is a file')
        for path in CANDIDATE.iterdir():
            if path.name.casefold()=='saves':
                no_reparse(path)
                if not path.is_dir() or next(path.iterdir(),None) is not None:raise ValueError('Populated candidate Saves blocks build without mutation')
    for name,expected in {'Assets/Supplied/AssetManifest.json':'f75e096eae99b26eebb69e91729099ad2f1c71f7538658a6a79be4b278935fb8','Assets/Supplied/BaseSetMaterials/Manifest.json':'7fef4c77cb34195b4195715fa99c6c01c400ee31bf73f4a5ede963d86eee93be'}.items():
        if sha(ROOT/name)!=expected:raise ValueError('Accepted runtime manifest changed: '+name)
    return CANDIDATE

def require_assembled_sources():
    for name in ['Assets/VintageCollector/Audio/RevealAudioHost.cs','Assets/VintageCollector/Audio/RevealAudioProfile.cs','Assets/VintageCollector/Runtime/RevealAudioSmokeProbe.cs']:
        if not (ROOT/name).is_file():raise ValueError('BLOCKED assembled source dependency missing: '+name)
    text=(ROOT/'Assets/VintageCollector/Presentation/TabletopPresentation.cs').read_text()
    if 'IRevealSoundMedia RevealSounds' not in text:raise ValueError('BLOCKED parent renderer integration missing')
    assets=list((ROOT/'Assets/VintageCollector/Audio').rglob('*.asset'))
    if len(assets)!=1 or assets[0]!=ROOT/'Assets/VintageCollector/Audio/DefaultRevealAudioProfile.asset':raise ValueError('BLOCKED frozen DefaultRevealAudioProfile.asset missing or ambiguous')

def main():
    import argparse,os,shutil,subprocess
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--label',required=True)
    parser.add_argument('--build',action='store_true');parser.add_argument('--prepare-scene',action='store_true');parser.add_argument('--host-ready',action='store_true',help='Parent explicitly confirms all workers finished; do not freeze changing source')
    parser.add_argument('--restage',action='store_true');args=parser.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9_-]+',args.label):parser.error('Unsafe label')
    admission() # zero writes before canonical candidate/save admission
    out=ROOT/'.artifacts/reveal-audio-build'/args.label;no_reparse(out);out.mkdir(parents=True,exist_ok=False)
    before=preserved_players();(out/'protected-before.json').write_text(json.dumps(before,indent=2))
    tools=['tools/run_reveal_audio_build.py','tools/run_tilt_build.py'];tool_pins={n:sha(ROOT/n) for n in tools}
    pins=source_manifest();(out/'source-pins.json').write_text(json.dumps(pins,indent=2));(out/'tool-pins.json').write_text(json.dumps(tool_pins,indent=2))
    report={'passed':False,'built':False,'failure':None,'toolPins':tool_pins}
    try:
        if args.build and args.prepare_scene:raise ValueError('Scene preparation and final build are separate admissions')
        if args.build or args.prepare_scene:
            if not args.host_ready:raise ValueError('BLOCKED workers-ready acknowledgement absent; no stage mutation')
            require_assembled_sources()
            if not args.prepare_scene:report['scene']=check_scene()
            stage_exact(pins,out,args.restage,args.prepare_scene)
            tmp=out/'tmp';tmp.mkdir();env=dict(os.environ,TMP=str(tmp),TEMP=str(tmp),TMPDIR=str(tmp),PYTHONDONTWRITEBYTECODE='1')
            method='CreateBootstrap' if args.prepare_scene else 'BuildWindows'
            command=[str(EDITOR),'-batchmode','-nographics','-quit','-projectPath',str(STAGE),'-executeMethod','VintageCollector.EditorTools.RevealAudioBuild.'+method,'-logFile',str(out/'unity.log')]
            (out/'command.json').write_text(json.dumps(command,indent=2));result=subprocess.run(command,cwd=ROOT,env=env,timeout=600);report['editorExit']=result.returncode
            if result.returncode:raise ValueError('Native Unity operation failed; genuine output retained')
            if source_manifest()!=pins:raise ValueError('Source changed during native operation; not current exact source')
            shared=ROOT/'.artifacts/reveal-audio-build'
            profile=shared/'editor-audio-profile.json'
            if not profile.is_file() or profile.stat().st_mtime<(out/'command.json').stat().st_mtime:raise ValueError('No fresh native imported profile validation')
            shutil.copy2(profile,out/'audio-profile.json');report['audioProfile']=json.loads(profile.read_text())
            if args.prepare_scene:
                owned=['Assets/VintageCollector/Editor/RevealAudioBuild.cs','Assets/VintageCollector/Editor/RevealAudioBuildPolicy.cs','Assets/VintageCollector/Editor/RevealAudioNativeChecks.cs','Assets/VintageCollector/Runtime/RevealAudioSmokeProbe.cs']
                generated={}
                for name in owned:
                    meta=name+'.meta'
                    if meta not in pins:
                        if sha(ROOT/name)!=pins[name] or not (STAGE/meta).is_file():raise ValueError('Native metadata provenance mismatch')
                        generated[meta]={'sourceSha256':pins[name],'stageSha256':sha(STAGE/meta),'stagePath':str(STAGE/meta)}
                        shutil.copy2(STAGE/meta,ROOT/meta)
                for relative in [SCENE,SCENE+'.meta']:
                    if not (STAGE/relative).is_file():raise ValueError('Native scene/meta missing')
                    shutil.copy2(STAGE/relative,ROOT/relative)
                (out/'native-meta-provenance.json').write_text(json.dumps(generated,indent=2))
                report['scene']=check_scene();report['preparedScene']=True
                (out/'generated-source-pins.json').write_text(json.dumps(source_manifest(),indent=2))
            else:
                for name,target in [('editor-build-report.json','build-report.json'),('editor-compiled-inputs.json','compiled-inputs.json')]:
                    path=shared/name
                    if not path.is_file() or path.stat().st_mtime<(out/'command.json').stat().st_mtime:raise ValueError('No fresh native report: '+name)
                    shutil.copy2(path,out/target)
                build=json.loads((out/'build-report.json').read_text());compiled=json.loads((out/'compiled-inputs.json').read_text())
                report['buildReport']=build;report['compiledClosure']=verify_compiler_closure(pins,compiled,STAGE)
                (out/'post-build-stage-pins.json').write_text(json.dumps(compiler_manifest(STAGE),indent=2))
                report['compilerAssemblies']={p.name:sha(p) for p in (STAGE/'Library/ScriptAssemblies').glob('Assembly-CSharp*.dll')}
                if build['result']!='Succeeded' or build['errors']:raise ValueError('Native build did not succeed')
                report['built']=True;report['candidateManifest']=tree_manifest(CANDIDATE,{'saves','logs'})
        else:report['scene']=check_scene()
        if any(sha(ROOT/n)!=v for n,v in tool_pins.items()):raise ValueError('Runner/helper changed during invocation')
        report['passed']=True
    except Exception as error:report['failure']=repr(error)
    finally:
        after=preserved_players();(out/'protected-after.json').write_text(json.dumps(after,indent=2));report['olderPlayersUnchanged']=before==after
        report['passed']=report['passed'] and before==after
        (out/'results.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
    return 0 if report['passed'] else 1
if __name__=='__main__':sys.exit(main())
