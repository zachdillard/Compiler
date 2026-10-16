namespace Syntax.Assembly;

public record Program(Function Function);
public record Function(string Identifier, List<Instruction> Instructions);
public readonly union Instruction(Mov, Ret);
public record Mov(Operand Source, Operand Destination);
public record Ret();
public readonly union Operand(Imm, Register);
public record Imm(int Value);
public record Register();
