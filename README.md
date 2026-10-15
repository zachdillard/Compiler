# C Compiler

A small C compiler built with C# and .NET 11 RC1, following
[*Writing a C Compiler*](https://nostarch.com/writing-c-compiler) by Nora Sandler.
This is a learning and experimentation project, with more language features added
as work progresses through the book.

## Purpose

The goals of this project are to:

- Learn how compilers work by building one step by step.
- Showcase and explore .NET 11 features.
- Learn more about CI/CD through hands-on practice.
- Experiment with different development methodologies and agentic engineering.

## Current capabilities

The custom compiler currently completes chapter one: a single `int` function
with `void` parameters that returns an integer constant, like the included
[data/return_2.c](data/return_2.c):

```c
int main(void) {
    return 2;
}
```

Compiling this program creates an executable that returns `2` to your shell.

## Prerequisites

- .NET 11 RC1 SDK **11.0.100-rc.1.26425.128**, pinned in [global.json](global.json).
- GCC on `PATH` (Apple's Clang-backed `gcc` works).
- macOS with x86-64 assembly and linking support. On Apple Silicon, Rosetta 2
  is needed to run the custom compiler's executables.

See the [development guide](docs/development.md#setup) for installation steps
and SDK troubleshooting.

## Quick start

With Git and the prerequisites installed, run:

```sh
git clone https://github.com/zachdillard/compiler.git
cd compiler
dotnet build -m:1 --disable-build-servers
dotnet run --project src/Compiler.csproj -- data/return_2.c
./data/return_2
echo $? # Expected: 2
```

The compiler creates `data/return_2` beside the source file. The generated
program prints nothing and exits with status `2`, which `echo $?` displays.

## Usage

Run commands from the repository root. The `--` separates `dotnet run` options
from the arguments passed to the compiler: its options and one C source path.
For example, to inspect the custom compiler's assembly:

```sh
dotnet run --project src/Compiler.csproj -- -S data/return_2.c
```

| Option | What it does |
| --- | --- |
| `--help`, `-h` | Show generated help for the compiler's arguments and options. |
| `--version` | Print the version without GCC; use alone, with no source path. |
| `--lex` | Check tokens without printing them or generating output files. |
| `-S` | Write custom assembly without linking an executable. |

Assembly-only mode leaves `data/return_2.s`. Successful executable builds remove
intermediate `.i` and `.s` files; assembly-only and lex-only modes remove `.i`.
Lex-only mode exits with `0` for valid tokens and a nonzero status on an error.
Integer constants must fit in a C# `int`; out-of-range constants report an error
and exit with status `1` in both normal and lex-only modes.

Cleanup is also attempted after preprocessing fails. If a compiler file cannot
be read, written, or removed, the compiler reports a diagnostic on standard error
and exits with status `1`.

## Testing

Run the project tests from the repository root:

```sh
dotnet test
```

Integration tests need GCC and, on Apple Silicon, Rosetta 2. The
[development guide](docs/development.md#testing) covers the optional book suite,
Debug/Release checks, and CI.

## Further reading

- [Compiler architecture](docs/architecture.md): grammar, pipeline, and compiler stages.
- [C AST](docs/c-ast.md) and [assembly AST](docs/asm-ast.md): current intermediate representations.
- [Development guide](docs/development.md): setup, debugging, testing, and versioning.
- [Concepts guide](concepts/README.md): standalone C# examples to explore.
- [Chapter 1 notes](notes/chapter-1.md) and [chapter 2 notes](notes/chapter-2.md): working notes for the book chapters.
- [Changelog](CHANGELOG.md): release history and current limitations.

## License

[MIT](LICENSE).
