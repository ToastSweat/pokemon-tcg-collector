"""Phase5 extension of the immutable proven per-message owned-process input fence.
All actual posts go through imported OwnedWindowInput.message: live -> current HWND PID -> live -> checked post.
Window existence and visibility are additionally checked for EVERY mouse/key/character/activation message.
"""
import ctypes,time
from ctypes import wintypes
from verify_tilt_player import OwnedWindowInput as _OwnedWindowInput,_NativeWindowBackend
class _VisibleWindowBackend(_NativeWindowBackend):
 def __init__(self):
  super().__init__()
  for name in ('IsWindow','IsWindowVisible'):
   f=getattr(self.api,name);f.argtypes=[wintypes.HWND];f.restype=wintypes.BOOL
 def valid(self,hwnd):return bool(self.api.IsWindow(hwnd))
 def visible(self,hwnd):return bool(self.api.IsWindowVisible(hwnd))
class OwnedWindowInput(_OwnedWindowInput):
 def __init__(self,process,backend=None,wait=time.sleep):
  super().__init__(process,backend if backend is not None else _VisibleWindowBackend(),wait)
 def message(self,hwnd,message,wparam,lparam):
  if self.process.poll() is not None:raise PermissionError('Owned Phase5 process exited')
  if not self.backend.valid(hwnd):raise PermissionError('Owned Phase5 HWND no longer exists')
  if not self.backend.visible(hwnd):raise PermissionError('Owned Phase5 HWND is not visible')
  # Inherited key()/click() route EVERY individual message back through this method.
  return super().message(hwnd,message,wparam,lparam)
