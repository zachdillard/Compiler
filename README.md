# Compiler

A small C compiler built with C# and .NET 11 based on [*Writing a C Compiler*](https://nostarch.com/writing-c-compiler) by Nora Sandler.

## Purpose

The goals of this project are to:

- Learn how compilers work by building one step by step.
- Showcase and explore .NET 11 features.
- Learn more about CI/CD through hands-on practice.
- Experiment with different development methodologies and agentic engineering.

## Capabilities

The compiler completes chapter one: a single `int` function
with `void` parameters that returns an integer constant.

```c
int main(void) {
    return 2;
}
```

Compiling this program creates an executable that returns `2` to your shell.

## Prerequisites

- macOS 27 (Rosetta 2 is required on Apple Silicon), or Linux x86-64.
- GCC on `PATH` (Apple's Clang-backed `gcc` works).
- .NET 11 RC1 SDK **11.0.100-rc.1.26425.128**

The compiler automatically selects macOS or Linux assembly for its host OS.
Both targets generate x86-64 programs.

See the [development guide](docs/development.md#setup) for installation steps
and SDK troubleshooting.

## Quick Start

### Build the compiler

Clone the repository and build:

```sh
git clone https://github.com/zachdillard/Compiler.git
cd Compiler
dotnet build
```

### Run the compiler

Add the build output to `PATH` for the current terminal, then compile a C file:

```sh
export PATH="$PWD/src/bin/Debug/net11.0:$PATH"
Compiler data/return_2.c
```

The compiler creates `data/return_2` beside the source file.

### Run the generated program

```sh
./data/return_2
echo $?
# 2
```

The generated program prints nothing and exits with status `2` and `echo $?` displays the result.

## Further reading

- [Compiler architecture](docs/architecture.md): grammar, pipeline, and compiler stages.
- [C AST](docs/c-ast.md) and [assembly AST](docs/asm-ast.md): current intermediate representations.
- [Development guide](docs/development.md): setup, debugging, testing, and versioning.
- [Concepts](concepts/README.md): standalone C# examples to explore.
- [Notes](notes/): working notes for the book chapters.
- [Changelog](CHANGELOG.md): release history and current limitations.

## License

[MIT](LICENSE).
