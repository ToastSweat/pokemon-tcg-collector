"""Offline only: compile exact source AST, inject fake user32; never load a native DLL.
Run with --source PATH --receipt PATH --log PATH. Every recorded post is simulated.
"""
import argparse,ast,ctypes,ctypes.wintypes as W,hashlib,io,json,sys,types,unittest
from pathlib import Path
ARGS=argparse.ArgumentParser()
ARGS.add_argument('--source',type=Path,required=True)
ARGS.add_argument('--receipt',type=Path,required=True)
ARGS.add_argument('--log',type=Path,required=True)
args=ARGS.parse_args()
raw=args.source.read_bytes();source=raw.decode('utf-8');tree=ast.parse(source)
classes: list[ast.stmt]=[n for n in tree.body if isinstance(n,ast.ClassDef) and n.name=='OwnedWindowInput']
initial=next(n for n in ast.walk(tree) if isinstance(n,ast.For) and ast.unparse(n.target)=='(msg, w)')
requests=next(n for n in ast.walk(tree) if isinstance(n,ast.For) and isinstance(n.target,ast.Name) and n.target.id=='filename')
start=next(i for i,n in enumerate(requests.body) if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='xy' for t in n.targets))
request_body=requests.body[start:]
segments={'initial': [initial], 'binder-click-request.json':request_body,'binder-type-request.json':request_body,'quantity-type-1280.json':request_body,'quantity-type-1600.json':request_body}
expected={k:([0x200,0x201,0x202]+([0x102,0x100,0x101] if k=='binder-type-request.json' else [0x100,0x101,0x102] if k.startswith('quantity') else [])) for k in segments}
class Process:
 pid=73
 def __init__(self):self.dead=False;self.calls=0;self.die_on_poll: int | None=None;self.trace=[]
 def poll(self):
  self.calls+=1;self.trace.append('poll')
  if self.calls==self.die_on_poll:self.dead=True
  return 1 if self.dead else None
class FakeUser32:
 def __init__(self,p,cut=None,loss='foreign'):
  self.p=p;self.cut=cut;self.loss=loss;self.owner=p.pid;self.valid=True;self.visible=True;self.thread=99;self.posts=[];self.foreign=[];self.success=True
  if cut==0:self.lose()
 def lose(self):
  if self.loss in ('foreign','reused'):self.owner=999
  elif self.loss=='dead':self.p.dead=True
  elif self.loss=='invalid':self.valid=False
  elif self.loss=='hidden':self.visible=False
  elif self.loss=='stale':self.thread=0;self.owner=0
 def IsWindow(self,h):self.p.trace.append('valid');return self.valid
 def IsWindowVisible(self,h):self.p.trace.append('visible');return self.visible
 def GetWindowThreadProcessId(self,h,out):
  self.p.trace.append('owner');ctypes.cast(out,ctypes.POINTER(W.DWORD)).contents.value=self.owner;return self.thread
 def PostMessageW(self,h,m,w,l):
  self.p.trace.append('post')
  row=[h,m,w,l];self.posts.append(row)
  if self.p.dead or not self.valid or not self.visible or not self.thread or self.owner!=self.p.pid:self.foreign.append(row)
  if self.cut==len(self.posts):self.lose()
  return self.success

def environment(cut=None,loss='foreign'):
 p=Process();u=FakeUser32(p,cut,loss)
 ns={'ctypes':ctypes,'W':W,'time':types.SimpleNamespace(sleep=lambda _:None),'p':p,'u':u,'h':100,'xy':10,'handled':set(),'print':lambda *a,**k:None}
 if classes:
  exec(compile(ast.Module(body=classes,type_ignores=[]),str(args.source),'exec'),ns)
  ns['sender']=ns['OwnedWindowInput'](p,u)
 return p,u,ns

def execute(segment,ns):
 ns['filename']=segment;ns['req']={'command':'search:Z' if segment=='binder-type-request.json' else 'quantity:37' if segment.startswith('quantity') else 'binder-set:jungle-en-unlimited1999','x':10,'y':10}
 exec(compile(ast.Module(body=segments[segment],type_ignores=[]),str(args.source),'exec'),ns)

class Ownership(unittest.TestCase):pass
cases=[]
def add(name,fn):
 setattr(Ownership,'test_'+name,fn);cases.append(name)
