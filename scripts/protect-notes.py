#!/usr/bin/env python3
"""Copilot preToolUse guardrail. No tool arguments are executed or logged."""

import json
import os
from pathlib import Path
import re
import shlex
import sys


READ_TOOLS = {"view", "read", "read_file", "grep", "rg", "glob", "web_fetch", "web_search"}
FILE_TOOLS = {"create", "write", "write_file", "edit", "str_replace_editor",
              "insert", "delete", "delete_file", "rename", "move", "notebookedit",
              "replace_string_in_file", "multi_replace_string_in_file", "create_file", "apply_patch"}
PATH_KEYS = {"path", "file_path", "filePath", "file", "uri", "old_path", "new_path",
             "source", "destination", "source_path", "destination_path"}
READ_COMMANDS = {"cat", "head", "tail", "less", "more", "ls", "pwd", "stat",
                 "wc", "file", "find", "rg", "grep", "diff", "git"}
PATCH_HEADER = re.compile(r"^(?:\*\*\* (?:Add File|Update File|Delete File|Move to): |--- |\+\+\+ )(.+)$", re.M)


def path_value(value):
    if value.startswith("file://"):
        from urllib.parse import unquote, urlparse
        return unquote(urlparse(value).path)
    return value


def protected(value, cwd, root, ancestors=False):
    """Check lexical and resolved paths, including symlinks and missing targets."""
    path = Path(path_value(value)).expanduser()
    if not path.is_absolute():
        path = cwd / path
    notes = root / "notes"
    for candidate, boundary in ((Path(os.path.abspath(path)), notes),
                                (path.resolve(), notes.resolve())):
        if candidate == boundary or boundary in candidate.parents:
            return True
        if ancestors and candidate in boundary.parents:
            return True
    return False


def paths(arguments):
    if isinstance(arguments, dict):
        for key, value in arguments.items():
            if key in PATH_KEYS and isinstance(value, str):
                yield value
            elif isinstance(value, (dict, list)):
                yield from paths(value)
    elif isinstance(arguments, list):
        for value in arguments:
            yield from paths(value)


def patch_paths(arguments):
    if isinstance(arguments, str):
        for match in PATCH_HEADER.finditer(arguments):
            value = match.group(1).split("\t", 1)[0].strip()
            if value != "/dev/null":
                # Unified diffs conventionally prefix paths with a/ and b/.
                if match.group(0).startswith(("--- ", "+++ ")) and value.startswith(("a/", "b/")):
                    value = value[2:]
                yield value
    elif isinstance(arguments, dict):
        for key in ("patch", "input", "patch_text", "patchText"):
            yield from patch_paths(arguments.get(key))


def shell_changes_notes(command, cwd, root):
    """Conservative check of explicit paths; this is not a shell security boundary."""
    lexer = shlex.shlex(command, posix=True, punctuation_chars=";&|<>()\n")
    lexer.whitespace = " \t\r"
    lexer.whitespace_split = True
    segments = [[]]
    for token in lexer:
        if token in {";", "&&", "||", "|", "&", "(", ")", "\n"}:
            segments.append([])
        else:
            segments[-1].append(token)
    for tokens in segments:
        if not tokens:
            continue
        executable = Path(tokens[0]).name
        if executable == "cd" and len(tokens) == 2:
            cwd = (cwd / tokens[1]).resolve()
            continue
        # git and find can mutate files; only their read-only forms are exempt.
        read_only = executable in READ_COMMANDS
        if executable == "git":
            read_only = len(tokens) > 1 and tokens[1] in {"status", "diff", "log", "show", "ls-files"}
        if executable == "find":
            read_only = not any(t in {"-delete", "-exec", "-execdir", "-ok", "-okdir", "-fprint", "-fprintf"} for t in tokens)
        output = any(">" in token or token.startswith("--output") for token in tokens)
        if read_only and not output:
            continue
        # Inspect all explicit operands conservatively, including rename sources
        # and ancestor directories. Scripts/variables can hide paths: see docs.
        for token in tokens[1:]:
            if token.startswith("--output="):
                token = token.split("=", 1)[1]
            if token and not token.startswith("-") and token not in {">", ">>", "<", "<<"}:
                if protected(token, cwd, root, ancestors=True):
                    return True
    return False


def decide(event, root):
    if not isinstance(event, dict):
        raise ValueError("expected a JSON object")
    tool = event.get("toolName", event.get("tool_name"))
    arguments = event.get("toolArgs", event.get("tool_input"))
    if not isinstance(tool, str) or arguments is None:
        raise ValueError("expected toolName/toolArgs or tool_name/tool_input")
    if isinstance(arguments, str):
        try:
            arguments = json.loads(arguments)
        except ValueError:
            if tool.lower() != "apply_patch":
                raise ValueError("expected JSON tool arguments")
    cwd = Path(event.get("cwd", str(root)))
    if not cwd.is_absolute():
        raise ValueError("expected an absolute cwd")
    tool = tool.lower()
    if tool in READ_TOOLS or (tool == "str_replace_editor" and isinstance(arguments, dict)
                              and arguments.get("command") == "view"):
        return {}
    targets = list(paths(arguments)) + list(patch_paths(arguments))
    blocked = any(protected(value, cwd, root, ancestors=True) for value in targets)
    if tool in FILE_TOOLS and not targets:
        raise ValueError("unrecognized file mutation arguments")
    if tool in {"bash", "powershell", "run_in_terminal", "shell"}:
        if not isinstance(arguments, dict) or not isinstance(arguments.get("command"), str):
            raise ValueError("expected a shell command")
        blocked |= shell_changes_notes(arguments["command"], cwd, root)
    if blocked:
        return {"permissionDecision": "deny", "permissionDecisionReason":
                "notes/ contains human-written files. Reading is allowed; changes must be made manually."}
    # Do not auto-approve anything: retain the normal permission flow.
    return {}


def main():
    try:
        result = decide(json.load(sys.stdin), Path(__file__).resolve().parent.parent)
    except (ValueError, TypeError, OSError, RuntimeError) as error:
        result = {"permissionDecision": "deny", "permissionDecisionReason":
                  "Cannot validate notes protection: " + str(error)}
    print(json.dumps(result))


if __name__ == "__main__":
    main()
