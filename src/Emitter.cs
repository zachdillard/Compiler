using Helpers;
using System.Text;
using Assembly = Syntax.Assembly;

public class Emitter
{
  public static string Run(Assembly.Program program, Target target)
  {
    StringBuilder output = new();
    string symbol = target switch
    {
      Target.Mac => $"_{program.Function.Identifier}",
      Target.Linux => program.Function.Identifier,
      _ => throw new InvalidOperationException("Unknown compilation target.")
    };

    output.AppendLine($"\t.globl {symbol}");
    output.Append(EmitFunction(program.Function, symbol));

    if (target == Target.Linux)
      output.AppendLine("\t.section .note.GNU-stack,\"\",@progbits");

    return output.ToString();
  }

  private static string EmitFunction(Assembly.Function function, string symbol)
  {
    StringBuilder output = new();

    output.AppendLine($"{symbol}:");
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
