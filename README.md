# Compiler

A small C compiler built with C# and .NET 11 based on [*Writing a C Compiler*](https://nostarch.com/writing-c-compiler) by Nora Sandler.

## Purpose

The goals of this project are to:

- Learn how compilers work by building one step by step.
- Showcase and explore .NET 11 features.
- Learn more about CI/CD through hands-on practice.
- Experiment with different development methodologies and agentic engineering.

## Capabilities

So far the compiler completes chapter one:

A single `main` function with `void` parameters that can only return a positive `int`.

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

The generated program prints nothing and exits with exit code `2`.

Use `echo $?` to display the result.

## License

[MIT](LICENSE).
