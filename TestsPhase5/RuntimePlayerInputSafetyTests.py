"""Offline window-backend negatives only; never sends a real Win32 message."""
from pathlib import Path
import sys,unittest
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools'))
class Process:
 pid=1234
 def __init__(self):self.dead=False;self.polls=0;self.die_at=None
 def poll(self):
  self.polls+=1
  if self.die_at==self.polls:self.dead=True
  return 1 if self.dead else None
class Backend:
 def __init__(self,p):self.p=p;self.iswindow=True;self.shown=True;self.pid=p.pid;self.posts=[];self.after=None;self.post_ok=True;self.log=[]
 def valid(self,h):self.log.append('IsWindow');return self.iswindow
 def visible(self,h):self.log.append('IsWindowVisible');return self.shown
 def owner(self,h):self.log.append('PID');return self.pid
 def post(self,*args):
  self.posts.append(args)
  if self.after:self.after()
  return self.post_ok
class InputSafety(unittest.TestCase):
 def setUp(self):
  path=ROOT/'tools/phase5_owned_window_input.py';self.assertTrue(path.is_file(),'Phase5 IsWindow/visibility/PID/live per-message guard required')
  import phase5_owned_window_input as m
  self.p=Process();self.b=Backend(self.p);self.n=m.OwnedWindowInput(self.p,self.b,wait=lambda _:None)
 def test_positive_each_message_window_visibility_pid(self):
  self.n.key(5,13);self.assertEqual(['IsWindow','IsWindowVisible','PID']*2,self.b.log);self.assertEqual(2,len(self.b.posts))
 def test_dead_child_zero_messages(self):
  self.p.dead=True
  with self.assertRaises(PermissionError):self.n.message(5,1,0,0)
  self.assertEqual([],self.b.posts)
 def test_invalid_window_zero_messages(self):
  self.b.iswindow=False
  with self.assertRaises(PermissionError):self.n.message(5,1,0,0)
  self.assertEqual([],self.b.posts)
 def test_invisible_window_zero_messages(self):
  self.b.shown=False
  with self.assertRaises(PermissionError):self.n.message(5,1,0,0)
  self.assertEqual([],self.b.posts)
 def test_reused_window_zero_foreign_messages(self):
  self.b.pid+=1
  with self.assertRaises(PermissionError):self.n.message(5,1,0,0)
  self.assertEqual([],self.b.posts)
 def test_death_during_click_no_down_or_up(self):
  self.b.after=lambda:setattr(self.p,'dead',True)
  with self.assertRaises(PermissionError):self.n.click(5,{'controls':[{'name':'a','enabled':True,'x':1,'y':1,'width':10,'height':10}]},'a')
  self.assertEqual([0x200],[p[1] for p in self.b.posts])
 def test_death_during_typing_no_keyup(self):
  self.b.after=lambda:setattr(self.p,'dead',True)
  with self.assertRaises(PermissionError):self.n.key(5,65)
  self.assertEqual([0x100],[p[1] for p in self.b.posts])
 def test_window_reused_mid_click_no_foreign_down(self):
  self.b.after=lambda:setattr(self.b,'pid',99999)
  with self.assertRaises(PermissionError):self.n.click(5,{'controls':[{'name':'a','enabled':True,'x':1,'y':1,'width':10,'height':10}]},'a')
  self.assertEqual([0x200],[p[1] for p in self.b.posts])
 def test_death_after_pid_before_post(self):
  self.p.die_at=3
  with self.assertRaises(PermissionError):self.n.message(5,1,0,0)
  self.assertEqual([],self.b.posts)
 def test_post_failure_checked(self):
  self.b.post_ok=False
  with self.assertRaises(OSError):self.n.message(5,1,0,0)
if __name__=='__main__':unittest.main(verbosity=2)
