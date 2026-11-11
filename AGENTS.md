# Repository Instructions

## Project conventions

- This is a learning, non-production project.
- Keep commit messages simple unless additional detail is required.
- The project is a C#/.NET 11 implementation of the C compiler described in *Writing a C Compiler* by Nora Sandler. Keep compiler stages clear, testable, and easy to extend.
- Never edit the human-written notes in `notes/`.

## Build and test

The stable .NET 11 SDK specified in `global.json` and GCC on `PATH` are required.

The compiler can be built on macOS and Linux. Build from the repository root.

```sh
dotnet build -m:1 --disable-build-servers
```

These flags avoid MSBuild worker socket failures in Codex's sandbox.

Run integration tests from the repository root.

```sh
dotnet test
```

Tests require GCC on `PATH`.

Run the book's chapter tests from the repository root.

```sh
./scripts/book.sh --chapter <number>
```

The script builds the compiler into `src/bin/<configuration>/net11.0/`, defaults to a sibling `writing-a-c-compiler-tests` checkout and forwards options to its `test_compiler` runner; use `BOOK_TESTS_DIR` when the test checkout is elsewhere. Python 3.8 or newer is required. Set `BUILD_CONFIGURATION` to select a build configuration; the default is Debug.

## Run the compiler

The custom compiler selects the output target based on the host operating system.

- On macOS, the compiler targets macOS x86-64. Running its output on Apple Silicon requires Rosetta 2.
- On Linux, the compiler targets Linux x86-64 and requires an x86-64 Linux host with GCC on `PATH`.

The compiler accepts one C source path.

```sh
dotnet run --project src/Compiler.csproj -- <source-file>
```

This selects the custom compiler implementation, which completes chapter one: a single `int` function with `void` parameters returning an integer constant.

Use `-S` for assembly-only mode without linking.

```sh
dotnet run --project src/Compiler.csproj -- -S <source-file>
```

The intermediate `.i` file is removed. GCC is used for preprocessing and for assembling and linking the custom compiler output.

The stage flags `--lex`, `--parse`, `--codegen`, and `-S` are mutually exclusive. The first three stop after tokens, the C AST, and the assembly AST respectively without emitting files. All modes remove `.i`. Run the chapter-one book suite with `--stage lex`, `--stage parse`, or `--stage codegen` to validate intermediate stages; integration tests validate `-S`.

## Add examples

- The `concepts/` folder contains self-contained, file-based C# examples. From that folder, run an example individually with `dotnet <concept-name>.cs`.
- When adding a new concept, add a matching build task in `.vscode/tasks.json` and debug configuration in `.vscode/launch.json`, using the labels `Build <Name> Concept` and `Debug <Name> Concept`.
- When adding a new C source file under `examples/`, add its generated output file to `.gitignore`.
