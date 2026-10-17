using System.Globalization;
using Helpers;
using Xunit;
using Assembly = Syntax.Assembly;

namespace CompilerIntegrationTests;

public sealed class AssemblyVisitorTests
{
  [Theory]
  [InlineData("main", 0, "0")]
  [InlineData("_entry1", 42, "42")]
  [InlineData("maximum", int.MaxValue, "2147483647")]
  public void PrintsGeneratedProgramAsIndentedTree(string identifier, int value, string immediate)
  {
    var program = Parser.Run(Lexer.Run($"int {identifier}(void) {{ return {value}; }}"));
    Assembly.Program assembly = Generator.Run(program);
    string expected = string.Join(Environment.NewLine,
      "Program", $"  Function(name=\"{identifier}\")", "    Mov",
      $"      Imm({immediate})", "      Register", "    Ret");

    Assert.Equal(expected, AssemblyVisitor.Run(assembly));
  }

  [Fact]
  public void PrintsStandaloneNodesWithoutParentIndentation()
  {
    IAssemblyNode[] nodes =
    [
      new Assembly.Function("main", [new Assembly.Ret()]),
      new Assembly.Mov(new Assembly.Imm(2), new Assembly.Register()),
      new Assembly.Ret(),
      new Assembly.Imm(2),
      new Assembly.Register()
    ];
    string[] expected =
    [
      $"Function(name=\"main\"){Environment.NewLine}  Ret",
      $"Mov{Environment.NewLine}  Imm(2){Environment.NewLine}  Register",
      "Ret",
      "Imm(2)",
      "Register"
    ];

    Assert.Equal(expected, nodes.Select(node => AssemblyVisitor.Run(node)).ToArray());
  }

  [Fact]
  public void PrintsUnionSubtreesWithoutParentIndentation()
  {
    Assembly.Instruction[] instructions =
    [
      new Assembly.Mov(new Assembly.Register(), new Assembly.Imm(7)),
      new Assembly.Ret()
    ];
    Assembly.Operand[] operands = [new Assembly.Imm(42), new Assembly.Register()];

    Assert.Equal(
      [$"Mov{Environment.NewLine}  Register{Environment.NewLine}  Imm(7)", "Ret"],
      instructions.Select(instruction => AssemblyVisitor.Run(instruction)).ToArray());
    Assert.Equal(["Imm(42)", "Register"],
      operands.Select(operand => AssemblyVisitor.Run(operand)).ToArray());
  }

  [Theory]
  [InlineData(int.MinValue, "Imm(-2147483648)")]
  [InlineData(0, "Imm(0)")]
  [InlineData(int.MaxValue, "Imm(2147483647)")]
  public void PrintsStandaloneImmediate(int value, string expected)
  {
    Assert.Equal(expected, AssemblyVisitor.Run(new Assembly.Imm(value)));
  }

  [Fact]
  public void FormatsImmediateIndependentlyOfCurrentCulture()
  {
    CultureInfo originalCulture = CultureInfo.CurrentCulture;
    CultureInfo customCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
    customCulture.NumberFormat.NegativeSign = "~";

    try
    {
      CultureInfo.CurrentCulture = customCulture;

      Assert.Equal("Imm(-42)", AssemblyVisitor.Run(new Assembly.Imm(-42)));
    }
    finally
    {
      CultureInfo.CurrentCulture = originalCulture;
    }
  }

  [Fact]
  public void PreservesInstructionOrder()
  {
    Assembly.Function function = new("main",
    [
      new Assembly.Mov(new Assembly.Imm(7), new Assembly.Register()),
      new Assembly.Ret(),
      new Assembly.Mov(new Assembly.Imm(42), new Assembly.Register())
    ]);
    string expected = string.Join(Environment.NewLine,
      "Function(name=\"main\")", "  Mov", "    Imm(7)", "    Register",
      "  Ret", "  Mov", "    Imm(42)", "    Register");

    Assert.Equal(expected, AssemblyVisitor.Run(function));
  }

  [Fact]
  public void PrintsEmptyFunctionWithoutTrailingNewline()
  {
    Assembly.Function function = new("empty", []);

    Assert.Equal("Function(name=\"empty\")", AssemblyVisitor.Run(function));
    Assert.Equal($"Program{Environment.NewLine}  Function(name=\"empty\")",
      AssemblyVisitor.Run(new Assembly.Program(function)));
  }

  [Fact]
  public void NodesDispatchToVisitorAndReturnItsResult()
  {
    Assembly.Imm immediate = new(42);
    Assembly.Register register = new();
    Assembly.Mov move = new(immediate, register);
    Assembly.Ret ret = new();
    Assembly.Function function = new("main", [move, ret]);
    Assembly.Program program = new(function);
    IAssemblyNode[] nodes = [program, function, move, ret, immediate, register];
    IAssemblyVisitor<int> visitor = new NodeValueVisitor();

    Assert.Equal([1, 2, 3, 4, 42, 6], nodes.Select(node => node.Accept(visitor)).ToArray());
  }

  [Fact]
  public void UnionsDispatchToVisitorAndReturnItsResult()
  {
    Assembly.Instruction[] instructions =
    [
      new Assembly.Mov(new Assembly.Imm(42), new Assembly.Register()),
      new Assembly.Ret()
    ];
    Assembly.Operand[] operands = [new Assembly.Imm(42), new Assembly.Register()];
    IAssemblyVisitor<int> visitor = new NodeValueVisitor();

    Assert.Equal([3, 4], instructions.Select(instruction => instruction.Accept(visitor)).ToArray());
    Assert.Equal([42, 6], operands.Select(operand => operand.Accept(visitor)).ToArray());
  }

  [Fact]
  public void RejectsDefaultInstructionWithDiagnostic()
  {
    Assembly.Instruction instruction = default;
    Assembly.Program program = new(new Assembly.Function("main", [instruction]));

    var standalone = Assert.Throws<InvalidOperationException>(() => AssemblyVisitor.Run(instruction));
    var nested = Assert.Throws<InvalidOperationException>(() => AssemblyVisitor.Run(program));

    Assert.Equal("Unknown assembly instruction.", standalone.Message);
    Assert.Equal("Unknown assembly instruction.", nested.Message);
  }

  [Fact]
  public void RejectsDefaultOperandWithDiagnostic()
  {
    Assembly.Operand operand = default;

    var exception = Assert.Throws<InvalidOperationException>(() => AssemblyVisitor.Run(operand));

    Assert.Equal("Unknown assembly operand.", exception.Message);
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void RejectsDefaultMoveOperandWithDiagnostic(bool invalidSource)
  {
    Assembly.Operand source = invalidSource ? default(Assembly.Operand) : new Assembly.Imm(42);
    Assembly.Operand destination = invalidSource ? new Assembly.Register() : default(Assembly.Operand);
    Assembly.Program program = new(new Assembly.Function("main", [new Assembly.Mov(source, destination)]));

    var exception = Assert.Throws<InvalidOperationException>(() => AssemblyVisitor.Run(program));

    Assert.Equal("Unknown assembly operand.", exception.Message);
  }

  private sealed class NodeValueVisitor : IAssemblyVisitor<int>
  {
    public int Visit(Assembly.Program program) => 1;
    public int Visit(Assembly.Function function) => 2;
    public int Visit(Assembly.Mov instruction) => 3;
    public int Visit(Assembly.Ret instruction) => 4;
    public int Visit(Assembly.Imm operand) => operand.Value;
    public int Visit(Assembly.Register operand) => 6;
  }
}
