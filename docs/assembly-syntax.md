```text
Program = Program(Function Function)
Function = Function(string Identifier, List<Instruction> Instructions)
Instruction = Mov(Operand Source, Operand Destination) | Ret
Operand = Imm(int Value) | Register
```
