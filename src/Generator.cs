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

  private static List<Assembly.Instruction> GenerateInstructions(C.Statement statement) => statement switch
  {
    C.Return @return =>
    [
      new Assembly.Mov(GenerateImmediate(@return.Expression), new Assembly.Register()),
      new Assembly.Ret()
    ],
    _ => throw new InvalidOperationException("Unknown C statement.")
  };

  private static Assembly.Imm GenerateImmediate(C.Expression expression) => expression switch
  {
    C.Constant constant => new Assembly.Imm(constant.Value),
    _ => throw new InvalidOperationException("Unknown C expression.")
  };
}
