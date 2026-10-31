"""Compose honest local visual proof from native GPU captures, not synthesized shader output."""
import argparse,hashlib,json,struct,zlib
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[1]
def validate(p):
    b=p.read_bytes();assert b[:8]==b'\x89PNG\r\n\x1a\n';at=8;dimensions=None;end=False
    while at<len(b):
        n=struct.unpack('>I',b[at:at+4])[0];k=b[at+4:at+8];d=b[at+8:at+8+n];assert len(d)==n
        assert zlib.crc32(k+d)&0xffffffff==struct.unpack('>I',b[at+8+n:at+12+n])[0]
        if k==b'IHDR':dimensions=struct.unpack('>II',d[:8])
        at+=12+n
        if k==b'IEND':end=True;break
    assert end and at==len(b)
    with Image.open(p) as im:im.load();assert im.size==dimensions
    return {'sha256':hashlib.sha256(b).hexdigest(),'dimensions':dimensions,'completeCRC':True}
def main():
    p=argparse.ArgumentParser(__doc__);p.add_argument('--label',default='native-final-green');a=p.parse_args();assert a.label and all(c.isalnum() or c in '-_' for c in a.label)
    source=ROOT/'.artifacts/tilt-surface-project/evidence'/a.label;out=ROOT/'.artifacts/tilt-surface-project/evidence'/('visual-proof-'+a.label);out.mkdir(parents=True,exist_ok=False)
    report=json.loads((source/'results.json').read_text());assert report['passed'];poses=['neutral','left','right','up','down'];pins={}
    groups=[('masked-finishes',[('holo','finish'),('wrapper-front','finish'),('wrapper-back','finish')]),('matte-surfaces',[('nonholo','matte'),('cardback','matte')])]
    for name,rows in groups:
        sheet=Image.new('RGB',(1500,len(rows)*336+36),(20,25,30));draw=ImageDraw.Draw(sheet);draw.text((12,10),'ACTUAL D3D11 GPU / FIXED CLOCK 7.25 / imposed poses, not a live pointer recording',fill='white')
        for row,(material,finish) in enumerate(rows):
            for col,pose in enumerate(poses):
                path=source/f'actual-{material}-{pose}-{finish}.png';pins[path.name]=validate(path)
                with Image.open(path) as image:sheet.paste(image.convert('RGB').resize((300,300)),(col*300,row*336+36))
                draw.text((col*300+10,row*336+340),f'{material}: {pose}',fill='white')
        sheet.save(out/f'{name}.png')
    frames=[]
    for pose in ['neutral','left','neutral','right','neutral','up','neutral','down','neutral']:
        frame=Image.new('RGB',(960,510),(20,25,30));draw=ImageDraw.Draw(frame)
        for col,material in enumerate(['holo','wrapper-front']):
            with Image.open(source/f'actual-{material}-{pose}-finish.png') as image:frame.paste(image.convert('RGB').resize((480,480)),(col*480,30))
        draw.text((10,8),f'Native GPU fixed-clock pose sequence: {pose} | NOT live pointer/human acceptance',fill='white');frames.append(frame)
    path=out/'fixed-clock-native-poses.gif';frames[0].save(path,save_all=True,append_images=frames[1:],loop=0,duration=650,disposal=2)
    with Image.open(path) as gif:
        count=getattr(gif,'n_frames')
        for i in range(count):gif.seek(i);gif.load()
        assert count==len(frames) and gif.size==(960,510)
    pngs={p.name:validate(p) for p in out.glob('*.png')}
    (out/'evidence.json').write_text(json.dumps({'sourceRun':a.label,'sourceCaptures':pins,'composites':pngs,'gif':{'frames':count,'dimensions':[960,510],'sha256':hashlib.sha256(path.read_bytes()).hexdigest()},'scope':'Composites/sequence of actual captured native imposed poses; not live mouse input, smooth-motion proof, or human acceptance.'},indent=2));print(json.dumps({'composites':len(pngs),'gifFrames':count,'output':str(out)}))
if __name__=='__main__':main()
