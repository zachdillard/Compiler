# Concepts

This folder contains small, self-contained C# examples that illustrate stages
of a compiler. They are learning examples related to the main compiler project
and can be run without building the compiler solution.

## Requirements

- .NET 11 RC1 SDK, as specified by the repository's `global.json`
- Preview C# language support for `compiler.cs`

Run each script from this folder:

```sh
dotnet <concept-name>.cs
```

| Example | Description |
| --- | --- |
| `lexer.cs` | Tokenizes a small C function and prints its tokens. |
| `parser.cs` | Parses a predefined token list into an abstract syntax tree. |
| `compiler.cs` | Combines lexing, parsing, code generation, and assembly emission for a sample function. |

## Debugging in VS Code

VS Code includes `Debug parser.cs` and `Debug compiler.cs` configurations.
Install the Microsoft C# extension to use them. Each configuration builds its
example into `obj/` before launching it; paths are relative to the workspace.
