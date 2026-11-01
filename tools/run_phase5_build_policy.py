"""Phase5 read-only path, staging and exact compiler admission. No Unity invocation on import."""
from pathlib import Path
import hashlib,json,os,stat,sys
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[1]
APPROVED=Path('F:/[WORKSPACE]/[HERMES]/[CHATGPT]/PokemonThrowbackTCG')
STAGE=ROOT/'.artifacts/phase5-build-project'
CANDIDATE=ROOT/'Builds/Phase5Windows'
COMPILER_ROOTS=('Assets','Packages','ProjectSettings')
CODE_SUFFIXES={'.cs','.asmdef','.asmref','.dll','.rsp'}
MARKER='.phase5-owned-stage.json'
def no_reparse(path):
 path=Path(path)
 for part in (*reversed(path.parents),path):
  try:a=part.lstat()
  except FileNotFoundError:continue
  if stat.S_ISLNK(a.st_mode) or getattr(a,'st_file_attributes',0)&0x400:raise ValueError('Reparse path refused: '+str(part))
 return path

def absolute(path):
 p=Path(path)
 if not p.is_absolute():raise ValueError('Absolute path required')
 no_reparse(p)
 return Path(os.path.abspath(p))

def require_stage(current,root=ROOT):
 root=absolute(root);current=absolute(current)
 if current!=root/'.artifacts/phase5-build-project':raise ValueError('Exact owned Phase5 stage required, never Root or cousin')
 return current

def sha(path):
 no_reparse(path)
 with Path(path).open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()

def tree_manifest(directory,prunes=()):
 root=absolute(directory);result={};seen=set();prunes={s.casefold() for s in prunes}
 def walk(folder):
  for p in sorted(folder.iterdir()):
   # Preservation prunes BEFORE stat/reparse/content; compiler callers never prune.
   if p.name.casefold() in prunes:continue
   no_reparse(p);key=p.relative_to(root).as_posix()
   if key.casefold() in seen:raise ValueError('Case-colliding path: '+key)
   seen.add(key.casefold())
   if p.is_dir():walk(p)
   elif p.is_file():result[key]=sha(p)
   else:raise ValueError('Non-regular input: '+key)
 if root.exists():
  if not root.is_dir():raise ValueError('Directory required')
  walk(root)
 return result

def compiler_manifest(root=ROOT):
 return {folder+'/'+name:digest for folder in COMPILER_ROOTS for name,digest in tree_manifest(Path(root)/folder).items()}

def player_manifest(player):return tree_manifest(player,{'saves','logs'})

def require_regular_tree(directory):
 # Stat-only write-boundary scan. Reject links before enumerating their targets;
 # never hash Unity caches, and never apply old-player preservation prunes here.
 root=absolute(directory)
 try:attrs=root.lstat()
 except FileNotFoundError:return root
 if not stat.S_ISDIR(attrs.st_mode):raise ValueError('Non-regular write tree: '+str(root))
 pending=[root]
 while pending:
  for child in pending.pop().iterdir():
   attrs=child.lstat()
   if stat.S_ISLNK(attrs.st_mode) or getattr(attrs,'st_file_attributes',0)&0x400:raise ValueError('Reparse path refused: '+str(child))
   if stat.S_ISDIR(attrs.st_mode):pending.append(child)
   elif not stat.S_ISREG(attrs.st_mode):raise ValueError('Non-regular write input: '+str(child))
 return root

def candidate_admission(root=ROOT):
 candidate=absolute(Path(root)/'Builds/Phase5Windows')
 if candidate.exists():
  if not candidate.is_dir():raise ValueError('Candidate is a file')
  for child in candidate.iterdir():
   no_reparse(child)
   if child.name.casefold()=='saves' and (not child.is_dir() or next(child.iterdir(),None) is not None):raise ValueError('Populated candidate Saves blocks mutation')
  require_regular_tree(candidate)
  members=[p for p in candidate.iterdir() if p.name.casefold()!='saves']
  if members:
   owner=candidate/'.phase5-candidate-owner.txt';no_reparse(owner)
   if not owner.is_file() or owner.read_text(encoding='utf-8')!='phase5-candidate-v1\n'+absolute(root).as_posix()+'\n':raise ValueError('Foreign candidate output refused')
 return candidate

