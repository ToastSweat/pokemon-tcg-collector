"""Local pixel diagnostic only, uses the exact supplied mask and captured native images."""
import json,math
from pathlib import Path
from PIL import Image
from typing import Any,cast
ROOT=Path(__file__).resolve().parents[1]
a=json.loads((ROOT/'Assets/Supplied/BaseSetMaterials/Manifest.json').read_text())['assets']
item=next(x for x in a if x['role']=='wrapper-front-mask');m=Image.open(ROOT/item['relativePath']).convert('RGBA')
f=next(x for x in a if x['role']=='wrapper-front');face=Image.open(ROOT/f['relativePath']);w,h=face.size
out=ROOT/'.artifacts/tilt-surface-project/evidence/reflection-green';im=Image.open(out/'actual-wrapper-front-up-finish.png');ref=Image.open(out/'actual-wrapper-front-up-matte.png')
sp=math.sin(math.radians(12));cp=math.cos(math.radians(12));sw=w/max(w,h);sh=h/max(w,h)
bad=[]
for y in range(0,720,2):
 for x in range(0,720,2):
  d=max(abs(v-u) for u,v in zip(cast(Any,ref.getpixel((x,y)))[:3],cast(Any,im.getpixel((x,y)))[:3]))
  if d<=2:continue
  rx=(x+.5-360)/520;ry=(360-y-.5)/520;rz=-2.4;den=-sp*ry+cp*rz;t=-cp*2.4/den;px=rx*t;py=ry*t;pz=2.4+rz*t
  u=px/sw+.5;v=(cp*py+sp*pz)/sh+.5;hit=0<=u<=1 and 0<=v<=1
  mx=u*m.width-.5;my=(1-v)*m.height-.5;coverage=0
  for oy in range(-2,3):
   for ox in range(-2,3):
    ix=max(0,min(m.width-1,round(mx+ox)));iy=max(0,min(m.height-1,round(my+oy)));p=cast(Any,m.getpixel((ix,iy)));coverage=max(coverage,p[0]*p[3]/65025)
  if not hit or coverage==0:bad.append({'x':x,'y':y,'d':d,'u':u,'v':v,'hit':hit,'coverage':coverage})
print(json.dumps({'dimensions':face.size,'badCount':len(bad),'examples':bad[:20]},indent=2))
