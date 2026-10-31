"""Bounded identical-request publication tests; only owned synthetic files."""
from pathlib import Path
import importlib.util
import sys
import tempfile
import unittest
from unittest.mock import patch
ROOT=Path(__file__).resolve().parents[1]
sys.dont_write_bytecode=True
spec=importlib.util.spec_from_file_location('tilt_request',ROOT/'tools/verify_tilt_player.py')
assert spec and spec.loader
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
class RequestPublicationTests(unittest.TestCase):
    def fixture(self):
        base=ROOT/'.artifacts/tilt-request-publication-tests';base.mkdir(parents=True,exist_ok=True)
        directory=tempfile.TemporaryDirectory(dir=base);self.addCleanup(directory.cleanup)
        dest=Path(directory.name)/'request.json';work=Path(directory.name)/'request.json.writing'
        dest.write_bytes(b'old-complete-request');work.write_bytes(b'exact-next-request')
        self.assertTrue(callable(getattr(module,'publish_request',None)),'Request mailbox cannot tolerate bounded native read sharing')
        return work,dest
    @staticmethod
    def sharing():
        error=PermissionError('Synthetic Windows read-sharing denial');error.winerror=5;return error
    def test_transient_sharing_retries_exact_owned_work_then_publishes(self):
        work,dest=self.fixture();original=module.os.replace;calls=[]
        def replace(a,b):
            calls.append((Path(a).read_bytes(),Path(b).read_bytes()))
            if len(calls)<3:raise self.sharing()
            original(a,b)
        with patch.object(module.os,'replace',replace),patch.object(module.time,'sleep',lambda _:None):module.publish_request(work,dest)
        self.assertEqual(calls,[(b'exact-next-request',b'old-complete-request')]*3)
        self.assertEqual(dest.read_bytes(),b'exact-next-request');self.assertFalse(work.exists())
    def test_persistent_sharing_fails_bounded_preserving_both(self):
        work,dest=self.fixture();calls=[]
        def replace(a,b):calls.append(1);raise self.sharing()
        with patch.object(module.os,'replace',replace),patch.object(module.time,'sleep',lambda _:None):
            with self.assertRaises(PermissionError):module.publish_request(work,dest)
        self.assertEqual(len(calls),101);self.assertEqual(dest.read_bytes(),b'old-complete-request');self.assertEqual(work.read_bytes(),b'exact-next-request')
    def test_unrelated_error_is_not_retried(self):
        work,dest=self.fixture();calls=[]
        def replace(a,b):calls.append(1);raise OSError('Synthetic unrelated failure')
        with patch.object(module.os,'replace',replace),patch.object(module.time,'sleep',lambda _:None):
            with self.assertRaises(OSError):module.publish_request(work,dest)
        self.assertEqual(len(calls),1)
if __name__=='__main__':unittest.main(verbosity=2)