for segment,messages in expected.items():
 def baseline(self,segment=segment,messages=messages):
  p,u,ns=environment();execute(segment,ns);self.assertEqual([r[1] for r in u.posts],messages);self.assertEqual(u.foreign,[])
 add('exact_ast_happy_'+segment.replace('.','_'),baseline)
 for cut in range(len(messages)):
  for loss in ['foreign','reused','dead','invalid','hidden','stale']:
   def reject(self,segment=segment,cut=cut,loss=loss):
    p,u,ns=environment(cut,loss)
    try:execute(segment,ns)
    except (PermissionError,OSError):pass
    self.assertEqual(u.foreign,[],f'{segment}: {loss} after {cut} accepted messages sent subsequent unsafe posts')
    self.assertEqual(len(u.posts),cut,'must abort remaining sequence without cleanup input')
   add('exact_ast_'+segment.replace('.','_')+'_'+loss+'_after_'+str(cut),reject)
# Guard-only arbitrary message coverage includes activation, character, mouse and key/Tab.
for message in [0x6,0x1c,0x200,0x201,0x202,0x100,0x101,0x102]:
 for loss in ['foreign','reused','dead','invalid','hidden','stale']:
  def guard(self,message=message,loss=loss):
   p,u,ns=environment(0,loss);self.assertIn('sender',ns,'per-message injectable guard is missing')
   with self.assertRaises(PermissionError):ns['sender'].message(100,message,9,1)
   self.assertEqual(u.posts,[])
   # Reject stays closed even if a caller catches and ownership later appears valid.
   u.owner=p.pid;u.valid=True;u.visible=True;u.thread=99;p.dead=False
   with self.assertRaises(PermissionError):ns['sender'].message(100,message,9,1)
   self.assertEqual(u.posts,[])
  add('guard_'+str(message)+'_'+loss,guard)
def ordering(self):
 p,u,ns=environment();self.assertIn('sender',ns)
 ns['sender'].message(100,0x102,ord('7'),1)
 self.assertEqual(p.trace,['poll','valid','visible','owner','poll','post'])
add('immediate_check_order_every_message',ordering)
def second_poll(self):
 p,u,ns=environment();self.assertIn('sender',ns);p.die_on_poll=2
 with self.assertRaises(PermissionError):ns['sender'].message(100,0x201,1,0)
 self.assertEqual(u.posts,[])
add('child_exits_after_owner_query_before_post',second_poll)
def failed_post(self):
 p,u,ns=environment();self.assertIn('sender',ns);u.success=False
 with self.assertRaises(OSError):ns['sender'].message(100,0x201,1,0)
 self.assertEqual(len(u.posts),1);u.success=True
 with self.assertRaises(PermissionError):ns['sender'].message(100,0x202,0,0)
 self.assertEqual(len(u.posts),1)
add('post_failure_latches_closed',failed_post)
def no_bypass(self):
 parents={child:node for node in ast.walk(tree) for child in ast.iter_child_nodes(node)}
 sends=[n for n in ast.walk(tree) if isinstance(n,ast.Call) and isinstance(n.func,ast.Attribute) and n.func.attr=='PostMessageW']
 self.assertEqual(len(sends),1,'all sends must share one checked boundary')
 for call in sends:
  node=call
  while node in parents and not isinstance(node,ast.ClassDef):node=parents[node]
  self.assertIsInstance(node,ast.ClassDef)
  if isinstance(node,ast.ClassDef):self.assertEqual(node.name,'OwnedWindowInput')
 forbidden={'SendInput','SetCursorPos','SetForegroundWindow','SetActiveWindow','keybd_event','mouse_event','SendMessageW'}
 self.assertFalse([n.func.attr for n in ast.walk(tree) if isinstance(n,ast.Call) and isinstance(n.func,ast.Attribute) and n.func.attr in forbidden])
add('all_native_posts_single_boundary_no_global_input',no_bypass)
if __name__=='__main__':
 log=io.StringIO();result=unittest.TextTestRunner(stream=log,verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(Ownership))
 args.log.write_text(log.getvalue(),encoding='utf-8')
 report={'passed':result.wasSuccessful(),'source':str(args.source),'sourceSha256':hashlib.sha256(raw).hexdigest(),'testCount':result.testsRun,'failures':len(result.failures),'errors':len(result.errors),'noNativeDLLLoaded':True,'noActualForeignInput':True,'exactASTSegments':{k:ast.unparse(ast.Module(body=v,type_ignores=[])) for k,v in segments.items()},'cases':cases,'failureNames':[str(t) for t,_ in result.failures],'errorNames':[str(t) for t,_ in result.errors]}
 args.receipt.write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps({k:v for k,v in report.items() if k not in ['cases','exactASTSegments','failureNames','errorNames']}));sys.exit(0 if result.wasSuccessful() else 1)
