namespace Syntax.C
{
  public record Program(Function Function);
  public record Function(string Identifier, Statement Statement);
  public readonly union Statement(Return);
  public record Return(Expression Expression);
  public readonly union Expression(Constant);
  public record Constant(int Value);
}

namespace Syntax.Assembly
{
  public record Program(Function Function);
  public record Function(string Identifier, List<Instruction> Instructions);
  public readonly union Instruction(Mov, Ret);
  public record Mov(Operand Source, Operand Destination);
  public record Ret();
  public readonly union Operand(Imm, Register);
  public record Imm(int Value);
  public record Register();
}
