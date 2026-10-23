# 0.1.0 release checklist

`v0.1.0` is the stable chapter 1 release ([versioning](development.md#versioning)).
`[x]` means the repository already covers the item; `[ ]` is still to do.

## Scope: chapter 1 is complete

- [x] Lexer, parser, generator, and emitter for a single `int main(void)`
      returning an integer constant
- [x] Stage flags `--lex`, `--parse`, `--codegen`, and `-S`, mutually exclusive
- [x] `--version` reports the version without needing GCC
- [x] macOS and Linux x86-64 targets; other hosts rejected up front
- [x] Diagnostics instead of crashes for bad input, out-of-range constants,
      missing GCC, and file errors
- [x] Unreleased driver fixes since the alpha (private temp directory,
      `@file.c` sources, error before usage, non-x86-64 Linux rejected)
- [ ] Decide which open PRs belong in 0.1.0. #12 (statement and expression
      unions) is chapter 2 work, so merge it after tagging. #9 (xUnit v3),
      #10 (benchmarks), and #11 (notes protection) can go in before or after.
- [ ] Decide whether chapter 1 extra credit is out of scope, and say so in the
      release notes (the alpha notes already say it is not implemented)

## Tests

- [x] Unit tests for each stage, an in-memory pipeline test, and CLI
      integration tests
- [x] Book suite for chapter 1, full and at each stage, in CI on macOS and
      Linux, Debug and Release
- [x] Release workflow runs the full test workflow and then the book suite
      against each native executable
- [x] Book tests pinned to a reviewed commit
- [ ] CI is green on the exact commit to be tagged
- [ ] Manual dry run of the release workflow
      (`gh workflow run release.yml --ref main`) passes
- [ ] Smoke test of both downloaded archives on a clean machine without .NET:
      `--version`, compile `data/return_2.c`, run it, check exit code `2`
- [ ] Check the macOS archive after a browser download. Gatekeeper may
      quarantine the unsigned binary; document `xattr -d com.apple.quarantine`
      or sign it

## Version and release files

- [x] Version comes from `Directory.Build.props`, and the release workflow
      checks it against the tag
- [ ] Set `Version` to `0.1.0`
- [ ] Move the changelog's `Unreleased` items under a dated `0.1.0` heading,
      with Added, Fixed, and Limitations sections
- [ ] Add `docs/releases/v0.1.0.md`. The workflow fails without it. Use
      Ubuntu 24.04 as the Linux build host; the alpha notes say 26.04, which
      the workflow no longer uses.
- [ ] Release notes list both archives, prerequisites (GCC, Rosetta 2 on
      Apple Silicon), how to verify the attestation, and known limitations

## Documentation

- [x] README covers purpose, capabilities, quick start, and license
- [x] Development guide covers setup, building, publishing, testing, CI, and
      versioning
- [x] Architecture and grammar docs
- [x] Concepts folder with its own README
- [ ] Development guide: replace the alpha-only steps and `v0.0.1-alpha`
      examples ("For this alpha, stop for explicit confirmation...", tag
      commands, archive tree) with version-neutral or `v0.1.0` examples
- [ ] README: add prerequisites (pinned .NET SDK, GCC, Rosetta 2) and links
      to the development guide, changelog, and GitHub Releases download
- [ ] README: say that Linux x86-64 is supported, not only macOS
- [ ] `AGENTS.md`: says the compiler targets macOS x86-64 only, but Linux
      x86-64 is now supported
- [ ] Decide how to describe the dependency on .NET 11 RC1. GA is expected in
      November 2026. Release on RC1 and note it, or wait and move
      `global.json` to GA.

## General open-source practice

- [x] License (MIT) in the repository and in each archive
- [x] Changelog
- [x] Security policy (`SECURITY.md`)
- [x] Semantic, chapter-based versioning documented
- [x] Dependabot for GitHub Actions and NuGet, with cooldown
- [x] Least-privilege workflow tokens and `persist-credentials: false`
- [x] Build provenance attestations for release archives
- [x] Warnings treated as errors; Native AOT and trimming analyzers on every
      build
- [x] `.editorconfig` and a formatting command
- [ ] `SECURITY.md`: say how to report a vulnerability (for example, enable
      GitHub private vulnerability reporting) and which versions get fixes
- [ ] Publish SHA-256 checksums for the archives alongside the attestations
- [ ] Add third-party notices to the archives. The native binary includes the
      .NET runtime and System.CommandLine, both MIT.
- [ ] Pin third-party actions to commit SHAs, or record why version tags are
      acceptable here
- [ ] Protect `main`: make the Tests check required and block force pushes
- [ ] Add `CONTRIBUTING.md`, or a short README section saying this is a
      personal learning project and whether issues and PRs are welcome
- [ ] Optional: issue and PR templates, a code of conduct, and a repository
      description and topics on GitHub

## Release day

- [ ] Final review of the release commit on `main`
- [ ] Create and push the annotated tag `v0.1.0`; the workflow publishes it as
      a normal release (not a prerelease) and marks it Latest
- [ ] Check the GitHub Release: both archives, notes, and attestations
      (`gh attestation verify ... --repo zachdillard/Compiler`)
- [ ] Download each archive and repeat the smoke test
- [ ] Add a new empty `Unreleased` section to the changelog, then start
      chapter 2
