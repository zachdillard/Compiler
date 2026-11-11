#!/usr/bin/env -S dotnet --

using System.Text;
using System.Text.RegularExpressions;

string input =
"""
int main(void) {
    return 2;
}
""";

List<Token> tokens = Lex(input);
C.Program c = Parse(tokens);
Assembly.Program asm = Generate(c);
string output = Emit(asm);

Console.WriteLine(input);
Console.WriteLine();
Console.WriteLine(output);

List<Token> Lex(string input)
{
    List<Token> tokens = [];

    while (input != string.Empty)
    {
        input = input.TrimStart();
        if (input == string.Empty)
            break;

        bool matched = false;

        foreach ((Type type, Regex pattern) in Patterns)
        {
            Match match = pattern.Match(input);
            if (!match.Success || match.Index != 0)
                continue;

            tokens.Add(CreateToken(type, match.Value));
            input = input[match.Length..];
            matched = true;
            break;
        }

        if (!matched)
            throw new InvalidOperationException($"Unexpected character: '{input[0]}'");
    }

    return tokens;
}

Token CreateToken(Type type, string value) => type switch
{
    var _ when type == typeof(Identifier) => new Identifier(value),
    var _ when type == typeof(Int) => new Int(),
    var _ when type == typeof(Void) => new Void(),
    var _ when type == typeof(Return) => new Return(),
    var _ when type == typeof(Constant) => new Constant(int.Parse(value)),
    var _ when type == typeof(OpenParenthesis) => new OpenParenthesis(),
    var _ when type == typeof(CloseParenthesis) => new CloseParenthesis(),
    var _ when type == typeof(OpenBrace) => new OpenBrace(),
    var _ when type == typeof(CloseBrace) => new CloseBrace(),
    var _ when type == typeof(Semicolon) => new Semicolon(),
    _ => throw new InvalidOperationException()
};

C.Program Parse(List<Token> tokens)
{
    return new C.Program(ParseFunction(tokens));
}

C.Function ParseFunction(List<Token> tokens)
{
    Expect<Int>(tokens);
    string identifier = ParseIdentifier(tokens);
    Expect<OpenParenthesis>(tokens);
    Expect<Void>(tokens);
    Expect<CloseParenthesis>(tokens);
    Expect<OpenBrace>(tokens);
    C.Return statement = ParseStatement(tokens);
    Expect<CloseBrace>(tokens);

    return new C.Function(identifier, statement);
}

C.Return ParseStatement(List<Token> tokens)
{
    Expect<Return>(tokens);
    C.Constant expression = ParseExpression(tokens);
    Expect<Semicolon>(tokens);

    return new C.Return(expression);
}

C.Constant ParseExpression(List<Token> tokens)
{
    return new C.Constant(ParseInt(tokens));
}

string ParseIdentifier(List<Token> tokens)
{
    if (TakeToken(tokens) is Identifier identifier)
        return identifier.Value;

    throw new InvalidOperationException();
}

int ParseInt(List<Token> tokens)
{
    if (TakeToken(tokens) is Constant constant)
        return constant.Value;

    throw new InvalidOperationException();
}

void Expect<T>(List<Token> tokens)
{
    if (TakeToken(tokens) is not T)
        throw new InvalidOperationException();
}

Token TakeToken(List<Token> tokens)
{
    Token token = tokens.First();
    tokens.RemoveAt(0);
    return token;
}

Assembly.Program Generate(C.Program program)
{
    Assembly.Function function = GenerateFunction(program.Function);
    return new Assembly.Program(function);
}

Assembly.Function GenerateFunction(C.Function function)
{
    List<Assembly.Instruction> instructions = GenerateInstructions(function.Statement);
    return new Assembly.Function(function.Identifier, instructions);
}

List<Assembly.Instruction> GenerateInstructions(C.Return statement)
{
    Assembly.Imm imm = GenerateImmediate(statement.Expression);
    Assembly.Register reg = new();

    return
    [
        new Assembly.Mov(imm, reg),
        new Assembly.Ret()
    ];
}

