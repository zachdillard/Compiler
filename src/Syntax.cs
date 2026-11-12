namespace Syntax.C
{
  public record Program(Function Function);
  public record Function(string Identifier, Return Statement);
  public record Return(Expression Expression);

  public readonly union Expression(Constant, Unary);
  public record Constant(int Value);
  public record Unary(Operator Operator, Expression Expression);

  public readonly union Operator(Complement, Negate);
  public record Complement();
  public record Negate();
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
