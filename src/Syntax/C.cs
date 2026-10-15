namespace Syntax.C;

public record Program(Function Function);
public record Function(string Identifier, Return Statement);
public record Return(Constant Expression);
public record Constant(int Value);
