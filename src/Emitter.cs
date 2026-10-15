using System.Text;
using Assembly = Syntax.Assembly;

public class Emitter
{
  public static string Run(Assembly.Program program)
  {
    StringBuilder output = new();

    output.AppendLine($"\t.globl _{program.Function.Identifer}");
    output.Append(EmitFunction(program.Function));

    return output.ToString();
  }

  private static string EmitFunction(Assembly.Function function)
  {
    StringBuilder output = new();

    output.AppendLine($"_{function.Identifer}:");
    foreach (Assembly.Instruction instruction in function.Instructions)
      output.AppendLine($"\t{EmitInstruction(instruction)}");

    return output.ToString();
  }

  private static string EmitInstruction(Assembly.Instruction instruction) => instruction switch
  {
    Assembly.Mov mov => $"movl {EmitOperand(mov.Source)}, {EmitOperand(mov.Destination)}",
    Assembly.Ret => "ret",
    _ => throw new InvalidOperationException("Unknown assembly instruction.")
  };

  private static string EmitOperand(Assembly.Operand operand) => operand switch
  {
    Assembly.Imm imm => $"${imm.Value}",
    Assembly.Register => "%eax",
    _ => throw new InvalidOperationException("Unknown assembly operand.")
  };
}
