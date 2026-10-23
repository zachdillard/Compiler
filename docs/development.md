# Development guide

Start with the [README](../README.md) for a quick introduction. Run commands
from the repository root unless noted otherwise.

## Setup

- .NET 11 RC1 SDK **11.0.100-rc.1.26425.128**, as specified in [global.json](../global.json)
- GCC available on `PATH` (Apple's Clang-backed `gcc` is suitable)
- For the custom compiler: macOS with x86-64 assembly/linking support, or
  Linux x86-64. The host OS selects the compilation target. Running its
  executables on Apple Silicon requires Rosetta 2.

Install the SDK from the [official .NET 11 download page](https://dotnet.microsoft.com/en-us/download/dotnet/11.0),
selecting the RC1 SDK version above: Arm64 for Apple Silicon or x64 for Intel
Macs. The repository uses preview C# features and limited SDK roll-forward;
installing only a newer SDK may not satisfy `global.json`.

Install Apple's Command Line Tools if you do not already have them:

```sh
xcode-select --install
```

On Apple Silicon, install Rosetta 2 if needed to execute the custom compiler's
x86-64 output:

```sh
softwareupdate --install-rosetta
```

With Git and the prerequisites installed, clone the repository and enter it:

```sh
git clone https://github.com/zachdillard/Compiler.git
cd Compiler
```

If you already cloned it, open a terminal in that checkout's root directory
(the folder containing `global.json`). Run the remaining setup commands there.

Check the prerequisites before building:

```sh
dotnet --version
gcc --version
xcode-select -p
```

Running `dotnet --version` inside the checkout checks SDK selection against
`global.json`. If `dotnet` is missing or reports that no compatible SDK is
installed, install the pinned RC1 SDK linked above, open a new terminal, and
return to the checkout before trying again. If the error persists, use
`dotnet --list-sdks` to check that the required SDK is installed.

The first project build or test run needs access to NuGet to restore test
dependencies.

Build from the repository root with:

```sh
./scripts/build.sh
./scripts/build.sh Release
```

The build script defaults to Debug and accepts `Debug` or `Release`.

The build script uses `-m:1 --disable-build-servers` to avoid MSBuild worker
socket failures in Codex’s sandbox.

## Publish a native compiler

On an Apple Silicon Mac or Linux x86-64, publish a single native `Compiler`
executable:

```sh
./scripts/publish.sh
```

The script selects the host's runtime: `osx-arm64` on Apple Silicon or
`linux-x64` on Linux x86-64. It rejects other platforms. It publishes a Release
build using Native AOT to `artifacts/publish/<runtime>/Release/`, which is
ignored by Git.
Debug symbols are excluded from the publish output, leaving a single executable.
`src/Compiler.csproj` sets `PublishAot`, so every build also runs the Native AOT
and trimming analyzers, and `Directory.Build.props` treats warnings as errors.

The same script also creates
`artifacts/release/compiler-<version>-<runtime>.tar.gz`, using the version from
`Directory.Build.props`. Extracting it produces:

```text
compiler-0.0.1-alpha-osx-arm64/
├── Compiler
├── README.md
└── LICENSE
```

Publishing requires the pinned .NET SDK and the host's native toolchain:
Xcode Command Line Tools on macOS, or Clang and zlib development headers on
Linux (`sudo apt-get install clang zlib1g-dev` on Ubuntu). The first run needs
NuGet access to restore the native compiler packages. Native AOT builds must
run on the target OS; the script produces one package for its host per run.

The published binary runs without an installed .NET runtime, but still needs
GCC on `PATH`. It generates x86-64 programs for the host OS; running those
programs on Apple Silicon requires Rosetta 2. Linux binaries require a
compatible system library baseline; the release workflow builds on Ubuntu 24.04.

### Create a GitHub release

Update `Version` in `Directory.Build.props` and the changelog, and add release
notes as `docs/releases/v<version>.md`. Commit and push those changes, then
verify CI. To rehearse a release, run the release workflow manually. This dry
run tests, builds, and uploads both packages as workflow artifacts but never
publishes, even when started on a tag. Run from a branch, it also skips the
tag check:

```sh
gh workflow run release.yml --ref main
```

For this alpha, stop for explicit confirmation before creating or pushing the
release tag. Only pushing the tag triggers publication. After confirmation,
create and push a matching version tag on the reviewed commit:

```sh
git tag -a v0.0.1-alpha -m "Release v0.0.1-alpha"
git push origin v0.0.1-alpha
```

The [release workflow](../.github/workflows/release.yml) first calls
[`tests.yml`](../.github/workflows/tests.yml) for the tagged commit. All Debug
and Release tests, including the book suite, must pass on both platforms
before publishing starts. It then runs `scripts/publish.sh` on Apple Silicon
(`xcode-27`) and Linux x86-64 (`ubuntu-24.04`) and runs the chapter-one book
suite against each native executable. For tags, each runner also checks that
its executable's version matches the tag and that `docs/releases/<tag>.md`
exists. After both builds succeed, a separate job creates build provenance
attestations for both archives, then one GitHub Release that uses
`docs/releases/<tag>.md` as its notes. Tags with a prerelease suffix (such as
`v0.0.1-alpha`) create a GitHub prerelease and are excluded from Latest. Stable
tags retain normal release behavior.

The workflow uses GitHub's built-in token; no additional secret is needed. Only
the release job can write to the repository and sign attestations; the build
job, which restores packages and runs tests, has a read-only token. Running
`scripts/publish.sh` locally only builds and packages the compiler.

### Download a release

Download `compiler-<version>-osx-arm64.tar.gz` for Apple Silicon or
`compiler-<version>-linux-x64.tar.gz` for Linux x86-64 from
[GitHub Releases](https://github.com/zachdillard/Compiler/releases) and extract it.
The archive contains a versioned folder with `Compiler`, `README.md`, and `LICENSE`.
Run `Compiler` from that folder, or add the folder to `PATH`:

```sh
./compiler-0.0.1-alpha-osx-arm64/Compiler program.c
```

Both native executables run without .NET installed. GCC is still required to
compile programs, and Apple Silicon also needs Rosetta 2 to run the generated
x86-64 programs.

Releases after `v0.0.1-alpha` include build provenance attestations. To check
that an archive was built by this repository's release workflow, verify it with
the [GitHub CLI](https://cli.github.com/):

```sh
gh attestation verify compiler-<version>-<runtime>.tar.gz --repo zachdillard/Compiler
```

## Usage

Compile a C source file or generate assembly only:

```sh
./scripts/run.sh data/return_2.c
./scripts/run.sh -S data/return_2.c
```

Pass compiler options directly to `run.sh`; no extra `--` is needed.
Use `./scripts/run.sh --help` for all compiler options. The
[compiler pipeline](architecture.md#pipeline) describes the stage flags
`--lex`, `--parse`, `--codegen`, and `-S`; choose one per run.

## Debugging in VS Code

Install the Microsoft C# extension and open the repository root in VS Code.
There is a debug configuration for the main compiler and each example in
`concepts/`. Choose a configuration in the Run and Debug view, set breakpoints,
and press F5. Each configuration runs its matching build task before launching.

The main compiler configuration uses `data/return_2.c` as its input. To debug
another source file or compiler mode, edit its `args` in
[`.vscode/launch.json`](../.vscode/launch.json).

## Formatting

The repository's `.editorconfig` defines C# formatting. To format the main
application and tests without changing the standalone concepts examples, run:

```sh
dotnet format whitespace Compiler.slnx
```

## Testing

Run the project tests from the repository root:

```sh
dotnet test
```

Run both the project tests and chapter-one book tests with:

```sh
./scripts/test.sh
```

This requires the book test checkout described below. Arguments are forwarded
to the book runner; for example, `./scripts/test.sh --chapter 1 --stage lex`.
Set `BUILD_CONFIGURATION=Release` to run both suites in Release.
The book tests reuse the compiler built by `dotnet test`.

### Debug and Release checks

To check the project and chapter-one book tests in both build configurations:

```sh
dotnet build --configuration Debug -m:1 --disable-build-servers
dotnet test --configuration Debug --no-build
BUILD_CONFIGURATION=Debug ./scripts/book.sh --no-build --chapter 1
dotnet build --configuration Release -m:1 --disable-build-servers
dotnet test --configuration Release --no-build
BUILD_CONFIGURATION=Release ./scripts/book.sh --no-build --chapter 1
```

### What the tests cover

The xUnit v3 project includes unit tests for Lexer, Parser, Generator, and
Emitter, an in-memory pipeline test from C source to assembly text, and
integration tests for the compiler executable. The unit and in-memory pipeline
tests do not invoke GCC or require Rosetta.

The test project builds as an executable, as required by xUnit v3. It uses
`xunit.v3.mtp-off` to retain VSTest and the existing `dotnet test` commands.

Stage-position, integer-overflow, and temporary-directory cases use xUnit v3
matrix theory data. Cleanup cases combine every compilation stage with valid,
invalid-syntax, and missing-entry-point sources.

Integration tests have a 30-second timeout and pass xUnit's cancellation token
to an asynchronous process helper. The helper reads stdout and stderr
concurrently, kills the process tree on cancellation, and allows up to five
seconds each for process exit and output draining. Compiler scratch directories
are owned by the helper so cancellation can also clean up temporary files.
Regression tests exercise large stderr output, nonzero exits, cancellation
before startup, and cancellation of a compiler blocked in GCC, including its
child process.

Console capture associates command, exit status, cancellation status, and up to
4096 characters from each output stream with the current test. Complete output
is still returned to assertions. Unsupported hosts skip integration tests at
runtime; GCC remains required on supported macOS and Linux x86-64 hosts.

Integration tests launch the compiler DLL copied into the test output, using
that build's test runtime configuration and dependency manifest. They do not
require a separate Debug build when testing Release. They require GCC on
`PATH`; executing custom compiler output on Apple Silicon also requires Rosetta 2.
They check CLI behavior, diagnostics, intermediate-file cleanup, and execution
of generated programs, including assembling and linking the output of `-S`.
Stage tests cover `--lex`, `--parse`, `--codegen`, and `-S`, including rejecting
combinations and ensuring that stage-only modes do not invoke the assembler.

Regression cases cover out-of-range integer constants in every compilation
mode, partial preprocessing output, missing GCC, and file-write and cleanup
errors. Some failure-path tests use a temporary GCC stub on `PATH` to reproduce
partial output and assembly cleanup failures deterministically.

### Continuous integration

[GitHub Actions](../.github/workflows/tests.yml) runs these builds and
tests on the Apple Silicon `xcode-27` preview runner and Linux x86-64
`ubuntu-24.04` runner, the same Ubuntu version as the Linux release build, for
every push to `main` and every pull request targeting `main`. Because it always
runs, the Tests check can be a required status check.
It can also be started manually or called by the release workflow. It installs
the SDK from `global.json` and checks GCC availability. Each configuration also
runs the chapter-one book suite in full and at the lex, parse, and codegen
stages against a separate checkout of the upstream tests. That checkout is
pinned to a reviewed commit because CI runs its Python; update the `ref` in
both workflows to use newer tests.

The workflows use a read-only token by default and do not keep Git credentials
in their checkouts (`persist-credentials: false`).
[Dependabot](../.github/dependabot.yml) checks GitHub Actions and NuGet
packages monthly and groups each ecosystem's updates into one pull request.

### Optional book tests

The book suite is a separate checkout and requires Git, Python 3.8 or newer,
and the compiler prerequisites above. From the compiler repository root:

```sh
git clone https://github.com/nlsandler/writing-a-c-compiler-tests.git \
  ../writing-a-c-compiler-tests
./scripts/book.sh --chapter 1
```

This runs the full chapter-one suite against the custom compiler, including
valid programs and invalid lexing/parsing cases. To test individual stages, use:

```sh
./scripts/book.sh --chapter 1 --stage lex
./scripts/book.sh --chapter 1 --stage parse
./scripts/book.sh --chapter 1 --stage codegen
```

The runner has no assembly-only stage; the project integration tests validate
`-S`, including linking its assembly with GCC and executing the result.

The script switches to the compiler repository root so `dotnet` uses the SDK
selection in `global.json` and repository-relative paths resolve correctly. It builds
the compiler into `src/bin/<configuration>/net11.0/`, restoring dependencies
as needed, then runs the book's `test_compiler` runner with your arguments. The generated
build output is ignored by Git.

To reuse an existing compiler build, pass `--no-build` as the first argument:
`./scripts/book.sh --no-build --chapter 1`.

For an existing checkout elsewhere:

```sh
BOOK_TESTS_DIR="/path/to/writing-a-c-compiler-tests" \
  ./scripts/book.sh --chapter 1
```

See the
[`writing-a-c-compiler-tests`](https://github.com/nlsandler/writing-a-c-compiler-tests)
repository for additional test runner usage. The script defaults to a Debug
build; set `BUILD_CONFIGURATION=Release` to test a Release build.

To compile an individual book-test file directly:

```sh
dotnet run --project src/Compiler.csproj -- /path/to/writing-a-c-compiler-tests/tests/chapter_1/valid/return_2.c
```

## Concepts

The `concepts/` folder contains self-contained, file-based C# examples. Each
example can be run individually from that folder with:

```sh
cd concepts
dotnet <concept-name>.cs
```

See the [concepts guide](../concepts/README.md) for the available examples,
requirements, and run instructions.

## Versioning

The initial `v0.0.1-alpha` prerelease is for testing chapter 1. Version `0.1.0`
is reserved for the stable chapter 1 release. Each stable `v0.N.0` release
completes chapter N, including all earlier chapters.
Patch releases use `v0.N.P` for fixes within that chapter's scope. `v1.0.0`
is reserved for a separate production-readiness milestone.

The version is defined in [Directory.Build.props](../Directory.Build.props).
See the [changelog](../CHANGELOG.md) for release history and unreleased work.
