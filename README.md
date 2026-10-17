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
./scripts/run.sh data/return_2.c
./data/return_2
echo $?
# 2
```

The compiler creates `data/return_2` beside the source file. The generated
program prints nothing and exits with status `2`, which `echo $?` displays.

See the [development guide](docs/development.md#usage) for compiler usage,
build options, and testing.

## Further reading

- [Compiler architecture](docs/architecture.md): grammar, pipeline, and compiler stages.
- [C AST](docs/c-ast.md) and [assembly AST](docs/asm-ast.md): current intermediate representations.
- [Development guide](docs/development.md): setup, debugging, testing, and versioning.
- [Concepts](concepts/README.md): standalone C# examples to explore.
- [Notes](notes/): working notes for the book chapters.
- [Changelog](CHANGELOG.md): release history and current limitations.

## License

[MIT](LICENSE).
