# C Compiler

> [!WARNING]
> This project is under development. Additional compiler stages and language
> features will be added as the implementation progresses through the book.

This project is a C# and .NET 11 RC1 implementation of the C compiler described in [*Writing a C Compiler*](https://nostarch.com/writing-c-compiler) by Nora Sandler.

## Versioning and license

Each `v0.N.0` release completes chapter N, including all earlier chapters.
Patch releases use `v0.N.P` for fixes within that chapter's scope. `v1.0.0`
is reserved for a separate production-readiness milestone.
Chapter one is implemented; `v0.1.0` is being prepared and has not been published.
The version is defined in `Directory.Build.props`; see the [changelog](CHANGELOG.md)
for release history.

The project is [MIT licensed](LICENSE).

## Requirements

- .NET 11 RC1 SDK **11.0.100-rc.1.26425.128**, as specified in `global.json`
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

Check the prerequisites before building:

```sh
dotnet --version
gcc --version
xcode-select -p
```

Run `dotnet --version` inside the checkout so it uses `global.json`. The first
project build or test run needs access to NuGet to restore test dependencies.

## Quick start

With Git and the prerequisites installed:

```sh
git clone https://github.com/zachdillard/compiler.git
cd compiler
dotnet build
dotnet run --project src/Compiler.csproj -- data/return_2.c
./data/return_2
echo $?
dotnet test
```

The generated program prints nothing and exits with status `2`, which
`echo $?` displays. This is the expected result, not a compilation failure.

## Architecture

See [Compiler architecture](docs/architecture.md) for the supported grammar,
pipeline diagram, stage responsibilities, and GCC-backed mode.

## Usage

Print the version without invoking GCC:

```sh
dotnet run --project src/Compiler.csproj -- --version
```

`--version` must be used on its own.


Build the compiler from the repository root:

```sh
dotnet build
```

Compile a C source file with the custom compiler by passing its path as the only argument:

```sh
dotnet run --project src/Compiler.csproj -- data/return_2.c
```

This creates `data/return_2` beside the input file. Intermediate `.i` and `.s`
files are removed.

To stop after lexing, pass `--lex`:

```sh
dotnet run --project src/Compiler.csproj -- --lex data/return_2.c
```

A lexically valid file exits with status `0`; an invalid token produces a
nonzero exit status. Tokens are collected without being printed. Lex-only mode
does not parse the program or create assembly or an executable. Integer
constants must fit in a C# `int`.

To test a specific file from the book's test suite, pass its path directly:

```sh
dotnet run --project src/Compiler.csproj -- /path/to/writing-a-c-compiler-tests/tests/chapter_1/valid/return_2.c
```

To use the temporary GCC-backed compiler, pass `-gcc`:

```sh
dotnet run --project src/Compiler.csproj -- -gcc data/return_2.c
```

The example above creates `data/return_2` beside the input file.
Intermediate `.i` and `.s` files are removed after each successful stage.

To generate assembly without linking an executable, use `-S`:

```sh
dotnet run --project src/Compiler.csproj -- -S data/return_2.c
```

This selects the custom compiler's assembly-only mode. To generate assembly
with GCC, combine `-S` and `-gcc` in either order:

```sh
dotnet run --project src/Compiler.csproj -- -gcc -S data/return_2.c
dotnet run --project src/Compiler.csproj -- -S -gcc data/return_2.c
```

These commands create `data/return_2.s` and remove the intermediate `.i` file.

You can run the generated executable with:

```sh
./data/return_2
echo $?
```

The example exits with status `2` with either compiler. It prints nothing;
`echo $?` displays the exit status.

## Optional book tests

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
selection in `global.json`. It builds the compiler into `artifacts/book-tests/`,
restoring dependencies as needed, then runs the book's `test_compiler` runner
with your arguments. The generated build output is ignored by Git.
For an existing checkout elsewhere:

```sh
BOOK_TESTS_DIR="/path/to/writing-a-c-compiler-tests" \
  ./tests/book_tests.sh --chapter 1
```

See the
[`writing-a-c-compiler-tests`](https://github.com/nlsandler/writing-a-c-compiler-tests)
repository for additional test runner usage. The script defaults to a Debug
build; set `BUILD_CONFIGURATION=Release` to test a Release build.

## Project tests

The repository's `.editorconfig` defines C# formatting. To format the main
application and tests without changing the standalone concepts examples, run:

```sh
dotnet format whitespace Compiler.slnx
```

Run the project tests from the repository root:

```sh
dotnet test
```

To check the project and chapter-one book tests in both build configurations:

```sh
dotnet build --configuration Debug -m:1 --disable-build-servers
dotnet test --configuration Debug --no-build
BUILD_CONFIGURATION=Debug ./tests/book_tests.sh --chapter 1
dotnet build --configuration Release -m:1 --disable-build-servers
dotnet test --configuration Release --no-build
BUILD_CONFIGURATION=Release ./tests/book_tests.sh --chapter 1
```

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

GitHub Actions runs these builds and tests on an Intel macOS runner for pushes,
pull requests, and manual runs. It installs the SDK from `global.json` and checks
GCC availability. Each configuration also runs the chapter-one book suite
against a separate checkout of the upstream tests.

## Concepts

The `concepts/` folder contains self-contained, file-based C# examples. Each
example can be run individually from that folder with:

```sh
cd concepts
dotnet <concept-name>.cs
```

See the [concepts guide](concepts/README.md) for the available examples,
requirements, and VS Code debugging instructions.
