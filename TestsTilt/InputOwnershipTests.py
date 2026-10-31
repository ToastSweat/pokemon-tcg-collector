"""Deterministic owned-window sender tests; fake backend, never native input."""
import importlib.util
from pathlib import Path
import sys
import unittest
ROOT=Path(__file__).resolve().parents[1]
sys.dont_write_bytecode=True
spec=importlib.util.spec_from_file_location('tilt_sender',ROOT/'tools/verify_tilt_player.py')
assert spec and spec.loader
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
class Process:
    pid=12345
    def __init__(self,dead=False):self.dead=dead;self.polls=0
    def poll(self):self.polls+=1;return 1 if self.dead else None
class Backend:
    def __init__(self,owners=None):self.owners=list(owners or [12345]*20);self.queries=[];self.posts=[]
    def owner(self,hwnd):self.queries.append(hwnd);return self.owners.pop(0)
    def post(self,hwnd,message,wparam,lparam):self.posts.append((hwnd,message,wparam,lparam));return True
class InputOwnershipTests(unittest.TestCase):
    def sender(self,process,backend):
        self.assertTrue(callable(getattr(module,'OwnedWindowInput',None)),'Input-mode messages lack an owned live-process boundary')
        return module.OwnedWindowInput(process,backend=backend,wait=lambda _:None)
    def test_every_mouse_key_character_message_checks_live_owner(self):
        p,b=Process(),Backend();s=self.sender(p,b)
        state={'controls':[{'name':'button','x':10,'y':20,'width':100,'height':40,'enabled':True}]}
        s.click(99,state,'button');s.key(99,27);s.message(99,0x102,ord('x'),1)
        self.assertEqual(len(b.posts),6);self.assertEqual(len(b.queries),6);self.assertGreaterEqual(p.polls,6)
    def test_stale_owner_rejected_without_post(self):
        p,b=Process(),Backend([999]);s=self.sender(p,b)
        with self.assertRaises(PermissionError):s.message(99,0x200,0,0)
        self.assertEqual(b.posts,[]);self.assertEqual(len(b.queries),1)
    def test_dead_process_rejected_before_window_query_or_post(self):
        p,b=Process(True),Backend();s=self.sender(p,b)
        with self.assertRaises(PermissionError):s.key(99,27)
        self.assertEqual(b.posts,[]);self.assertEqual(b.queries,[])
    def test_recycled_owner_between_click_messages_stops_remaining_input(self):
        p,b=Process(),Backend([12345,12345,999]);s=self.sender(p,b)
        with self.assertRaises(PermissionError):s.click(99,{'controls':[{'name':'button','x':10,'y':20,'width':100,'height':40,'enabled':True}]},'button')
        self.assertEqual(len(b.posts),2);self.assertEqual(len(b.queries),3)
if __name__=='__main__':unittest.main(verbosity=2)
