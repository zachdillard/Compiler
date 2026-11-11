# Changelog

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Version numbers use the [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
format with [chapter-based versioning](docs/development.md#versioning).

## Unreleased

## [0.1.0] - 2026-11-11

### Added

- Chapter 1 compiler for a single `int` function with `void` parameters and one `return` statement containing a positive integer literal.
- x86-64 assembly and executable output for macOS and Linux, using `gcc` to preprocess, assemble, and link.
- Stage flags `--lex`, `--parse`, `--codegen`, and `-S`, plus `--version`.
- Native archives for Apple Silicon and Linux x86-64 that run without .NET, with build provenance attestations and dependency notices.

[0.1.0]: docs/releases/v0.1.0.md
