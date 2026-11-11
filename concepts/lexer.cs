#!/usr/bin/env -S dotnet --

using System.Text.RegularExpressions;

string input = "int main(void) { return 2; }";

Regex[] patterns = [IdentifierRegex, ConstantRegex, OpenParenthesisRegex, CloseParenthesisRegex, OpenBraceRegex, CloseBraceRegex, SemicolonRegex];
string[] keywords = ["int", "void", "return"];

while (input != string.Empty)
{
    input = input.TrimStart();
    if (input == string.Empty)
        break;

    int length = 0;
    bool matched = false;

    foreach (Regex pattern in patterns)
    {
        Match match = pattern.Match(input);
        if (match.Success && match.Index == 0)
        {
            matched = true;
            string value = match.Value;
            length = match.Length;

            switch (pattern)
            {
                case var _ when pattern == IdentifierRegex:
                    Console.WriteLine($"{(Array.IndexOf(keywords, value) >= 0 ? "keyword" : "identifier")}: {value}");
                    break;
                case var _ when pattern == ConstantRegex:
                    Console.WriteLine($"constant: {value}");
                    break;
                case var _ when pattern == OpenParenthesisRegex:
                    Console.WriteLine($"open_parenthesis: {value}");
                    break;
                case var _ when pattern == CloseParenthesisRegex:
                    Console.WriteLine($"close_parenthesis: {value}");
                    break;
                case var _ when pattern == OpenBraceRegex:
                    Console.WriteLine($"open_brace: {value}");
                    break;
                case var _ when pattern == CloseBraceRegex:
                    Console.WriteLine($"close_brace: {value}");
                    break;
                case var _ when pattern == SemicolonRegex:
                    Console.WriteLine($"semicolon: {value}");
                    break;
                default:
                    break;
            }
        }
    }

    if (!matched)
    {
        throw new InvalidOperationException($"Unexpected character: '{input[0]}'");
    }

    input = input.Substring(length);
}

partial class Program
{
    [GeneratedRegex(@"[a-zA-Z_]\w*\b")]
    private static partial Regex IdentifierRegex { get; }

    [GeneratedRegex(@"[0-9]+\b")]
    private static partial Regex ConstantRegex { get; }

    [GeneratedRegex(@"\(")]
    private static partial Regex OpenParenthesisRegex { get; }

    [GeneratedRegex(@"\)")]
    private static partial Regex CloseParenthesisRegex { get; }

    [GeneratedRegex(@"{")]
    private static partial Regex OpenBraceRegex { get; }

    [GeneratedRegex(@"}")]
    private static partial Regex CloseBraceRegex { get; }

    [GeneratedRegex(@";")]
    private static partial Regex SemicolonRegex { get; }
}
