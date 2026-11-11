# Compiler

A small C compiler built with C# and .NET 11 based on [*Writing a C Compiler*](https://nostarch.com/writing-c-compiler) by Nora Sandler.

## Purpose

The goals of this project are to:

- Learn how compilers work by building one step by step.
- Showcase and explore .NET 11 features.
- Learn more about CI/CD through hands-on practice.
- Experiment with different development methodologies and agentic engineering.

## Scope

The compiler implements four stages:

- [Lexing](src/Lexer.cs)
- [Parsing](src/Parser.cs)
- [Code generation](src/Generator.cs)
- [Assembly emission](src/Emitter.cs)

It uses `gcc` to preprocess C source files, assemble the object file, and to produce the executable.

## Capabilities

So far the compiler completes chapter one:

A single `main` function with `void` parameters that returns a positive `int`.

```c
int main(void) {
    return 2;
}
```

## Quick Start

### Build the compiler

Clone the repository and build:

```sh
git clone https://github.com/zachdillard/Compiler.git
cd Compiler
dotnet build
```

### Run the compiler

The compiler requires `gcc` on `PATH`.

Add the build output to `PATH` for the current terminal and then compile a C file:

```sh
export PATH="$PWD/src/bin/Debug/net11.0:$PATH"
Compiler examples/return_int.c
```

The compiler creates `examples/return_int` beside the source file.

### Run the generated program

```sh
./examples/return_int
echo $?
# 2
```

The generated program returns immediately and exits with status code `2`.

Use `echo $?` to display the result.

## License

[MIT](LICENSE).
