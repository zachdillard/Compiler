# Compiler

A small C compiler built with C# and .NET 11 based on [*Writing a C Compiler*](https://nostarch.com/writing-c-compiler) by Nora Sandler.

## Purpose

The goals of this project are to:

- Learn how compilers work by building one step by step.
- Showcase and explore .NET 11 features.
- Learn more about CI/CD through hands-on practice.
- Experiment with different development methodologies and agentic engineering.

## Capabilities

The compiler currently completes chapter one: a single `int` function
with `void` parameters that returns an integer constant.

```c
int main(void) {
    return 2;
}
```

Compiling this program creates an executable that returns `2` to your shell.

## Prerequisites

- macOS 27 with Rosetta 2 installed
- GCC on `PATH` (Apple's Clang-backed `gcc` works).
- .NET 11 RC1 SDK **11.0.100-rc.1.26425.128**

See the [development guide](docs/development.md#setup) for installation steps
and SDK troubleshooting.

## Quick Start

With Git and the prerequisites installed, run:

```sh
git clone https://github.com/zachdillard/compiler.git
cd compiler
./scripts/build.sh
./scripts/run.sh data/return_2.c
./data/return_2
echo $? # Expected: 2
```

The compiler creates `data/return_2` beside the source file. The generated
program prints nothing and exits with status `2`, which `echo $?` displays.

## Usage

Run commands from the repository root:

| Command | What it does |
| --- | --- |
| `./scripts/build.sh` | Build the solution in Debug; also accepts `Debug` or `Release`. |
| `./scripts/run.sh data/return_2.c` | Compile a C source file, forwarding arguments to the compiler. |
| `./scripts/test.sh` | Run the .NET tests and chapter-one book tests, building once. |
| `./scripts/book.sh --chapter 1` | Build the compiler and run the chapter-one book tests. |

For a Release build, use `./scripts/build.sh Release`. To run both test suites
in Release, use `BUILD_CONFIGURATION=Release ./scripts/test.sh`.
Book tests require a separate checkout of the book's test repository; see the
[development guide](docs/development.md#optional-book-tests) for setup.
To reuse an existing compiler build, use `./scripts/book.sh --no-build --chapter 1`.

Pass compiler options and one C source path directly to `run.sh`.
The source path must end in `.c`; other extensions are rejected before GCC runs.
For example, to inspect the custom compiler's assembly:

```sh
./scripts/run.sh -S data/return_2.c
```

| Option | What it does |
| --- | --- |
| `--help`, `-h` | Show generated help for the compiler's arguments and options. |
| `--version` | Print the version without GCC; use alone, with no source path. |
| `--lex` | Check tokens without printing them or generating output files. |
| `--parse` | Parse the C program without generating output files. |
| `--codegen` | Generate the assembly AST without emitting output files. |
| `-S` | Write custom assembly without linking an executable. |

Assembly-only mode leaves `data/return_2.s`. Successful executable builds remove
intermediate `.i` and `.s` files. All stage-only modes remove `.i`.
The stage flags (`--lex`, `--parse`, `--codegen`, and `-S`) are mutually exclusive
and may appear before or after the source path. Each exits with `0` when its
stages succeed and a nonzero status on an error. `--codegen` stops before
assembly text emission; only `-S` writes a `.s` file.
Integer constants must fit in a C# `int`; out-of-range constants report an error
and exit with status `1` in every compilation mode.

Cleanup is also attempted after preprocessing fails. If a compiler file cannot
be read, written, or removed, the compiler reports a diagnostic on standard error
and exits with status `1`.

## Further reading

- [Compiler architecture](docs/architecture.md): grammar, pipeline, and compiler stages.
- [C AST](docs/c-ast.md) and [assembly AST](docs/asm-ast.md): current intermediate representations.
- [Development guide](docs/development.md): setup, debugging, testing, and versioning.
- [Concepts](concepts/README.md): standalone C# examples to explore.
- [Notes](notes/): working notes for the book chapters.
- [Changelog](CHANGELOG.md): release history and current limitations.

## License

[MIT](LICENSE).
