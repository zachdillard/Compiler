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

    private static ThreeAddressCode.Function LowerFunction(C.Function function)
    {
        List<ThreeAddressCode.Instruction> instructions = LowerStatement(function.Statement);
        return new ThreeAddressCode.Function(function.Identifier, instructions);
    }

    private static List<ThreeAddressCode.Instruction> LowerStatement(C.Return statement)
    {
        List<ThreeAddressCode.Instruction> instructions = [];
        ThreeAddressCode.Val value = LowerExpression(statement.Expression, instructions);
        instructions.Add(new ThreeAddressCode.Return(value));
        return instructions;
    }

    private static ThreeAddressCode.Val LowerExpression(C.Expression expression, List<ThreeAddressCode.Instruction> instructions)
    {
        switch (expression)
        {
            case C.Constant constant:
                return new ThreeAddressCode.Constant(constant.Value);
            case C.Unary unary:
                ThreeAddressCode.Val source = LowerExpression(unary.Expression, instructions);
                string destinationName = MakeTemporary();
                ThreeAddressCode.Val destination = new ThreeAddressCode.Var(destinationName);
                ThreeAddressCode.Operator op = LowerOperator(unary.Operator);
                instructions.Add(new ThreeAddressCode.Unary(op, source, destination));
                return destination;
            default:
                throw new NotSupportedException($"Expression type {expression.GetType().Name} is not supported.");
        }
    }

    private static ThreeAddressCode.Operator LowerOperator(C.Operator op)
    {
        switch (op)
        {
            case C.Negate:
                return new ThreeAddressCode.Negate();
            default:
                throw new NotSupportedException($"Operator {op} is not supported.");
        }
    }

    private static string MakeTemporary()
    {
        return $"t{Guid.NewGuid().ToString("N")}";
    }
}