def stage_preflight(root,pins,restage=False):
 root=absolute(root);stage=require_stage(root/'.artifacts/phase5-build-project',root)
 candidate_admission(root)
 require_regular_tree(stage)
 for name in (MARKER,'.phase5-stage-owner.txt','.phase5-authorization.txt','.phase5-root-pins.tsv','.phase5-stage-pins.tsv','.phase5-bindings.json','.phase5-native-meta-allowlist.txt','.phase5-native-reports'):
  no_reparse(stage/name)
 if compiler_manifest(root)!=pins:raise ValueError('Current Root does not match frozen source pins')
 expected={'schemaVersion':1,'owner':'phase5-exact-source','source':str(root)}
 if stage.exists():
  # Inspect all roots before any mutation, including markers, and before rmtree.
  actual=compiler_manifest(stage)
  bad=[n for n,d in actual.items() if Path(n).suffix.casefold() in CODE_SUFFIXES and pins.get(n)!=d]
  if bad:raise ValueError('Unknown or stale compiler-capable stage inputs: '+repr(bad))
  marker=stage/MARKER;no_reparse(marker)
  if not marker.is_file() or json.loads(marker.read_text(encoding='utf-8'))!=expected:raise ValueError('Foreign/unowned stage')
  if not restage:raise ValueError('Existing stage requires explicit restage')
 return stage,expected

def stage_exact(root,pins,restage=False):
 import shutil
 stage,owner=stage_preflight(root,pins,restage)
 # All source files were visited above without Saves/Logs exclusions.
 stage.mkdir(parents=True,exist_ok=True)
 (stage/MARKER).write_text(json.dumps(owner),encoding='utf-8')
 for folder in COMPILER_ROOTS:
  target=stage/folder
  if target.exists():shutil.rmtree(target)
 for name in pins:
  target=stage/name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(Path(root)/name,target)
 if compiler_manifest(stage)!=pins or compiler_manifest(root)!=pins:raise ValueError('Exact stage/current Root drift')
 return stage

# Exactly the four folder metas observed as omitted by older source closures; no code exception.
FOLDER_META_EXCEPTIONS={'Assets/Supplied/BaseSetMaterials.meta','Assets/Supplied/BaseSetMaterials/generations.meta','Assets/Supplied/BaseSetMaterials/generations/ac0a72cb65e509f252c73d7692d84c691f9b71718127a5ecf45563db4ab75bbb.meta','Assets/VintageCollector/Collections.meta'}
def meta_guid(path,folder=False):
 import re
 no_reparse(path)
 if Path(path).stat().st_size>65536:raise ValueError('Native meta byte bound')
 text=Path(path).read_text(encoding='utf-8');guids=re.findall(r'^guid: ([0-9a-f]{32})$',text,re.M)
 if len(guids)!=1 or not re.search(r'^fileFormatVersion: 2$',text,re.M) or (folder and ('folderAsset: yes' not in text or 'DefaultImporter:' not in text)):raise ValueError('Invalid native meta: '+str(path))
 return guids[0]

def verify_closure(source,declared,stage):
 actual=compiler_manifest(stage)
 if actual!=declared:raise ValueError('Independent full compiler closure differs from declared bytes/membership')
 if any(actual.get(n)!=d for n,d in source.items()):raise ValueError('Source bytes missing/changed in compiler closure')
 extras=set(actual)-set(source)
 if not extras<=FOLDER_META_EXCEPTIONS:raise ValueError('Undeclared compiler closure extras: '+repr(sorted(extras)))
 for name in extras:
  folder=Path(stage)/name[:-5];no_reparse(folder)
  if not folder.is_dir() or (Path(stage)/name).stat().st_size>4096:raise ValueError('Folder meta is not a bounded actual native folder')
  meta_guid(Path(stage)/name,folder=True)
 return actual

