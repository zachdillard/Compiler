using Xunit;
using Assembly = Syntax.Assembly;

namespace CompilerIntegrationTests;

public sealed class EmitterTests
{
  [Theory]
  [InlineData("main", 2, "2")]
  [InlineData("answer", 0, "0")]
  [InlineData("_entry1", 42, "42")]
  [InlineData("minimum", int.MinValue, "-2147483648")]
  [InlineData("maximum", int.MaxValue, "2147483647")]
  public void EmitsFunctionWithMatchingSymbolAndInstructions(string identifier, int value, string immediate)
  {
    Assembly.Program program = new(new Assembly.Function(identifier,
    [
      new Assembly.Mov(new Assembly.Imm(value), new Assembly.Register()),
      new Assembly.Ret()
    ]));

    string output = Emitter.Run(program);

    Assert.Equal($"\t.globl _{identifier}\n_{identifier}:\n\tmovl ${immediate}, %eax\n\tret\n"
      .Replace("\n", Environment.NewLine), output);
  }

  [Fact]
  public void PreservesInstructionOrder()
  {
    Assembly.Program program = new(new Assembly.Function("main",
    [
      new Assembly.Mov(new Assembly.Imm(7), new Assembly.Register()),
      new Assembly.Mov(new Assembly.Imm(42), new Assembly.Register()),
      new Assembly.Ret()
    ]));
    string expected = string.Join(Environment.NewLine,
      "\t.globl _main", "_main:", "\tmovl $7, %eax", "\tmovl $42, %eax", "\tret", "");

    Assert.Equal(expected, Emitter.Run(program));
  }

  [Fact]
  public void EmitsFunctionHeaderForEmptyInstructions()
  {
    Assembly.Program program = new(new Assembly.Function("empty", []));
    string expected = string.Join(Environment.NewLine, "\t.globl _empty", "_empty:", "");

    Assert.Equal(expected, Emitter.Run(program));
  }

  [Fact]
  public void RejectsDefaultInstructionWithDiagnostic()
  {
    Assembly.Program program = new(new Assembly.Function("main", [default]));

    InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Emitter.Run(program));

    Assert.Equal("Unknown assembly instruction.", exception.Message);
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void RejectsDefaultOperandWithDiagnostic(bool invalidSource)
  {
    Assembly.Operand source = invalidSource ? default(Assembly.Operand) : new Assembly.Imm(42);
    Assembly.Operand destination = invalidSource ? new Assembly.Register() : default(Assembly.Operand);
    Assembly.Program program = new(new Assembly.Function("main", [new Assembly.Mov(source, destination)]));

    InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Emitter.Run(program));

    Assert.Equal("Unknown assembly operand.", exception.Message);
  }
}
