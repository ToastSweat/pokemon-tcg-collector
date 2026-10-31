"""Core-connected Phase 3 media/audio checks using only labelled original fixtures."""
import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import re
import struct
import subprocess
import sys
import time
import wave
from PIL import Image, ImageDraw
from verify_phase3_player import owned_window, native_click, post_click, read_json_eventually, png_size

ROOT = Path(__file__).resolve().parents[1]
EXE = ROOT/'Builds/Phase3Windows/VintageCardCollector.exe'
CARD = 'pkmn-base-en-unlimited1999-001-holo-standard'


def fixtures(folder, variant):
    face = Image.new('RGB',(630,880),(35,180,215))
    draw = ImageDraw.Draw(face)
    draw.rectangle((0,0,629,119), fill=(210,35,140))
    draw.rectangle((0,740,629,879), fill=(210,35,140))
    draw.text((24,30),'ORIGINAL SYNTHETIC TEST FACE - NOT SUPPLIED ART',fill='white')
    face.save(folder/'face.png')
    mask = Image.new('RGB',(630,880),(0,0,0))
    ImageDraw.Draw(mask).rectangle((60,160,570,480),fill='white')
    mask.save(folder/'mask.png')
    Image.new('RGB',(630,880),(210,75,30)).save(folder/'back.png')
    wrapper=Image.new('RGB',(400,800),(90,55,190))
    ImageDraw.Draw(wrapper).text((10,40),'ORIGINAL TEST WRAPPER',fill='white')
    wrapper.save(folder/'wrapper.png')
    rate=44100
    samples=[round(0.12*32767*math.sin(2*math.pi*660*i/rate)*(1-i/rate)) for i in range(rate)]
    with wave.open(str(folder/'open.wav'),'wb') as output:
        output.setnchannels(1); output.setsampwidth(2); output.setframerate(rate)
        output.writeframes(struct.pack('<'+'h'*len(samples),*samples))
    entries=[]
    for role,name,card in [('card-face','face.png',CARD),('holo-mask','mask.png',CARD),
                           ('card-back','back.png',''),('wrapper-front','wrapper.png',''),('sound-open','open.wav','')]:
        if variant=='no-mask' and role=='holo-mask':
            continue
        path=folder/name
        entries.append({'role':role,'cardId':card,'relativePath':path.relative_to(ROOT).as_posix(),
                        'source':'Original generated integration fixture, not supplied or branded',
                        'rights':'original-test-only','restrictions':'Do not represent as user materials',
                        'sha256':('0'*64 if variant=='bad-hash' and role=='card-face' else hashlib.sha256(path.read_bytes()).hexdigest())})
    manifest={'schemaVersion':1,'status':'original-synthetic-test-fixture','intendedUse':'isolated-local-verification',
              'redistributionPermission':'not-established','rightsDefault':'unknown','notes':'No real user/branded media.', 'assets':entries}
    (folder/'media-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    return len(entries)


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--label',required=True)
    args=p.parse_args()
    if not re.fullmatch('[A-Za-z0-9_-]+',args.label): p.error('Unsafe label')
    if not EXE.is_file(): p.error('Phase 3 candidate is not built')
    out=ROOT/'.artifacts/phase3-media-player'/args.label
    out.mkdir(parents=True,exist_ok=False)
    (out/'tmp').mkdir()
    env=dict(os.environ,TMP=str(out/'tmp'),TEMP=str(out/'tmp'),TMPDIR=str(out/'tmp'))
    checks=[]; cases={}; images=[]
    real_manifest=ROOT/'Assets/Supplied/AssetManifest.json'
    original=real_manifest.read_bytes()
    def check(name,passed,detail=''):
        checks.append({'name':name,'passed':bool(passed),'detail':detail})
        if not passed: raise RuntimeError(name+': '+detail)
    def launch(name,scenario,variant=None):
        folder=out/name; folder.mkdir()
        if variant: fixtures(folder,variant)
        command=[str(EXE),'-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-logFile',str(folder/'player.log'),
                 '--vcc-smoke='+scenario,'--vcc-save-root='+str(out/('save-'+name)),'--vcc-artifacts='+str(folder)]
        stream=(folder/'process.log').open('w',encoding='utf-8')
        process=subprocess.Popen(command,cwd=EXE.parent,env=env,stdout=stream,stderr=subprocess.STDOUT)
        return process,stream,folder
    def finish(process,stream,folder):
        try: code=process.wait(timeout=90)
        finally:
            if process.poll() is None: process.kill(); process.wait(timeout=10)
            stream.close()
        check(folder.name+':exit',code==0,'Actual graphical player')
        report=json.loads((folder/'report.json').read_text(encoding='utf-8'))
        check(folder.name+':core-report',report['passed'] and report['checkCount']==len(report['checks']) and report['checkCount']>0 and all(c['passed'] for c in report['checks']))
        for name in report['screenshots']:
            check(folder.name+':png:'+name,Path(name).name==name and png_size(folder/name)==(1600,900))
            images.append(str((folder/name).relative_to(ROOT)))
        media=json.loads((folder/'media-probe.json').read_text(encoding='utf-8'))
        cases[folder.name]={'coreReport':report,'media':media}
        return media
    failure=None
    try:
        process,stream,folder=launch('default-original','input')
        try:
            probe=folder/'probe.json'; mp=folder/'media-probe.json'
            state=read_json_eventually(probe,lambda s:s['stage']=='ready',35)
            media=read_json_eventually(mp,lambda m:m['bound'],15)
            check('empty-real-manifest-defaults-honestly',not media['testManifest'] and media['suppliedAssets']==0 and media['warnings']==0)
            check('no-startup-cue',media['cues']==0 and not media['sourcePlaying'])
            check('four-real-original-clips',len(media['clips'])==4 and all(c['readbackSucceeded'] and c['samples']>0 and 0<c['peak']<=0.25 and c['rms']>0 for c in media['clips']))
            check('source-is-safe-2d-one-shot',not media['playOnAwake'] and not media['loop'] and media['spatialBlend']==0)
            window=owned_window(process.pid,15)
            if state['paused']: state=native_click(window,probe,state,'resume',lambda s:not s['paused'])
            state=native_click(window,probe,state,'primary',lambda s:s['stage']=='sealed')
            media=read_json_eventually(mp,lambda m:m['cues']>=1 and m['everPlaying'],8)
            check('real-committed-generate-playback-observed',media['generatedResults']==1 and media['virtualCards']==11)
            state=native_click(window,probe,state,'primary',lambda s:s['stage']=='revealing')
            read_json_eventually(mp,lambda m:m['cues']>=2,8)
            state=native_click(window,probe,state,'primary',lambda s:s['revealedCount']==1)
            media=read_json_eventually(mp,lambda m:m['cues']>=3,8); baseline=media['cues']
            state=native_click(window,probe,state,'pause',lambda s:s['paused'])
            read_json_eventually(mp,lambda m:m['paused'] and not m['sourcePlaying'],8)
            def with_mute(s):
                s=dict(s); s['controls']=list(s['controls'])
                scale=min(s['screenWidth']/1600,s['screenHeight']/900)
                w=s['screenWidth']/scale; h=s['screenHeight']/scale
                s['controls'].append({'name':'mute','x':((w-660)/2+254)*scale,'y':((h-470)/2+222)*scale,'width':366*scale,'height':52*scale})
                return s
            post_click(window,with_mute(state),'mute')
            read_json_eventually(mp,lambda m:m['volume']==0 and not m['sourcePlaying'],8)
            state=read_json_eventually(probe,lambda s:s['lastCommand']=='volume:0',8)
            state=native_click(window,probe,state,'resume',lambda s:not s['paused'])
            state=native_click(window,probe,state,'primary',lambda s:s['revealedCount']==2)
            time.sleep(0.3)
            media=json.loads(mp.read_text())
            check('muted-committed-next-is-consumed-silently',media['cues']==baseline and media['volume']==0 and not media['sourcePlaying'])
            state=native_click(window,probe,state,'pause',lambda s:s['paused'])
            post_click(window,with_mute(state),'mute')
            read_json_eventually(mp,lambda m:m['volume']>0 and not m['sourcePlaying'],8)
            state=read_json_eventually(probe,lambda s:s['lastCommand'].startswith('volume:') and s['lastCommand']!='volume:0',8)
            state=native_click(window,probe,state,'resume',lambda s:not s['paused'])
            time.sleep(0.15)
            check('resume-unmute-do-not-replay',json.loads(mp.read_text())['cues']==baseline)
            state=native_click(window,probe,state,'primary',lambda s:s['revealedCount']==3)
            media=read_json_eventually(mp,lambda m:m['cues']==baseline+1,8)
            check('next-after-unmute-produces-one-new-cue',media['virtualCards']==11 and media['generatedResults']==1)
            check('native-dsp-output-observed',media['maxOutputPeak']>0,'Engine DSP output, not physical speaker/hearing acceptance.')
            post_click(window,state,'quit')
            finish(process,stream,folder)
        finally:
            if process.poll() is None: process.kill(); process.wait(timeout=10)
            stream.close()
        for variant in ['valid','bad-hash','no-mask']:
            process,stream,folder=launch(variant,'layout',variant)
            media=finish(process,stream,folder)
            check(variant+':explicit-test-override',media['testManifest'] and media['bound'])
            check(variant+':supplied-test-wav-decoded',any(c['action']=='open' and c['frequency']==44100 and c['samples']==44100 and c['readbackSucceeded'] for c in media['clips']))
            if variant=='valid': check('all-five-test-bindings-loaded',media['suppliedAssets']==5 and media['warnings']==0)
            elif variant=='bad-hash': check('bad-face-and-orphan-mask-fall-back',media['suppliedAssets']==3 and media['warnings']>=2)
            else: check('face-without-mask-remains-valid',media['suppliedAssets']==4 and media['warnings']==0)
        check('real-production-manifest-unchanged',real_manifest.read_bytes()==original and json.loads(original)['assets']==[])
    except Exception as error:
        failure=repr(error); checks.append({'name':'integration-failure','passed':False,'detail':failure})
    report={'passed':failure is None and all(c['passed'] for c in checks),'checkCount':len(checks),'checks':checks,
            'cases':cases,'validatedScreenshotCount':len(images),'screenshots':images,
            'fixturePolicy':'Original synthetic patterns/PCM only, not user-supplied/branded material. Explicit smoke override; production manifest unchanged.',
            'physicalHearingAccepted':False}
    (out/'verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'passed':report['passed'],'checkCount':len(checks),'validatedScreenshotCount':len(images),'failure':failure,'report':str(out/'verification.json')},indent=2))
    return 0 if report['passed'] else 1


if __name__=='__main__':
    sys.exit(main())
