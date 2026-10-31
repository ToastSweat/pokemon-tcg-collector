"""Verify real-loader supplied-test face pixels stay intact outside its explicit mask."""
from pathlib import Path
import json
import math
import sys
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]


def main():
    label=sys.argv[1] if len(sys.argv)>1 else 'media-01'
    if not label.replace('-','').replace('_','').isalnum(): raise ValueError('Unsafe label')
    folder=ROOT/'.artifacts/phase3-media-player'/label
    masked=Image.open(folder/'valid/inspection-holo.png').convert('RGB')
    plain=Image.open(folder/'no-mask/inspection-holo.png').convert('RGB')
    if masked.size!=(1600,900) or plain.size!=masked.size: raise ValueError('Unexpected capture dimensions')
    # Frozen accepted 1600x900 inspection layout and exact 630x880 fixture mask.
    x,y,h=330.0,140.0,620.0
    w=h*63/88
    allowed=(x+60/630*w-2,y+160/880*h-2,x+571/630*w+2,y+481/880*h+2)
    changed_inside=changed_outside=0
    printed_band_changes=0
    for py in range(math.ceil(y)+3,math.floor(y+h)-3):
        for px in range(math.ceil(x)+3,math.floor(x+w)-3):
            if masked.getpixel((px,py))!=plain.getpixel((px,py)):
                if allowed[0]<=px<=allowed[2] and allowed[1]<=py<=allowed[3]: changed_inside+=1
                else: changed_outside+=1
                if py<y+120/880*h-2 or py>y+741/880*h+2: printed_band_changes+=1
    checks=[{'name':'actual-mask-has-visible-effect','passed':changed_inside>0,'actualChangedPixels':changed_inside},
            {'name':'no-effect-escapes-explicit-mask-region','passed':changed_outside==0,'actualEscapingPixels':changed_outside},
            {'name':'top-bottom-print-bands-byte-identical','passed':printed_band_changes==0,'actualChangedPixels':printed_band_changes}]
    result={'passed':all(c['passed'] for c in checks),'checkCount':len(checks),'checks':checks,
            'scope':'Actual core-connected loader+Resources shader, original synthetic face/mask, 2 physical pixel sampling tolerance. Not branded material acceptance.'}
    (folder/'pixel-verification.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(result,indent=2))
    return 0 if result['passed'] else 1


if __name__=='__main__': sys.exit(main())
