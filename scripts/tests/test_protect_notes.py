"""Disposable fixtures for the single Copilot protection script; Python 3.8+."""

import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / "protect-notes.py"
SPEC = importlib.util.spec_from_file_location("protect_notes", SCRIPT)
HOOK = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(HOOK)


class ProtectNotesTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name).resolve()
        (self.root / "notes" / "nested").mkdir(parents=True)
        (self.root / "src").mkdir()
        (self.root / "notes" / "chapter.md").write_text("human text")

    def decision(self, tool, arguments, cwd=None, snake=False):
        event = {"cwd": str(cwd or self.root)}
        event.update({"tool_name": tool, "tool_input": arguments} if snake else
                     {"toolName": tool, "toolArgs": arguments})
        return HOOK.decide(event, self.root)

    def assertDenied(self, result):
        self.assertEqual("deny", result.get("permissionDecision"))
        self.assertIn("human-written", result["permissionDecisionReason"])

    def test_file_mutations_deny_notes_and_descendants(self):
        for tool in ("create", "edit", "delete_file", "Write", "NotebookEdit"):
            for path in ("notes", "notes/chapter.md", "notes/nested/new.md"):
                with self.subTest(tool=tool, path=path):
                    self.assertDenied(self.decision(tool, {"path": path}))

    def test_absolute_traversal_and_nested_cwd_paths_are_denied(self):
        for path in (str(self.root / "notes/chapter.md"), "../notes/chapter.md"):
            with self.subTest(path=path):
                self.assertDenied(self.decision("edit", {"file_path": path}, self.root / "src"))
        self.assertDenied(self.decision("edit", {"path": "new.md"}, self.root / "notes/nested"))

    def test_symlinks_into_and_out_of_notes_remain_protected(self):
        (self.root / "alias").symlink_to(self.root / "notes", target_is_directory=True)
        (self.root / "notes" / "outside").symlink_to(self.root / "src", target_is_directory=True)
        for path in ("alias/new.md", "notes/outside/new.cs"):
            with self.subTest(path=path):
                self.assertDenied(self.decision("edit", {"path": path}))

    def test_file_uri_and_nested_edit_arrays_are_denied(self):
        self.assertDenied(self.decision("multi_replace_string_in_file", {
            "replacements": [{"filePath": (self.root / "notes/chapter.md").as_uri()}]}))

    def test_rename_sources_destinations_and_ancestors_are_denied(self):
        for source, destination in (("notes/chapter.md", "src/copy.md"),
                                    ("src/code.cs", "notes/new.md"), (".", "backup")):
            with self.subTest(source=source, destination=destination):
                self.assertDenied(self.decision("rename", {"source": source, "destination": destination}))

    def test_patch_add_update_delete_move_and_unified_headers_are_denied(self):
        for header in ("*** Add File: notes/new.md", "*** Update File: notes/chapter.md",
                       "*** Delete File: notes/chapter.md", "*** Move to: notes/moved.md",
                       "--- a/notes/chapter.md", "+++ b/notes/new.md"):
            with self.subTest(header=header):
                self.assertDenied(self.decision("apply_patch", {"input": header + "\n"}))
        self.assertDenied(self.decision("apply_patch", "*** Delete File: notes/chapter.md\n"))

    def test_reads_and_edits_elsewhere_preserve_default_permissions(self):
        for tool in ("view", "Read", "grep", "glob"):
            with self.subTest(tool=tool):
                self.assertEqual({}, self.decision(tool, {"path": "notes/chapter.md"}))
        self.assertEqual({}, self.decision("str_replace_editor", {"command": "view", "path": "notes/chapter.md"}))
        self.assertEqual({}, self.decision("edit", {"path": "src/code.cs"}))
        self.assertEqual({}, self.decision("edit", {"path": "notes-backup/new.md"}))
        self.assertEqual({}, self.decision("apply_patch", {"input": "*** Add File: src/code.cs\n+x\n"}))

    def test_shell_explicit_mutations_and_redirections_are_denied(self):
        commands = ("rm -rf notes", "mv notes/chapter.md src/copy.md",
                    "mv src/code.cs notes/new.md", "printf text >notes/new.md",
                    "cat src/code.cs >> notes/new.md", "tee 'notes/nested/new file.md'",
                    "sed -i '' s/a/b/ notes/chapter.md", "git checkout -- notes",
                    "find notes -delete", "git diff --output=notes/diff.txt", "rm -rf .", "cd notes && touch new.md",
                    "pwd\nrm notes/chapter.md", "cat notes/chapter.md; rm notes/chapter.md")
        for command in commands:
            with self.subTest(command=command):
                self.assertDenied(self.decision("bash", {"command": command}))

    def test_shell_reads_and_ordinary_builds_are_allowed(self):
        for command in ("cat notes/chapter.md", "rg compiler notes", "git diff -- notes",
                        "find notes -type f", "dotnet build -m:1 --disable-build-servers",
                        "printf text > src/output.txt", "git status --short"):
            with self.subTest(command=command):
                self.assertEqual({}, self.decision("bash", {"command": command}))

    def test_camel_snake_and_json_encoded_arguments_are_supported(self):
        for snake in (False, True):
            with self.subTest(snake=snake):
                self.assertDenied(self.decision("Edit", json.dumps({"file_path": "notes/new.md"}), snake=snake))

    def test_cli_malformed_input_fails_closed_without_traceback(self):
        for payload in ("not-json", "[]", '{}', '{"toolName":"edit","toolArgs":{}}',
                        '{"toolName":"bash","toolArgs":{"command":"unterminated \\\""}}'):
            with self.subTest(payload=payload):
                result = subprocess.run([sys.executable, str(SCRIPT)], input=payload,
                                        text=True, capture_output=True, check=True)
                self.assertEqual("deny", json.loads(result.stdout)["permissionDecision"])
                self.assertEqual("", result.stderr)

    def test_cli_known_edit_is_denied(self):
        result = subprocess.run([sys.executable, str(SCRIPT)],
                                input=json.dumps({"toolName": "edit", "toolArgs": {"path": "notes/new.md"}}),
                                text=True, capture_output=True, check=True)
        self.assertDenied(json.loads(result.stdout))
        self.assertEqual("", result.stderr)

    def test_cli_allows_read_without_overriding_normal_permissions(self):
        result = subprocess.run([sys.executable, str(SCRIPT)],
                                input=json.dumps({"toolName": "view", "toolArgs": {"path": "notes"}}),
                                text=True, capture_output=True, check=True)
        self.assertEqual({}, json.loads(result.stdout))


if __name__ == "__main__":
    unittest.main()