Assembly.Imm GenerateImmediate(C.Constant expression)
{
    return new Assembly.Imm(expression.Value);
}

string Emit(Assembly.Program asm)
{
    StringBuilder output = new();

    output.AppendLine($"\t.globl _{asm.Function.Identifer}");
    output.AppendLine(EmitFunction(asm.Function));

    return output.ToString();
}

string EmitFunction(Assembly.Function function)
{
    StringBuilder output = new();

    output.AppendLine($"{function.Identifer}:");
    foreach (Assembly.Instruction instruction in function.Instructions)
        output.AppendLine($"\t{EmitInstruction(instruction)}");

    return output.ToString();
}

string EmitInstruction(Assembly.Instruction instruction) => instruction switch
{
    Assembly.Mov mov => $"movl {EmitOperand(mov.Source)}, {EmitOperand(mov.Destination)}",
    Assembly.Ret _ => "ret",
    _ => throw new InvalidOperationException()
};

string EmitOperand(Assembly.Operand operand) => operand switch
{
    Assembly.Imm imm => $"${imm.Value}",
    Assembly.Register => "%eax",
    _ => throw new InvalidOperationException()
};

readonly union Token
(
    Identifier,
    Int,
    Void,
    Return,
    Constant,
    OpenParenthesis,
    CloseParenthesis,
    OpenBrace,
    CloseBrace,
    Semicolon
);

record Identifier(string Value);
record Int();
record Void();
record Return();
record Constant(int Value);
record OpenParenthesis();
record CloseParenthesis();
record OpenBrace();
record CloseBrace();
record Semicolon();

namespace C
{
    record Program(Function Function);
    record Function(string Identifier, Return Statement);
    record Return(Constant Expression);
    record Constant(int Value);
}

namespace Assembly
{
    record Program(Function Function);
    record Function(string Identifer, List<Instruction> Instructions);
    readonly union Instruction(Mov, Ret);
    record Mov(Operand Source, Operand Destination);
    record Ret();
    readonly union Operand(Imm, Register);
    record Imm(int Value);
    record Register();
}

partial class Program
{
    private static readonly List<KeyValuePair<Type, Regex>> Patterns =
    [
        new(typeof(Int), IntPattern),
        new(typeof(Void), VoidPattern),
        new(typeof(Return), ReturnPattern),
        new(typeof(Identifier), IdentifierPattern),
        new(typeof(Constant), ConstantPattern),
        new(typeof(OpenParenthesis), OpenParenthesisPattern),
        new(typeof(CloseParenthesis), CloseParenthesisPattern),
        new(typeof(OpenBrace), OpenBracePattern),
        new(typeof(CloseBrace), CloseBracePattern),
        new(typeof(Semicolon), SemicolonPattern)
    ];

    [GeneratedRegex(@"[a-zA-Z_]\w*\b")]
    private static partial Regex IdentifierPattern { get; }

    [GeneratedRegex(@"\bint\b")]
    private static partial Regex IntPattern { get; }

    [GeneratedRegex(@"\bvoid\b")]
    private static partial Regex VoidPattern { get; }

    [GeneratedRegex(@"\breturn\b")]
    private static partial Regex ReturnPattern { get; }

    [GeneratedRegex(@"[0-9]+\b")]
    private static partial Regex ConstantPattern { get; }

    [GeneratedRegex(@"\(")]
    private static partial Regex OpenParenthesisPattern { get; }

    [GeneratedRegex(@"\)")]
    private static partial Regex CloseParenthesisPattern { get; }

    [GeneratedRegex(@"{")]
    private static partial Regex OpenBracePattern { get; }

    [GeneratedRegex(@"}")]
    private static partial Regex CloseBracePattern { get; }

    [GeneratedRegex(@";")]
    private static partial Regex SemicolonPattern { get; }
}
