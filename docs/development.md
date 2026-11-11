# Development Guide

Run commands from the repository root. See the [README Quick Start](../README.md#quick-start) to clone the project and check supported platforms.

## Setup

- **.NET SDK.** Install 11.0.100 or a later stable patch in the 11.0.1xx feature band. See [global.json](../global.json).
- **GCC.** Put `gcc` on `PATH`. On macOS, the Clang-backed `gcc` from Apple's Command Line Tools works. Install the tools with `xcode-select --install`.
- **Rosetta 2.** On Apple Silicon, install Rosetta 2 to run generated programs and integration tests that execute them.

The first build needs access to NuGet to restore packages.

## Build and Run

The build script defaults to Debug. Pass `Release` to build in Release mode.

```sh
./scripts/build.sh
./scripts/build.sh Release
```

Compile a C file or emit assembly only.

```sh
./scripts/run.sh examples/return_int.c
./scripts/run.sh -S examples/return_int.c
```

Run `./scripts/run.sh --help` to list options. See the [stage flags](architecture.md#stage-flags) for where each mode stops.

## Test

Run the xUnit suite, which covers the compiler stages, the in-memory pipeline, and the command-line interface.

```sh
dotnet test
```

### Book tests

The book suite requires Python 3.8 or later and a separate checkout.

```sh
git clone https://github.com/nlsandler/writing-a-c-compiler-tests.git ../writing-a-c-compiler-tests
./scripts/book.sh --chapter 1
```

- Set `BOOK_TESTS_DIR` if the test checkout is elsewhere.
- Add `--stage lex`, `--stage parse`, or `--stage codegen` to check one stage. The xUnit suite covers `-S`.
- Pass `--no-build` first to reuse an existing compiler build.

Run both suites with `./scripts/test.sh`. The test and book scripts default to Debug. Set `BUILD_CONFIGURATION=Release` to use Release.

### Continuous integration

GitHub Actions runs both suites in Debug and Release on Apple Silicon macOS and Ubuntu 24.04 x86-64. The book suite runs in full and at the lex, parse, and code generation stages. See the [test workflow](../.github/workflows/tests.yml).

## Debug and Format

- **Debug.** Install the C# extension for VS Code. Select **Debug Compiler** and press F5 to build and debug with `examples/return_int.c`. Change `args` to use another input or compiler flag. The concept examples also have debug configurations. See [launch.json](../.vscode/launch.json).
- **Format.** Run `dotnet format whitespace Compiler.slnx` to apply the whitespace rules to the compiler and tests. See [.editorconfig](../.editorconfig).

## Publish a Native Compiler

Run the publish script on the platform you want to package.

```sh
./scripts/publish.sh
```

| Host | Package runtime | Native build tools |
| --- | --- | --- |
| Apple Silicon Mac | `osx-arm64` | Xcode Command Line Tools |
| Linux x86-64 | `linux-x64` | Clang and zlib development headers (`clang` and `zlib1g-dev` on Ubuntu) |

- The Native AOT executable is written to `artifacts/publish/<runtime>/Release/Compiler`. It runs without .NET and uses `gcc` to compile C programs.
- The archive is written to `artifacts/release/compiler-<version>-<runtime>.tar.gz`. It contains `Compiler`, `README.md`, `LICENSE`, and `NOTICE`.
- `NOTICE` lists the bundled components and their copyright notices. The publish script appends the third-party notices from the .NET Native AOT runtime pack used for that build. `LICENSE` contains the shared MIT terms.

## Create a GitHub Release

1. Set `Version` in [Directory.Build.props](../Directory.Build.props) and add `docs/releases/<tag>.md`. The tag must be `v` followed by the version.
2. Push the release commit and verify CI. Run `gh workflow run release.yml --ref main` to test and package both builds as workflow artifacts.
3. Create and push the matching annotated tag from the verified commit. The workflow reruns CI, tests each native executable against the book suite, and publishes both archives with build provenance attestations.

Only a pushed `v*` tag publishes a release. Tags with a prerelease suffix publish a prerelease. See the [release workflow](../.github/workflows/release.yml).

## Versioning

| Version | Meaning |
| --- | --- |
| `0.N.0` | Completes chapter N and all earlier chapters |
| `0.N.P` | Fixes within chapter N's scope |
| `1.0.0` | Reserved for a separate production-readiness milestone |
