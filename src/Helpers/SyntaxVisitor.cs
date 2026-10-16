using System.Globalization;
using C = Syntax.C;

namespace Helpers;

public class SyntaxVisitor : IVisitor<string>
{
  public static string Run(INode node) => node.Accept(new SyntaxVisitor());

  public string Visit(C.Program program) =>
    $"Program{Environment.NewLine}{Indent(program.Function.Accept(this))}";

  public string Visit(C.Function function) =>
    $"Function(name=\"{function.Identifier}\"){Environment.NewLine}{Indent(function.Statement.Accept(this))}";

  public string Visit(C.Return statement) =>
    $"Return{Environment.NewLine}{Indent(statement.Expression.Accept(this))}";

  public string Visit(C.Constant expression) =>
    $"Constant({expression.Value.ToString(CultureInfo.InvariantCulture)})";

  private static string Indent(string text) =>
    "  " + text.Replace(Environment.NewLine, Environment.NewLine + "  ");
}
