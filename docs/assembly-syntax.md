```asdl
program = Program(function Function)
function = Function(string Identifier, instruction* Instructions)
instruction = Mov(operand Source, operand Destination) | Ret
operand = Imm(int Value) | Register
```