PREPARE_FOLDER_METAS=FOLDER_META_EXCEPTIONS|{'Assets/VintageCollector/Data/Phase5.meta','Assets/VintageCollector/Audio/Profiles.meta','Assets/VintageCollector/Audio/Profiles/Phase5.meta','Assets/VintageCollector/Media/Phase5.meta'}
def verify_prepare_closure(before,after,stage,action,owned_new):
 if compiler_manifest(stage)!=after:raise ValueError('Native preparation full compiler membership changed')
 changed={n for n,d in before.items() if after.get(n)!=d}
 config={'ProjectSettings/ProjectSettings.asset','ProjectSettings/EditorBuildSettings.asset'} if action=='CreateBootstrap' else set()
 if not changed<=config:raise ValueError('Native authoring changed borrowed/source inputs: '+repr(sorted(changed-config)))
 if set(before)-set(after):raise ValueError('Native authoring deleted source/compiler input')
 allowed={n+'.meta' for n in owned_new}|PREPARE_FOLDER_METAS
 if action=='CreateAssets':allowed|={f'Assets/VintageCollector/Audio/Profiles/Phase5/{s}RevealAudioProfile.asset'+suffix for s in ('Jungle','Fossil') for suffix in ('','.meta')}
 if action=='CreateBootstrap':allowed|={'Assets/VintageCollector/Scenes/BootstrapPhase5.unity','Assets/VintageCollector/Scenes/BootstrapPhase5.unity.meta'}|config
 extras=set(after)-set(before)
 if not extras<=allowed:raise ValueError('Undeclared native-authoring output: '+repr(sorted(extras-allowed)))
 for n in extras:
  if n.endswith('.meta'):meta_guid(Path(stage)/n,folder=n in PREPARE_FOLDER_METAS)
 return {'changed':{n:after[n] for n in changed},'created':{n:after[n] for n in extras}}

def native_meta_plan(root,stage,pins,owned_new):
 root=absolute(root);require_stage(stage,root)
 if compiler_manifest(root)!=pins:raise ValueError('Source drift before native adoption')
 result={};guids={}
 for name in compiler_manifest(stage):
  if name.endswith('.meta'):
   guid=meta_guid(Path(stage)/name)
   if guid in guids:raise ValueError('Native GUID collision')
   guids[guid]=name
 for name in owned_new:
  if name not in pins or not name.startswith('Assets/') or name.endswith('.meta'):raise ValueError('Only explicitly owned new source files may adopt')
  meta=name+'.meta';target=root/meta
  if target.exists() or meta in pins:raise ValueError('Existing source meta cannot be adopted/overwritten')
  if sha(Path(stage)/name)!=pins[name]:raise ValueError('Native source-to-stage mismatch')
  meta_guid(Path(stage)/meta)
  result[meta]={'sha256':sha(Path(stage)/meta),'sourceSha256':pins[name],'stagePath':str(Path(stage)/meta)}
 return result

def preserved_players(root=ROOT):
 result={};builds=absolute(Path(root)/'Builds')
 if builds.exists():
  for p in sorted(builds.iterdir()):
   if p.name.casefold() in {'phase5windows','saves','logs'}:continue
   no_reparse(p)
   if p.is_dir():result[p.name]=player_manifest(p)
 return result

def originals_manifest(root=ROOT):
 return {name:tree_manifest(Path(root)/name) for name in ('Pokemon-Base-Set','Pokemon Cries')}

def shared_base_pins(root=ROOT):
 names=('Assets/Supplied/AssetManifest.json','Assets/Supplied/BaseSetMaterials/Manifest.json','Assets/VintageCollector/Audio/DefaultRevealAudioProfile.asset')
 return {n:sha(Path(root)/n) for n in names}

