using Helpers;

namespace Syntax.C;

public record Program(Function Function) : INode
{
  public TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.Visit(this);
}

public record Function(string Identifier, Return Statement) : INode
{
  public TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.Visit(this);
}

public record Return(Constant Expression) : INode
{
  public TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.Visit(this);
}

public record Constant(int Value) : INode
{
  public TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.Visit(this);
}
