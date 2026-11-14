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

        

        return instructions;
    }
}