CANONICAL_DATA={
 'jungle-en-unlimited1999.catalog.json':'60fc385e944778008e284c96d0038411238c0b44607b370537c825b9c88a0355',
 'jungle-en-unlimited1999.rules.json':'0d58b66ee91099bf6ad47845d62751d523404bd0dc866a62f1897041972bff12',
 'fossil-en-unlimited1999.catalog.json':'e724f63927dd66a819c02ab4dd14408ccd0affd2bee6d5a8caa7908ed5f69860',
 'fossil-en-unlimited1999.rules.json':'eff5082e6f11efbe638d62cfbd314bda1e2fb8d54b5c7534d3a77956cd10353e'}
def require_assembled_sources(root,action):
 root=absolute(root);prefix='Assets/VintageCollector/'
 required=('Domain/ApprovedSetRegistry.cs','Domain/ApprovedSetPolicy.cs','Runtime/OpeningGame.cs','Runtime/Phase5CollectionHost.cs','Runtime/Phase5MediaHost.cs','Runtime/Phase5InputPolicy.cs','Runtime/Phase5SelectionPolicy.cs','Runtime/Phase5SetConfig.cs','Runtime/Phase5SmokeProbe.cs','Presentation/SetSelectorPresentation.cs','Presentation/WrapperVariant.cs','Media/Phase5PresentationMedia.cs','Editor/Phase5Build.cs','Editor/Phase5BuildPolicy.cs','Editor/Phase5NativeChecks.cs')
 missing=[prefix+n for n in required if not (root/(prefix+n)).is_file()]
 if missing:raise ValueError('BLOCKED assembled Phase5 Root source not promoted: '+repr(missing))
 for name,digest in CANONICAL_DATA.items():
  for relative in ('Authoring/Phase5/ApprovedData/'+name,prefix+'Data/Phase5/'+name):
   if not (root/relative).is_file() or sha(root/relative)!=digest:raise ValueError('BLOCKED exact approved source/data copy missing: '+relative)
 if action=='BuildWindows' and not (root/(prefix+'Scenes/BootstrapPhase5.unity')).is_file():raise ValueError('BLOCKED parent-promoted native scene missing')
 return True

def validate_authorization(receipt,root,action):
 root=absolute(root)
 required={'schemaVersion','owner','root','promoted','action','sourcePins','bindings','toolPins','protectedPlayers','sharedBasePins','nativeMetaAssets','stagePins'}
 if type(receipt)!=dict or set(receipt)!=required or type(receipt.get('schemaVersion')) is not int or receipt['schemaVersion']!=1 or receipt['owner']!='phase5-parent-authorized' or receipt['promoted'] is not True or receipt['action']!=action or receipt['root']!=root.as_posix():raise ValueError('BLOCKED explicit parent promotion/go-ahead and frozen receipt required')
 if action not in {'CreateAssets','CreateBootstrap','BuildWindows'}:raise ValueError('Unknown native action')
 if compiler_manifest(root)!=receipt['sourcePins']:raise ValueError('Current Root differs from parent frozen source pins')
 if not receipt['toolPins'] or any(sha(root/n)!=d for n,d in receipt['toolPins'].items()):raise ValueError('Frozen tooling mismatch')
 if preserved_players(root)!=receipt['protectedPlayers'] or shared_base_pins(root)!=receipt['sharedBasePins']:raise ValueError('Preserved player/Base baseline differs')
 if type(receipt['nativeMetaAssets']) is not list or len(set(receipt['nativeMetaAssets']))!=len(receipt['nativeMetaAssets']):raise ValueError('Explicit unique owned new native metadata assets required')
 for n in receipt['nativeMetaAssets']:
  if not n.startswith('Assets/VintageCollector/') or n not in receipt['sourcePins'] or n+'.meta' in receipt['sourcePins']:raise ValueError('Only genuinely new owned source assets may receive native metas')
 if action=='BuildWindows' and receipt['nativeMetaAssets']:raise ValueError('Build cannot bootstrap missing native metas')
 missing={n for n in receipt['sourcePins'] if n.startswith('Assets/') and not n.endswith('.meta') and n+'.meta' not in receipt['sourcePins']}
 if missing!=set(receipt['nativeMetaAssets']):raise ValueError('Every missing source asset meta must be explicitly new/owned; no legacy metadata bootstrap')
 candidate_admission(root)
 return receipt
