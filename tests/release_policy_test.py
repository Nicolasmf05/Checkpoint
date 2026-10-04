# Pruebas de la política de releases mediante una API GitHub simulada.
# Comprueban que los recursos y CI se validan antes de hacer pública la release.

import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import hashlib

spec = importlib.util.spec_from_file_location(
    "checkpoint_release", Path(__file__).resolve().parents[1] / "scripts/Publish-Release.py"
)
release = importlib.util.module_from_spec(spec)
spec.loader.exec_module(release)


class ReleasePolicyTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / "Directory.Build.props").write_text(
            "<Project><PropertyGroup><Version>1.2.3</Version></PropertyGroup></Project>"
        )
        self.notes = self.root / "notes.md"
        self.notes.write_text("Complete release notes")
        (self.root / "dist").mkdir()
        for name in [
            "Checkpoint-1.2.3-win-x64.zip",
            "Checkpoint-1.2.3-win-x64.msi",
            "Checkpoint-source-1.2.3.zip",
        ]:
            content = ("fixture:" + name).encode()
            (self.root / "dist" / name).write_bytes(content)
            (self.root / "dist" / (name + ".sha256")).write_text(
                hashlib.sha256(content).hexdigest() + "  " + name + "\n"
            )
        self.root_patch = patch.object(release, "ROOT", self.root)
        self.root_patch.start()
        self.addCleanup(self.root_patch.stop)
        self.expected = release.packages("1.2.3")
        self.assets = [
            {"name": n, "size": v[0], "digest": v[1], "state": "uploaded"}
            for n, v in self.expected.items()
        ]
        self.old = {
            "id": 1,
            "draft": False,
            "tag_name": "v1.2.2",
            "assets": [{"name": "preserved-old-package"}],
        }
        self.current = {
            "id": 2,
            "draft": True,
            "prerelease": False,
            "tag_name": "v1.2.3",
            "assets_url": release.BASE + "/releases/2/assets",
            "assets": self.assets,
            "html_url": "https://github.com/Nicolasmf05/Checkpoint/releases/tag/v1.2.3",
        }
        self.events = []
        self.ci = "success"
        self.tag_head = "checked-head"
        owner = self

        class FakeGitHub:
            def releases(self):
                return [owner.current, owner.old]

            def request(self, method, url, data=None, file=None):
                owner.events.append((method, url, data))
                if "/actions/runs?" in url:
                    return {
                        "workflow_runs": [
                            {
                                "head_sha": "checked-head",
                                "name": "Build and verify",
                                "conclusion": owner.ci,
                            }
                        ]
                    }
                if "/git/ref/tags/" in url:
                    return {"object": {"type": "commit", "sha": owner.tag_head}}
                if "/assets?" in url:
                    return owner.assets
                if method == "PATCH":
                    target = owner.current if url.endswith("/2") else owner.old
                    target.update(data)
                    return target
                if "/releases/tags/" in url or url.endswith("/releases/latest"):
                    return owner.current
                raise AssertionError("Unexpected API operation: " + url)

        self.api_patch = patch.object(release, "GitHub", FakeGitHub)
        self.api_patch.start()
        self.addCleanup(self.api_patch.stop)
        self.git_patch = patch.object(
            release, "git", side_effect=lambda *args: "" if args[0] == "status" else "checked-head"
        )
        self.git_patch.start()
        self.addCleanup(self.git_patch.stop)

    def test_stable_complete_release_verified_before_older_release_is_hidden(self):
        release.publish(self.notes)
        writes = [(url, data) for method, url, data in self.events if method == "PATCH"]
        self.assertEqual(writes[0][1]["prerelease"], False)
        self.assertEqual(writes[0][1]["make_latest"], "true")
        self.assertTrue(writes[1][1]["draft"])
        self.assertFalse(self.current["draft"])
        self.assertTrue(self.old["draft"])
        self.assertEqual(self.old["assets"], [{"name": "preserved-old-package"}])

    def test_tampered_remote_package_never_publishes_or_hides_current_release(self):
        self.assets[0]["digest"] = "sha256:" + "0" * 64
        with self.assertRaises(RuntimeError):
            release.publish(self.notes)
        self.assertFalse(any(method == "PATCH" for method, _, _ in self.events))
        self.assertFalse(self.old["draft"])

    def test_failed_ci_never_changes_visibility(self):
        self.ci = "failure"
        with self.assertRaises(RuntimeError):
            release.publish(self.notes)
        self.assertFalse(any(method in ("POST", "PATCH") for method, _, _ in self.events))

    def test_public_release_cannot_be_overwritten(self):
        self.current["draft"] = False
        with self.assertRaises(RuntimeError):
            release.publish(self.notes)
        self.assertFalse(any(method in ("POST", "PATCH") for method, _, _ in self.events))

    def test_draft_tag_must_match_tested_commit(self):
        self.tag_head = "different-head"
        with self.assertRaises(RuntimeError):
            release.publish(self.notes)
        self.assertFalse(any(method in ("POST", "PATCH") for method, _, _ in self.events))

    def test_tampered_checksum_stops_before_any_remote_operation(self):
        (self.root / "dist/Checkpoint-1.2.3-win-x64.zip.sha256").write_text(
            "0" * 64 + "  Checkpoint-1.2.3-win-x64.zip"
        )
        with self.assertRaises(RuntimeError):
            release.publish(self.notes)
        self.assertEqual(self.events, [])


if __name__ == "__main__":
    unittest.main()
