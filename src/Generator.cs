using Assembly = Syntax.Assembly;
using C = Syntax.C;

public class Generator
{
  public static Assembly.Program Run(C.Program program)
  {
    Assembly.Function function = GenerateFunction(program.Function);
    return new Assembly.Program(function);
  }

  private static Assembly.Function GenerateFunction(C.Function function)
  {
    List<Assembly.Instruction> instructions = GenerateInstructions(function.Statement);
    return new Assembly.Function(function.Identifier, instructions);
  }

  private static List<Assembly.Instruction> GenerateInstructions(C.Return statement)
  {
    Assembly.Imm imm = GenerateImmediate(statement.Expression);
    Assembly.Register reg = new();

    return
    [
      new Assembly.Mov(imm, reg),
      new Assembly.Ret()
    ];
  }

  private static Assembly.Imm GenerateImmediate(C.Constant expression)
  {
    return new Assembly.Imm(expression.Value);
  }
}
