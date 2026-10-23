# Repository Instructions

- This is a learning, non-production project.
- The project is a C#/.NET 11 RC1 implementation of the C compiler described in *Writing a C Compiler* by Nora Sandler. Keep compiler stages clear, testable, and easy to extend.
- The .NET 11 RC1 SDK and GCC on `PATH` are required. The custom compiler targets macOS x86-64; executing its output on Apple Silicon requires Rosetta 2.
- Keep changes simple, clear, and easy to understand.
- Keep commit messages simple unless additional detail is required.
- From the repository root, use `dotnet build -m:1 --disable-build-servers` to build. These flags avoid MSBuild worker socket failures in Codex's sandbox.
- From the repository root, run the book's chapter tests with `./scripts/book.sh --chapter <number>`. The script builds the compiler into `src/bin/<configuration>/net11.0/`, defaults to a sibling `writing-a-c-compiler-tests` checkout and forwards options to its `test_compiler` runner; use `BOOK_TESTS_DIR` when the test checkout is elsewhere. Python 3.8 or newer is required. Set `BUILD_CONFIGURATION` to select a build configuration; the default is Debug.
- The compiler accepts one C source path: `dotnet run --project src/Compiler.csproj -- <source-file>`. This selects the custom compiler implementation, which completes chapter one: a single `int` function with `void` parameters returning an integer constant.
- Use `dotnet run --project src/Compiler.csproj -- -S <source-file>` for assembly-only mode without linking. The intermediate `.i` file is removed. GCC is used for preprocessing and for assembling and linking the custom compiler output.
- The stage flags `--lex`, `--parse`, `--codegen`, and `-S` are mutually exclusive. The first three stop after tokens, the C AST, and the assembly AST respectively without emitting files. All modes remove `.i`. Run the chapter-one book suite with `--stage lex`, `--stage parse`, or `--stage codegen` to validate intermediate stages. Validate `-S` with a temporary C source file and check that assembly is emitted without linking.
- The `concepts/` folder contains self-contained, file-based C# examples. From that folder, run an example individually with `dotnet <concept-name>.cs`.
- When adding a new concept, add a matching build task in `.vscode/tasks.json` and debug configuration in `.vscode/launch.json`, using the labels `Build <Name> Concept` and `Debug <Name> Concept`.
