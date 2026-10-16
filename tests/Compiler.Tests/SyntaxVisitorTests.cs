using Helpers;
using Xunit;
using C = Syntax.C;

namespace CompilerIntegrationTests;

public sealed class SyntaxVisitorTests
{
  [Theory]
  [InlineData("main", 0)]
  [InlineData("_entry1", 42)]
  [InlineData("maximum", int.MaxValue)]
  public void PrintsParsedProgramAsIndentedTree(string identifier, int value)
  {
    C.Program program = Parser.Run(Lexer.Run($"int {identifier}(void) {{ return {value}; }}"));
    string expected = string.Join(Environment.NewLine,
      "Program", $"  Function(name=\"{identifier}\")", "    Return", $"      Constant({value})");

    string output = SyntaxVisitor.Run(program);

    Assert.Equal(expected, output);
  }

  [Fact]
  public void PrintsSubtreeWithoutParentIndentation()
  {
    INode statement = new C.Return(new C.Constant(2));

    string output = SyntaxVisitor.Run(statement);

    Assert.Equal($"Return{Environment.NewLine}  Constant(2)", output);
  }

  [Theory]
  [InlineData(int.MinValue, "Constant(-2147483648)")]
  [InlineData(0, "Constant(0)")]
  [InlineData(int.MaxValue, "Constant(2147483647)")]
  public void PrintsStandaloneConstant(int value, string expected)
  {
    string output = SyntaxVisitor.Run(new C.Constant(value));

    Assert.Equal(expected, output);
  }

  [Fact]
  public void NodesDispatchToVisitorAndReturnItsResult()
  {
    C.Constant expression = new(42);
    C.Return statement = new(expression);
    C.Function function = new("main", statement);
    C.Program program = new(function);
    INode[] nodes = [program, function, statement, expression];
    IVisitor<int> visitor = new NodeValueVisitor();

    int[] results = nodes.Select(node => node.Accept(visitor)).ToArray();

    Assert.Equal([1, 2, 3, 42], results);
  }

  private sealed class NodeValueVisitor : IVisitor<int>
  {
    public int Visit(C.Program program) => 1;
    public int Visit(C.Function function) => 2;
    public int Visit(C.Return statement) => 3;
    public int Visit(C.Constant expression) => expression.Value;
  }
}
