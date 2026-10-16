using C = Syntax.C;

namespace Helpers;

public interface IVisitor<out TResult>
{
  TResult Visit(C.Program program);
  TResult Visit(C.Function function);
  TResult Visit(C.Return statement);
  TResult Visit(C.Constant expression);
}

public interface INode
{
  TResult Accept<TResult>(IVisitor<TResult> visitor);
}
