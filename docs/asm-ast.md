```asdl
program = Program(function_definition)
function_definition = Function(identifier name, instruction* instructions)
instruction = Mov(operand src, operand dst) | Ret
operand = Imm(int) | Register
```

Every concrete assembly AST node implements `Helpers.IAssemblyNode`. Its
`Accept` method dispatches to the matching overload on
`Helpers.IAssemblyVisitor<TResult>`. Visitors choose their result type and call
`Accept` on child nodes to traverse the tree. These interfaces are separate from
the C AST visitor interfaces.

The `Instruction` and `Operand` unions also support `Accept` through extension
methods in `Helpers`. Default union values throw `InvalidOperationException`
with `Unknown assembly instruction.` or `Unknown assembly operand.`.

`Helpers.AssemblyVisitor` prints the generated assembly AST:

```csharp
using Helpers;

var program = Parser.Run(Lexer.Run("int main(void) { return 2; }"));
var assembly = Generator.Run(program);
Console.WriteLine(AssemblyVisitor.Run(assembly));
```

```text
Program
  Function(name="main")
    Mov
      Imm(2)
      Register
    Ret
```

`AssemblyVisitor.Run` accepts any `IAssemblyNode`, `Instruction`, or `Operand`
to print a whole tree or a standalone subtree. Output uses two-space
indentation, platform newlines, invariant integer formatting, and no trailing
newline. Instructions retain their list order, and move operands appear in
source/destination order. An empty function prints only its function header.
This helper does not change compiler command-line output or assembly emission.
