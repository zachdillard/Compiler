using Helpers;
using C = Syntax.C;
using ThreeAddressCode = Syntax.ThreeAddressCode;

public class Lowerer
{
    public static ThreeAddressCode.Program Run(C.Program program)
    {
        ThreeAddressCode.Function function = LowerFunction(program.Function);
        return new ThreeAddressCode.Program(function);
    }

    public static ThreeAddressCode.Function LowerFunction(C.Function function)
    {
        List<ThreeAddressCode.Instruction> instructions = LowerStatement(function.Statement);
        return new ThreeAddressCode.Function(function.Identifier, instructions);
    }

    public static List<ThreeAddressCode.Instruction> LowerStatement(C.Return statement)
    {
        List<ThreeAddressCode.Instruction> instructions = [];
        instructions.AddRange(LowerExpression(statement.Expression, out ThreeAddressCode.Val value));
        instructions.Add(new ThreeAddressCode.Return(value));
        return instructions;
    }

    public static List<ThreeAddressCode.Instruction> LowerExpression(C.Expression expression, out ThreeAddressCode.Val value)
    {
        List<ThreeAddressCode.Instruction> instructions = [];

        value = new ThreeAddressCode.Constant(0);
        return instructions;
    }
}
