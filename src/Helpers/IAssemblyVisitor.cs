using Assembly = Syntax.Assembly;

namespace Helpers;

public interface IAssemblyVisitor<out TResult>
{
  TResult Visit(Assembly.Program program);
  TResult Visit(Assembly.Function function);
  TResult Visit(Assembly.Mov instruction);
  TResult Visit(Assembly.Ret instruction);
  TResult Visit(Assembly.Imm operand);
  TResult Visit(Assembly.Register operand);
}

public interface IAssemblyNode
{
  TResult Accept<TResult>(IAssemblyVisitor<TResult> visitor);
}

public static class AssemblyNodeExtensions
{
  public static TResult Accept<TResult>(this Assembly.Instruction instruction, IAssemblyVisitor<TResult> visitor) => instruction switch
  {
    Assembly.Mov mov => mov.Accept(visitor),
    Assembly.Ret ret => ret.Accept(visitor),
    _ => throw new InvalidOperationException("Unknown assembly instruction.")
  };

  public static TResult Accept<TResult>(this Assembly.Operand operand, IAssemblyVisitor<TResult> visitor) => operand switch
  {
    Assembly.Imm imm => imm.Accept(visitor),
    Assembly.Register register => register.Accept(visitor),
    _ => throw new InvalidOperationException("Unknown assembly operand.")
  };
}
