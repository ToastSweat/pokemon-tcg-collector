"""Read-only Phase 4 candidate/evidence audit; writes only a fresh scoped report. Never traverses Saves."""
from pathlib import Path, PurePosixPath
import argparse
from collections import Counter
import hashlib
import json
import re
import sys
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools'))
from run_phase4_build import tree_manifest,source_manifest,preserved_players,check_scene,no_reparse,STAGE
from verify_phase4_player import validate_png

def load(path):return json.loads(path.read_text(encoding='utf-8'))
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def verify_compiler_closure(source, compiled, claimed, stage):
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
        if len(raw)>4096 or 'folderAsset: yes' not in raw.decode('utf-8',errors='strict') or hashlib.sha256(raw).hexdigest()!=files[name]:
            raise ValueError('Compiler folder meta content/hash mismatch')
        generated[name]=files[name]
    derived={'sourceCount':len(source),'compilerCount':len(files),'allDeliveredInputsExact':True,'nativeGeneratedFolderMetas':generated}
    if claimed!=derived:raise ValueError('Compiler closure summary disagrees with independently derived inputs')
    return derived

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    for name in ['label','build-label','services-label','input-label']:parser.add_argument('--'+name,required=True)
    args=parser.parse_args()
    for label in vars(args).values():
        if not re.fullmatch(r'[A-Za-z0-9_-]+',label):parser.error('Unsafe label')
    builddir=ROOT/'.artifacts/phase4-build'/args.build_label
    build=load(builddir/'results.json');source=load(builddir/'source-pins.json');stage=load(builddir/'staged-inputs.json')['files']
    if not build['passed'] or not build['built'] or build['buildReport']['result']!='Succeeded' or build['buildReport']['errors']:raise ValueError('Build not accepted')
    if source!=stage or source_manifest()!=source:raise ValueError('Source/stage/current mismatch')
    compiled=load(builddir/'compiled-inputs.json');compiler={item['path']:item['sha256'] for item in compiled['files']}
    closure=verify_compiler_closure(source,compiled,build['compiledClosure'],STAGE)
    player=tree_manifest(ROOT/'Builds/Phase4Windows',{'saves','logs'})
    if player!=build['candidateManifest']:raise ValueError('Candidate membership or bytes changed')
    older=preserved_players();protected=load(builddir/'protected-before.json')
    if older!=protected or load(builddir/'protected-after.json')!=protected:raise ValueError('Older players changed')
    cases={};pngs={};evidence={};external=0;native=0;processes=0
    directories=[builddir]
    for mode,label in [('services',args.services_label),('input',args.input_label)]:
        directory=ROOT/'.artifacts/phase4-player'/label;report=load(directory/'verification.json');directories.append(directory)
        if not report['passed'] or report['checkCount']!=len(report['checks']) or not all(c['passed'] for c in report['checks']):raise ValueError('Verifier report failed/count mismatch')
        if report['screenshotCount']!=len(report['screenshots']):raise ValueError('Screenshot count mismatch')
        if load(directory/'protected-before.json')!=protected or load(directory/'protected-after.json')!=protected:raise ValueError('Player preservation mismatch')
        for name,digest in load(directory/'runner-pins.json').items():
            if sha(ROOT/name)!=digest:raise ValueError('Runner input drift: '+name)
        external+=report['checkCount']
        for name,case in report['cases'].items():
            if not case['passed'] or case['checkCount']!=len(case['checks']) or not all(c['passed'] for c in case['checks']):raise ValueError('Native report mismatch')
            native+=case['checkCount'];processes+=1
        for name in report['screenshots']:
            path=ROOT/name
            if validate_png(path)!=(1600,900):raise ValueError('PNG dimensions')
            pngs[path.relative_to(ROOT).as_posix()]=sha(path)
        profiles=directory/'session/Profiles';no_reparse(profiles)
        # This is the explicitly owned synthetic session, never an accepted player's Saves.
        selected=report['cases']['reopen']['initial']['profileId'];profile_ids=[]
        for path in profiles.iterdir():
            no_reparse(path)
            if not path.is_dir() or not re.fullmatch(r'[a-f0-9]{32}',path.name):raise ValueError('Unexpected fixture profile entry')
            profile_ids.append(path.name)
        if profile_ids!=[selected]:raise ValueError('Restore retry created extra profile or selected wrong clone')
        observed_prefixes=0
        if mode=='input':
            for entry in load(directory/'native-acknowledgements.json'):
                state=entry['state'];binder=state['binder']
                if not binder['open'] or binder['mode']!='virtual':continue
                expected=Counter(card for pack in json.loads(state['sessionJson'])['packs'] for card in pack['orderedCardIds'][:pack['revealedCount']])
                if any(row['quantity']!=expected[row['id']] or row['owned']!=(expected[row['id']]>0) for row in binder['rows']):raise ValueError('Actual observed binder leaks hidden per-ID ownership')
                observed_prefixes+=1
            if observed_prefixes<3:raise ValueError('Missing sealed/revealing/completed native projection observations')
        cases[mode]={'report':str((directory/'verification.json').relative_to(ROOT)),'externalAssertions':report['checkCount'],'nativeAssertions':sum(c['checkCount'] for c in report['cases'].values()),'processCount':len(report['cases']),'pngCount':report['screenshotCount'],'exactlyOneImportedProfile':selected,'realObservedPerIdPrefixChecks':observed_prefixes}
    for directory in directories:
        # Only owned evidence, not TMP or synthetic session saves. No save-directory enumeration.
        for name,digest in tree_manifest(directory,{'tmp','session','saves','logs'}).items():evidence[(directory/name).relative_to(ROOT).as_posix()]=digest
    scene=check_scene();scene_path=ROOT/scene['scene'];references={}
    for guid in sorted(set(re.findall(r'guid: ([a-f0-9]{32})',scene_path.read_text(encoding='utf-8')))):
        if guid.startswith('0000000000000000'):continue
        matches=[name for name in source if name.startswith('Assets/') and name.endswith('.meta') and re.search(r'^guid: '+guid+r'$',(ROOT/name).read_text(encoding='utf-8'),re.M)]
        if len(matches)!=1:raise ValueError('Source GUID closure mismatch')
        references[guid]={'meta':matches[0],'metaSha256':source[matches[0]],'asset':matches[0][:-5]}
    result={'passed':True,'auditRunnerSha256':sha(Path(__file__)),'exe':str(ROOT/'Builds/Phase4Windows/VintageCardCollector.exe'),'buildLabel':args.build_label,'buildReport':build['buildReport'],'scene':scene,'sceneGuidReferences':references,
            'sourceFiles':source,'stagedFiles':stage,'compilerInputs':compiler,'compilerClosure':closure,'compilerAssemblies':build['compilerAssemblies'],
            'candidateFiles':player,'olderPlayerFiles':older,'evidenceFiles':evidence,'framebuffers':pngs,'cases':cases,
            'externalAssertionCount':external,'nativeAssertionCount':native,'actualPlayerProcessCount':processes,'completePngCount':len(pngs),
            'limitations':['Injected background HWND input, not human physical acceptance','Branded assets deferred; production manifest has no supplied assets','Actual generic media host binding checked including restored profile; no human hearing or supplied audio playback claim','Cross-process prepared-pointer recovery remains fail-closed; same-process retry tested, not automatic recovery after kill']}
    out=ROOT/'.artifacts/phase4-build'/args.label;no_reparse(out);out.mkdir(parents=True,exist_ok=False)
    path=out/'final-pins.json';path.write_text(json.dumps(result,indent=2))
    print(json.dumps({'passed':True,'report':str(path),'sha256':sha(path),'sourceCount':len(source),'compilerCount':len(compiler),'playerImmutableCount':len(player),'processCount':processes,'externalAssertions':external,'nativeAssertions':native,'completePngCount':len(pngs),'cases':cases},indent=2))
    return 0
if __name__=='__main__':sys.exit(main())
