# Changelog

## 0.0.1-alpha — Prepared for testing

This chapter 1 alpha is a testing prerelease. Version `0.1.0` is reserved for
the stable chapter 1 release.

### Added

- `--parse` and `--codegen` stop after parsing and assembly AST generation without
  emitting output files. Stage flags (`--lex`, `--parse`, `--codegen`, and `-S`)
  are mutually exclusive.
- Intermediate-stage book tests in CI and assembly-only output validation through
  GCC assembly, linking, and execution.

- Chapter one custom compiler: a single `int` function with `void` parameters
  returning an integer constant, emitted as macOS or Linux x86-64 assembly or
  an executable.
- Native AOT release packages for macOS Arm64 and Linux x64 that run without
  an installed .NET runtime.
- Shared version metadata in `Directory.Build.props` and `--version`, which
  reports the compiler version without requiring GCC.
- MIT license and chapter-based versioning policy.

### Fixed

- Inputs without a `.c` extension are rejected before preprocessing, preventing
  intermediate-file cleanup from deleting an input `.i` file.
- Preprocessing selects the x86-64 target on macOS so architecture macros match
  the generated executable on Apple Silicon.
- CI also runs when root build configuration or the test workflow changes.
- Out-of-range integer constants report a diagnostic and exit with status `1`
  instead of crashing, including in lex-only mode.
- Partial preprocessed output is cleaned up after preprocessing failures.
- File access and intermediate-file cleanup exceptions, including assembly
  cleanup exceptions, report diagnostics instead of crashing.
- Release workflow test invocation and prerelease classification. `--version`
  preserves the alpha suffix without appending a Git commit hash.

### Limitations

- Later chapter features and extra credit are not implemented.
- GCC is required for preprocessing, assembly, and linking.
- The custom compiler generates x86-64 programs for macOS or Linux; Apple Silicon
  needs Rosetta 2 to run generated programs.
- Source builds require the pinned .NET 11 RC1 SDK.
