using System.Globalization;
using Assembly = Syntax.Assembly;

namespace Helpers;

public class AssemblyVisitor : IAssemblyVisitor<string>
{
  public static string Run(IAssemblyNode node) => node.Accept(new AssemblyVisitor());

  // Prefer the concrete node over its implicit conversion to an assembly union.
  public static string Run<TNode>(TNode node) where TNode : IAssemblyNode => Run((IAssemblyNode)node);

  public static string Run(Assembly.Instruction instruction) => instruction.Accept(new AssemblyVisitor());

  public static string Run(Assembly.Operand operand) => operand.Accept(new AssemblyVisitor());

  public string Visit(Assembly.Program program) =>
    $"Program{Environment.NewLine}{Indent(program.Function.Accept(this))}";

  public string Visit(Assembly.Function function)
  {
    string header = $"Function(name=\"{function.Identifier}\")";
    if (function.Instructions.Count == 0)
      return header;

    string instructions = string.Join(Environment.NewLine,
      function.Instructions.Select(instruction => instruction.Accept(this)));
    return $"{header}{Environment.NewLine}{Indent(instructions)}";
  }

  public string Visit(Assembly.Mov instruction) =>
    $"Mov{Environment.NewLine}{Indent(instruction.Source.Accept(this))}{Environment.NewLine}{Indent(instruction.Destination.Accept(this))}";

  public string Visit(Assembly.Ret instruction) => "Ret";

  public string Visit(Assembly.Imm operand) =>
    $"Imm({operand.Value.ToString(CultureInfo.InvariantCulture)})";

  public string Visit(Assembly.Register operand) => "Register";

  private static string Indent(string text) =>
    "  " + text.Replace(Environment.NewLine, Environment.NewLine + "  ");
}
