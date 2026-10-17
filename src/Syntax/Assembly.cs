using Helpers;

namespace Syntax.Assembly;

public record Program(Function Function) : IAssemblyNode
{
  public TResult Accept<TResult>(IAssemblyVisitor<TResult> visitor) => visitor.Visit(this);
}

public record Function(string Identifier, List<Instruction> Instructions) : IAssemblyNode
{
  public TResult Accept<TResult>(IAssemblyVisitor<TResult> visitor) => visitor.Visit(this);
}

public readonly union Instruction(Mov, Ret);

public record Mov(Operand Source, Operand Destination) : IAssemblyNode
{
  public TResult Accept<TResult>(IAssemblyVisitor<TResult> visitor) => visitor.Visit(this);
}

public record Ret() : IAssemblyNode
{
  public TResult Accept<TResult>(IAssemblyVisitor<TResult> visitor) => visitor.Visit(this);
}

public readonly union Operand(Imm, Register);

public record Imm(int Value) : IAssemblyNode
{
  public TResult Accept<TResult>(IAssemblyVisitor<TResult> visitor) => visitor.Visit(this);
}

public record Register() : IAssemblyNode
{
  public TResult Accept<TResult>(IAssemblyVisitor<TResult> visitor) => visitor.Visit(this);
}
