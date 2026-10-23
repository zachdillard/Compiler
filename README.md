# Compiler

A small C compiler built with C# and .NET 11 based on [*Writing a C Compiler*](https://nostarch.com/writing-c-compiler) by Nora Sandler.

## Purpose

The goals of this project are to:

- Learn how compilers work by building one step by step.
- Showcase and explore .NET 11 features.
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

## Quick Start

### Build the compiler

Clone the repository and build:

```sh
git clone https://github.com/zachdillard/Compiler.git
cd Compiler
dotnet build -m:1 --disable-build-servers
```

### Run the compiler

Add the build output to `PATH` for the current terminal, then create and compile a temporary C file:

```sh
export PATH="$PWD/src/bin/Debug/net11.0:$PATH"
sample_dir=$(mktemp -d)
cat > "$sample_dir/return_2.c" <<'EOF'
int main(void) {
    return 2;
}
EOF
Compiler "$sample_dir/return_2.c"
```

The compiler creates `return_2` beside the temporary source file.

### Run the generated program

```sh
"$sample_dir/return_2"
echo $?
# 2
```

The generated program prints nothing and exits with status `2` and `echo $?` displays the result.

## Book tests

With Python 3.8 or newer and a sibling checkout of
[the book's test suite](https://github.com/nlsandler/writing-a-c-compiler-tests),
run the chapter-one tests from the repository root:

```sh
./scripts/book.sh --chapter 1
./scripts/book.sh --chapter 1 --stage lex
./scripts/book.sh --chapter 1 --stage parse
./scripts/book.sh --chapter 1 --stage codegen
```

The script builds the compiler before running the tests. Set `BOOK_TESTS_DIR`
if the test checkout is elsewhere, and `BUILD_CONFIGURATION` to select a build
configuration (the default is Debug).

## Further reading

- [C grammar](docs/c-grammar.md): the supported language grammar.
- [C syntax](docs/c-syntax.md) and [assembly syntax](docs/assembly-syntax.md): current intermediate representations.
- [Concepts](concepts/README.md): standalone C# examples to explore.

## License

[MIT](LICENSE).
