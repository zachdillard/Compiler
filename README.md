# C Compiler

A small C compiler built with C# and .NET 11 RC1, following
[*Writing a C Compiler*](https://nostarch.com/writing-c-compiler) by Nora Sandler.
This is a learning project, with more language features added as work progresses
through the book.

The custom compiler currently completes chapter one: a single `int` function
with `void` parameters that returns an integer constant.

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
echo $?
```

The compiler creates `data/return_2` beside the source file. The generated
program prints nothing and exits with status `2`, which `echo $?` displays.

## Usage

Pass a source path to compile it, adding options after `--`:

```sh
dotnet run --project src/Compiler.csproj -- [options] <source-file>
```

| Option | What it does |
| --- | --- |
| `--version` | Print the compiler version without invoking GCC. Use alone, without a source path. |
| `--lex` | Stop after lexing; print no tokens and produce no assembly or executable. |
| `-S` | Write custom assembly beside the source file, without linking. |
| `-gcc` | Use the temporary GCC-backed compiler instead of custom code generation. |
| `-gcc -S` | Write GCC assembly without linking; either option order works. |

For example, to inspect the custom compiler's assembly:

```sh
dotnet run --project src/Compiler.csproj -- -S data/return_2.c
```

Assembly-only modes leave `data/return_2.s`. Successful executable builds remove
intermediate `.i` and `.s` files; assembly-only and lex-only modes remove `.i`.
Lex-only mode exits with `0` for valid tokens and a nonzero status on an error.
Integer constants must fit in a C# `int`.

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
- [Development guide](docs/development.md): setup, debugging, formatting, and testing.
- [Concepts guide](concepts/README.md): standalone C# examples to explore.
- [Changelog](CHANGELOG.md): release history and current limitations.

Releases follow the book: `v0.N.0` completes chapter N and all earlier chapters;
`v0.N.P` contains fixes within that scope. See the development guide for the
[versioning policy](docs/development.md#versioning).

## License

[MIT](LICENSE).
