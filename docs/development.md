# Development guide

Start with the [README](../README.md) for a quick introduction and compiler
options. Run commands from the repository root unless noted otherwise.

## Setup

- .NET 11 RC1 SDK **11.0.100-rc.1.26425.128**, as specified in [global.json](../global.json)
- GCC available on `PATH` (Apple's Clang-backed `gcc` is suitable)
- For the custom compiler: macOS with x86-64 assembly/linking support. Running
  its executables on Apple Silicon requires Rosetta 2.

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
git clone https://github.com/zachdillard/compiler.git
cd compiler
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
dotnet build -m:1 --disable-build-servers
```

These flags avoid MSBuild worker socket failures in Codex’s sandbox.

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

### Debug and Release checks

To check the project and chapter-one book tests in both build configurations:

```sh
dotnet build --configuration Debug -m:1 --disable-build-servers
dotnet test --configuration Debug --no-build
BUILD_CONFIGURATION=Debug ./tests/book_tests.sh --chapter 1
dotnet build --configuration Release -m:1 --disable-build-servers
dotnet test --configuration Release --no-build
BUILD_CONFIGURATION=Release ./tests/book_tests.sh --chapter 1
```

### What the tests cover

The xUnit v2 project includes unit tests for Lexer, Parser, Generator, and
Emitter, an in-memory pipeline test from C source to assembly text, and
integration tests for the compiler executable. The unit and in-memory pipeline
tests do not invoke GCC or require Rosetta.

Integration tests launch the compiler DLL copied into the test output, using
that build's test runtime configuration and dependency manifest. They do not
require a separate Debug build when testing Release. They require GCC on
`PATH`; executing custom compiler output on Apple Silicon also requires Rosetta 2.
They check CLI behavior, diagnostics, intermediate-file cleanup, and execution
of generated programs.

Regression cases cover out-of-range integer constants in normal and lex-only
modes, partial preprocessing output, missing GCC, and file-write and cleanup
errors. Some failure-path tests use a temporary GCC stub on `PATH` to reproduce
partial output and assembly cleanup failures deterministically.

### Continuous integration

[GitHub Actions](../.github/workflows/compiler-tests.yml) runs these builds and
tests on an Intel macOS runner for pushes to `main` and pull requests that change
`src/`, `tests/`, `global.json`, `Directory.Build.props`, `Compiler.slnx`, or the
workflow itself. It can also be started manually. It installs the SDK from
`global.json` and checks GCC availability. Each configuration also runs the
chapter-one book suite against a separate checkout of the upstream tests.

### Optional book tests

The book suite is a separate checkout and requires Git, Python 3.8 or newer,
and the compiler prerequisites above. From the compiler repository root:

```sh
git clone https://github.com/nlsandler/writing-a-c-compiler-tests.git \
  ../writing-a-c-compiler-tests
./tests/book_tests.sh --chapter 1
```

This runs the full chapter-one suite against the custom compiler, including
valid programs and invalid lexing/parsing cases. To test only lexing, use:

```sh
./tests/book_tests.sh --chapter 1 --stage lex
```

Run the script from the compiler repository root so `dotnet` uses the SDK
selection in `global.json` and the script resolves its paths correctly. It builds
the compiler into `artifacts/book-tests/`, restoring dependencies as needed,
then runs the book's `test_compiler` runner with your arguments. The generated
build output is ignored by Git.

For an existing checkout elsewhere:

```sh
BOOK_TESTS_DIR="/path/to/writing-a-c-compiler-tests" \
  ./tests/book_tests.sh --chapter 1
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

Each `v0.N.0` release completes chapter N, including all earlier chapters.
Patch releases use `v0.N.P` for fixes within that chapter's scope. `v1.0.0`
is reserved for a separate production-readiness milestone.

The version is defined in [Directory.Build.props](../Directory.Build.props).
See the [changelog](../CHANGELOG.md) for release history and unreleased work.
